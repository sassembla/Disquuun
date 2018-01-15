using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace DisquuunCore
{
    public class DisquuunSocket : SocketBase
    {
        public readonly string socketId;
        private int pipelineIndex = 0;

        private Action<DisquuunSocket, int> SocketOpened;
        public Action<DisquuunSocket> SocketReloaded;
        private Action<DisquuunSocket, string, Exception> SocketClosed;


        private SocketToken socketToken;

        public bool IsAvailable()
        {
            if (socketToken.socketState == SocketState.OPENED)
            {
                return true;
            }

            return false;
        }

        public void SetBusy()
        {
            socketToken.socketState = SocketState.BUSY;
        }

        public enum SocketState
        {
            NONE,
            OPENING,
            OPENED,
            BUSY,

            SENDED,
            RECEIVED,

            CLOSING,
            CLOSED
        }

        public class SocketToken
        {
            public SocketState socketState;

            public readonly Socket socket;

            public byte[] receiveBuffer;
            public int readableDataLength;

            public readonly SocketAsyncEventArgs connectArgs;
            public SocketAsyncEventArgs sendArgs;
            public readonly SocketAsyncEventArgs receiveArgs;

            public bool isPipeline;
            public bool continuation;

            public DisqueCommand[] currentCommands;
            public byte[] currentSendingBytes;

            public Func<DisqueCommand, DisquuunCore.DisquuunResult[], bool> AsyncCallback;

            public SocketToken() { }

            public SocketToken(Socket socket, long bufferSize, SocketAsyncEventArgs connectArgs, SocketAsyncEventArgs sendArgs, SocketAsyncEventArgs receiveArgs)
            {
                this.socket = socket;

                this.receiveBuffer = new byte[bufferSize];

                this.connectArgs = connectArgs;
                this.sendArgs = sendArgs;
                this.receiveArgs = receiveArgs;

                this.connectArgs.UserToken = this;
                this.sendArgs.UserToken = this;
                this.receiveArgs.UserToken = this;

                this.receiveArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
            }
        }
        public readonly int socketIndex;

        public DisquuunSocket(
            int index,
            Action<DisquuunSocket, int> SocketOpenedAct,
            Action<DisquuunSocket> SocketReloadedAct,
            Action<DisquuunSocket, string, Exception> SocketClosedAct
        )
        {
            this.socketIndex = index;
            this.socketId = Guid.NewGuid().ToString();

            this.SocketOpened = SocketOpenedAct;
            this.SocketReloaded = SocketReloadedAct;
            this.SocketClosed = SocketClosedAct;
        }

        public void Connect(IPEndPoint endPoint, long bufferSize)
        {
            try
            {
                var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.NoDelay = true;

                var connectArgs = new SocketAsyncEventArgs();
                connectArgs.RemoteEndPoint = endPoint;
                connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnect);

                var sendArgs = new SocketAsyncEventArgs();
                sendArgs.RemoteEndPoint = endPoint;
                sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);

                var receiveArgs = new SocketAsyncEventArgs();
                receiveArgs.RemoteEndPoint = endPoint;
                receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceived);

                socketToken = new SocketToken(clientSocket, bufferSize, connectArgs, sendArgs, receiveArgs);

                socketToken.socketState = SocketState.OPENING;

                // start connect.
                StartConnectAsync(clientSocket, socketToken.connectArgs);
            }
            catch (Exception e)
            {
                SocketClosed(this, "failed to create new socket.", e);
            }
        }

        private void StartConnectAsync(Socket clientSocket, SocketAsyncEventArgs connectArgs)
        {
#if LOGIC_BENCH
            {
                socketToken.socketState = SocketState.OPENED;
                SocketOpened(this, socketIndex);
                return;
            }
#endif

            if (!clientSocket.ConnectAsync(socketToken.connectArgs))
            {
                OnConnect(clientSocket, connectArgs);
            }
        }

        /*
			Core methods of Disquuun.
		*/

        /**
			method for Sync execution of specific Disque command.
			DEPRECATED. only use for testing.
		*/
        public override DisquuunResult[] DEPRECATED_Sync(DisqueCommand command, byte[] data)
        {
#if LOGIC_BENCH
            {
                socketToken.socketState = SocketState.OPENED;
                return new DisquuunResult[0];
            }
#endif
            try
            {
                socketToken.socket.Send(data);

                var currentLength = 0;
                var scanResult = new DisquuunAPI.ScanResult(false);

                while (true)
                {
                    // waiting for head of transferring data or rest of data.
                    socketToken.socket.Receive(socketToken.receiveBuffer, currentLength, 1, SocketFlags.None);
                    currentLength = currentLength + 1;

                    var available = socketToken.socket.Available;
                    var readableLength = currentLength + available;
                    {
                        if (socketToken.receiveBuffer.Length < readableLength)
                        {
                            Array.Resize(ref socketToken.receiveBuffer, readableLength);
                        }
                    }

                    // read rest.
                    socketToken.socket.Receive(socketToken.receiveBuffer, currentLength, available, SocketFlags.None);
                    currentLength = currentLength + available;

                    scanResult = DisquuunAPI.ScanBuffer(command, socketToken.receiveBuffer, 0, currentLength, socketId);
                    if (scanResult.isDone) break;

                    // continue reading data from socket.
                    // if need, prepare for next 1 byte.
                    if (socketToken.receiveBuffer.Length == readableLength)
                    {
                        Array.Resize(ref socketToken.receiveBuffer, socketToken.receiveBuffer.Length + 1);
                    }
                }

                socketToken.socketState = SocketState.OPENED;
                return scanResult.data;

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /**
			method for Async execution of specific Disque command.
		*/
        public override void Async(DisqueCommand[] commands, byte[] data, Func<DisqueCommand, DisquuunResult[], bool> Callback)
        {
            switch (socketToken.socketState)
            {
                case SocketState.BUSY:
                    {
                        StartReceiveAndSendDataAsync(commands, data, Callback);
                        break;
                    }
            }
        }

        /**
			method for start Looping of specific Disque command.
		*/
        public override void Loop(DisqueCommand[] commands, byte[] data, Func<DisqueCommand, DisquuunResult[], bool> Callback)
        {
            switch (socketToken.socketState)
            {
                case SocketState.BUSY:
                    {
                        StartReceiveAndSendDataAsync(commands, data, Callback);
                        break;
                    }
            }
        }

        /**
			method for execute pipelined commands.
		*/
        public override void Execute(DisqueCommand[] commands, byte[] wholeData, Func<DisqueCommand, DisquuunResult[], bool> Callback)
        {
            switch (socketToken.socketState)
            {
                case SocketState.BUSY:
                    {
                        StartReceiveAndSendDataAsync(commands, wholeData, Callback);
                        break;
                    }
            }
        }

        private void StartReceiveAndSendDataAsync(DisqueCommand[] commands, byte[] data, Func<DisqueCommand, DisquuunResult[], bool> Callback)
        {
#if LOGIC_BENCH
            {
                socketToken.currentCommands = commands;
                socketToken.currentSendingBytes = data;
                socketToken.AsyncCallback = Callback;

                socketToken.isPipeline = false;
                if (1 < commands.Length)
                {
                    socketToken.isPipeline = true;
                    pipelineIndex = 0;

                    // -> PipelineReceive(token);
                    {
                        foreach (var command in commands)
                        {
                            var result = new DisquuunResult[0];
                            socketToken.AsyncCallback(command, result);
                        }
                        socketToken.socketState = SocketState.OPENED;
                        SocketReloaded(this);
                    }
                }
                else
                {
                    // OnSend -> token.socketState = SocketState.SENDED;
                    // receive -> Bench_LoopOrAsyncReceive(token);
                    {
                        Bench_LoopOrAsyncReceive(commands[0]);
                    }
                }
                return;
            }
#endif

            // ready for receive.
            socketToken.readableDataLength = 0;

            socketToken.receiveArgs.SetBuffer(socketToken.receiveBuffer, 0, socketToken.receiveBuffer.Length);
            if (!socketToken.socket.ReceiveAsync(socketToken.receiveArgs))
            {
                OnReceived(socketToken.socket, socketToken.receiveArgs);
            }

            // if multiple commands exist, set as pipeline.
            socketToken.isPipeline = false;
            if (1 < commands.Length)
            {
                socketToken.isPipeline = true;
                pipelineIndex = 0;
            }

            socketToken.currentCommands = commands;
            socketToken.currentSendingBytes = data;
            socketToken.AsyncCallback = Callback;

            try
            {
                socketToken.sendArgs.SetBuffer(data, 0, data.Length);
            }
            catch
            {
                // renew. potential error is exists and should avoid this error.
                var sendArgs = new SocketAsyncEventArgs();
                sendArgs.RemoteEndPoint = socketToken.receiveArgs.RemoteEndPoint;
                sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);
                sendArgs.UserToken = socketToken;

                socketToken.sendArgs = sendArgs;
                socketToken.sendArgs.SetBuffer(data, 0, data.Length);
            }

            if (!socketToken.socket.SendAsync(socketToken.sendArgs))
            {
                OnSend(socketToken.socket, socketToken.sendArgs);
            }
        }

#if LOGIC_BENCH
        private void Bench_LoopOrAsyncReceive(DisqueCommand command)
        {
            // ここで、コマンドの結果内容を精査する場合、結果を生成する必要があるが、どうするかな、、
            var result = new DisquuunResult[0];
            socketToken.continuation = socketToken.AsyncCallback(command, result);
            if (socketToken.continuation)
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    Bench_LoopOrAsyncReceive(command);
                });
            }
            else
            {
                socketToken.socketState = SocketState.OPENED;
                SocketReloaded(this);
            }
        }
#endif

        /*
            socket handlers
        */
        private void OnConnect(object unused, SocketAsyncEventArgs args)
        {
            var token = (SocketToken)args.UserToken;
            switch (token.socketState)
            {
                case SocketState.OPENING:
                    {
                        if (args.SocketError != SocketError.Success)
                        {
                            token.socketState = SocketState.CLOSED;
                            var error = new Exception("connect error:" + args.SocketError.ToString());

                            SocketClosed(this, "connect failed.", error);
                            return;
                        }

                        token.socketState = SocketState.OPENED;
                        SocketOpened(this, socketIndex);
                        return;
                    }
                default:
                    {
                        SocketClosed(this, "connect failed.", new Exception("socket state does not correct:" + token.socketState));
                        return;
                    }
            }
        }

        private void OnSend(object unused, SocketAsyncEventArgs args)
        {
            switch (args.SocketError)
            {
                case SocketError.Success:
                    {
                        var token = args.UserToken as SocketToken;

                        switch (token.socketState)
                        {
                            case SocketState.BUSY:
                                {
                                    token.socketState = SocketState.SENDED;
                                    break;
                                }
                            case SocketState.RECEIVED:
                                {
                                    if (token.continuation)
                                    {
                                        // ready for next loop receive.
                                        token.readableDataLength = 0;
                                        token.receiveArgs.SetBuffer(token.receiveBuffer, 0, token.receiveBuffer.Length);
                                        if (!token.socket.ReceiveAsync(token.receiveArgs))
                                        {
                                            OnReceived(token.socket, token.receiveArgs);
                                        }

                                        try
                                        {
                                            token.sendArgs.SetBuffer(token.currentSendingBytes, 0, token.currentSendingBytes.Length);
                                        }
                                        catch
                                        {
                                            // renew. potential error is exists and should avoid this error.
                                            var sendArgs = new SocketAsyncEventArgs();
                                            sendArgs.RemoteEndPoint = token.receiveArgs.RemoteEndPoint;
                                            sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);
                                            sendArgs.UserToken = token;
                                            token.sendArgs = sendArgs;
                                            token.sendArgs.SetBuffer(token.currentSendingBytes, 0, token.currentSendingBytes.Length);
                                        }

                                        if (!token.socket.SendAsync(token.sendArgs))
                                        {
                                            OnSend(token.socket, token.sendArgs);
                                        }
                                        return;
                                    }

                                    token.socketState = SocketState.OPENED;
                                    SocketReloaded(this);
                                    return;
                                }
                        }
                        return;
                    }
                default:
                    {
                        DisquuunLogger.Log("onsend error, " + args.SocketError, true);
                        // if (Error != null) {
                        // 	var error = new Exception("send error:" + socketError.ToString());
                        // 	Error(error);
                        // }
                        return;
                    }
            }
        }



        private void OnReceived(object unused, SocketAsyncEventArgs args)
        {
            var token = (SocketToken)args.UserToken;
            if (args.SocketError != SocketError.Success)
            {
                switch (token.socketState)
                {
                    case SocketState.CLOSING:
                    case SocketState.CLOSED:
                        {
                            // already closing, ignore.
                            return;
                        }
                    default:
                        {
                            Disconnect();

                            var e1 = new Exception("receive status is not good. socket error:" + args.SocketError);
                            SocketClosed(this, "failed to receive.", e1);
                            return;
                        }
                }
            }

            if (args.BytesTransferred == 0)
            {
                return;
            }

            var bytesAmount = args.BytesTransferred;

            // update token-dataLength as read completed.
            token.readableDataLength = token.readableDataLength + bytesAmount;

            if (token.isPipeline)
            {
                PipelineReceive(token);
            }
            else
            {
                LoopOrAsyncReceive(token);
            }
        }

        private void PipelineReceive(SocketToken token)
        {
            var fromCursor = 0;


            /*
                read data from receiveBuffer by moving fromCursor.
            */
            while (true)
            {
                var currentCommand = token.currentCommands[pipelineIndex];
                var result = DisquuunAPI.ScanBuffer(currentCommand, token.receiveBuffer, fromCursor, token.readableDataLength, socketId);

                if (result.isDone)
                {
                    token.AsyncCallback(currentCommand, result.data);

                    // deque as read done.
                    pipelineIndex++;

                    if (token.currentCommands.Length == pipelineIndex)
                    {
                        // pipelining is over.
                        switch (token.socketState)
                        {
                            case SocketState.BUSY:
                                {
                                    token.socketState = SocketState.RECEIVED;
                                    break;
                                }
                            case SocketState.SENDED:
                                {
                                    token.socketState = SocketState.OPENED;
                                    SocketReloaded(this);
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }
                        return;
                    }

                    // commands are still remained.

                    // got all data is just consumed. get rest from outside.
                    if (fromCursor == token.readableDataLength)
                    {
                        StartContinueReceiving(token, 0);
                        return;
                    }

                    // rest pipeline commands and received data is exists in buffer. 
                    fromCursor = result.cursor;
                    continue;
                }

                /*
                    reading is not completed. the fragment of command exists.
                */

                var fragmentDataLength = token.readableDataLength - fromCursor;

                // move fragment data to head of buffer.
                Buffer.BlockCopy(token.receiveBuffer, fromCursor, token.receiveBuffer, 0, fragmentDataLength);

                StartContinueReceiving(token, fragmentDataLength);
                break;
            }
        }

        private void LoopOrAsyncReceive(SocketToken token)
        {
            var currentCommand = token.currentCommands[0];
            var result = DisquuunAPI.ScanBuffer(currentCommand, token.receiveBuffer, 0, token.readableDataLength, socketId);

            if (result.isDone && result.cursor == token.readableDataLength)
            {
                // update continuation status.
                token.continuation = token.AsyncCallback(currentCommand, result.data);

                if (token.continuation)
                {
                    switch (token.socketState)
                    {
                        case SocketState.BUSY:
                            {
                                token.socketState = SocketState.RECEIVED;
                                break;
                            }
                        case SocketState.SENDED:
                            {
                                // ready for next loop receive.
                                token.readableDataLength = 0;
                                token.receiveArgs.SetBuffer(token.receiveBuffer, 0, token.receiveBuffer.Length);
                                if (!token.socket.ReceiveAsync(token.receiveArgs))
                                {
                                    OnReceived(token.socket, token.receiveArgs);
                                }

                                try
                                {
                                    token.sendArgs.SetBuffer(token.currentSendingBytes, 0, token.currentSendingBytes.Length);
                                }
                                catch
                                {
                                    // renew. potential error is exists and should avoid this error.
                                    var sendArgs = new SocketAsyncEventArgs();
                                    sendArgs.RemoteEndPoint = token.receiveArgs.RemoteEndPoint;
                                    sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);
                                    sendArgs.UserToken = token;
                                    token.sendArgs = sendArgs;
                                    token.sendArgs.SetBuffer(token.currentSendingBytes, 0, token.currentSendingBytes.Length);
                                }

                                if (!token.socket.SendAsync(token.sendArgs))
                                {
                                    OnSend(token.socket, token.sendArgs);
                                }

                                break;
                            }
                        default:
                            {
                                // closing or other state. should close.
                                break;
                            }
                    }
                    return;
                }

                // end of loop or end of async.

                switch (token.socketState)
                {
                    case SocketState.BUSY:
                        {
                            token.socketState = SocketState.RECEIVED;
                            break;
                        }
                    case SocketState.SENDED:
                        {
                            token.socketState = SocketState.OPENED;
                            SocketReloaded(this);
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
                return;
            }

            // not yet received all data.
            // continue receiving.

            StartContinueReceiving(token, token.readableDataLength);
        }

        private void StartContinueReceiving(SocketToken token, int receiveAfterFragmentIndex)
        {
            // set already got data length to set param.
            token.readableDataLength = receiveAfterFragmentIndex;

            /*
                get readable size of already received data for next read=OnReceived.
                resize if need.
            */
            var nextAdditionalBytesLength = token.socket.Available;
            if (receiveAfterFragmentIndex == token.receiveBuffer.Length)
            {
                Array.Resize(ref token.receiveBuffer, token.receiveArgs.Buffer.Length + nextAdditionalBytesLength);
            }

            /*
                note that,

                SetBuffer([buffer], offset, count)'s "count" is, actually not count.

                it's "offset" is "offset of receiving-data-window against buffer",
                but the "count" is actually "size limitation of next receivable data size".

                this "size" should be smaller than size of current bufferSize - offset && larger than 0.

                e.g.
                    if buffer is buffer[10], offset can set 0 ~ 8, and,
                    count should be 9 ~ 1.

                if vaiolate to this rule, ReceiveAsync never receive data. not good behaviour.

                and, the "buffer" is treated as pointer. this API treats the pointer of buffer directly.
                this means, when the byteTransferred is reaching to the size of "buffer", then you resize it to proper size,

                you should re-set the buffer's pointer by using SetBuffer API.


                actually, SetBuffer's parameters are below.

                socket.SetBuffer([bufferAsPointer], additionalDataOffset, receiveSizeLimit)
            */
            var receivableCount = token.receiveBuffer.Length - receiveAfterFragmentIndex;

            // should set token.receiveBuffer to receiveArgs. because it was resized or not.
            // and of cource this SetBuffer is for setting receivableCount.
            token.receiveArgs.SetBuffer(token.receiveBuffer, receiveAfterFragmentIndex, receivableCount);

            if (!token.socket.ReceiveAsync(token.receiveArgs))
            {
                OnReceived(token.socket, token.receiveArgs);
            }
        }

        public void Disconnect()
        {
            try
            {
                socketToken.socketState = SocketState.CLOSING;
                socketToken.socket.Shutdown(SocketShutdown.Both);
                socketToken.socket.Dispose();
            }
            catch
            {

            }
            finally
            {
                socketToken.socketState = SocketState.CLOSED;
            }
            return;
        }
    }

    public struct StackCommandData
    {
        public readonly DisquuunExecuteType executeType;
        public readonly DisqueCommand[] commands;
        public readonly byte[] data;
        public readonly Func<DisqueCommand, DisquuunResult[], bool> Callback;

        public StackCommandData(DisquuunExecuteType executeType, DisqueCommand[] commands, byte[] dataSource, Func<DisqueCommand, DisquuunResult[], bool> Callback)
        {
            this.executeType = executeType;
            this.commands = commands;
            this.data = dataSource;
            this.Callback = Callback;
        }
    }


    /**
        extension definition for DisquuunSocket.
*/
    public static class DisquuunExtension
    {
        public static DisquuunResult[] DEPRICATED_Sync(this DisquuunInput input)
        {
            var socket = input.socketPool.ChooseAvailableSocket();
            return socket.DEPRECATED_Sync(input.command, input.data);
        }

        public static void Async(this DisquuunInput input, Action<DisqueCommand, DisquuunResult[]> Callback)
        {
            var socket = input.socketPool.ChooseAvailableSocket();
            var commands = new DisqueCommand[] { input.command };

            socket.Async(
                commands,
                input.data,
                (command, resultBytes) =>
                {
                    Callback(command, resultBytes);
                    return false;
                }
            );
        }

        public static void Loop(this DisquuunInput input, Func<DisqueCommand, DisquuunResult[], bool> Callback)
        {
            var socket = input.socketPool.ChooseAvailableSocket();
            var commands = new DisqueCommand[] { input.command };

            socket.Loop(commands, input.data, Callback);
        }

        public static void Execute(this List<List<DisquuunInput>> inputs, Action<DisqueCommand, DisquuunResult[]> Callback)
        {
            if (!inputs.Any())
            {
                return;
            }
            if (!inputs[0].Any())
            {
                return;
            }

            var socketPool = inputs[0][0].socketPool;

            for (var i = 0; i < inputs.Count; i++)
            {
                var currentSlotInputs = inputs[i];

                var socket = socketPool.ChooseAvailableSocket();

                var commands = new DisqueCommand[currentSlotInputs.Count];
                for (var j = 0; j < currentSlotInputs.Count; j++)
                {
                    commands[j] = currentSlotInputs[j].command;
                }

                using (var memStream = new MemoryStream())
                {
                    for (var j = 0; j < currentSlotInputs.Count; j++)
                    {
                        var input = currentSlotInputs[j];
                        memStream.Write(input.data, 0, input.data.Length);
                    }

                    var wholeData = memStream.ToArray();

                    socket.Execute(
                        commands,
                        wholeData,
                        (command, resultBytes) =>
                        {
                            Callback(command, resultBytes);
                            return false;
                        }
                    );
                }
            }

            inputs.Clear();
        }
    }

}
