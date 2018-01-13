
using System;
using System.Net;

namespace DisquuunCore
{
    public class DisquuunSocketPool
    {
        private DisquuunSocket[] sockets;// ここを可変式にすべき

        private SocketBase stackSocket;

        private object lockObject = new object();

        public DisquuunSocketPool(int connectionCount, Action<DisquuunSocket, string> OnSocketOpened, Action<DisquuunSocket, string, Exception> OnSocketConnectionFailed)
        {
            this.stackSocket = new SocketBase();
            this.sockets = new DisquuunSocket[connectionCount];
            for (var i = 0; i < sockets.Length; i++)
            {
                this.sockets[i] = new DisquuunSocket(OnSocketOpened, this.OnReloaded, OnSocketConnectionFailed);
            }
        }

        public void Connect(IPEndPoint endPoint, long bufferSize)
        {
            for (var i = 0; i < sockets.Length; i++)
            {
                this.sockets[i].Connect(endPoint, bufferSize);
            }
        }

        public void Disconnect()
        {
            lock (lockObject)
            {
                for (var i = 0; i < sockets.Length; i++)
                {
                    var socket = sockets[i];
                    socket.Disconnect();
                }
            }
        }

        public SocketBase ChooseAvailableSocket()
        {
            lock (lockObject)
            {
                for (var i = 0; i < sockets.Length; i++)
                {
                    var socket = sockets[i];

                    if (socket.IsChoosable())
                    {
                        socket.SetBusy();
                        return socket;
                    }
                }

                DisquuunLogger.Log("stacked.");
                return stackSocket;
            }
        }

        public void OnReloaded(DisquuunSocket reloadedSocket)
        {
            lock (lockObject)
            {
                if (stackSocket.IsQueued())
                {
                    if (reloadedSocket.IsChoosable())
                    {
                        reloadedSocket.SetBusy();

                        var commandAndData = stackSocket.Dequeue();
                        switch (commandAndData.executeType)
                        {
                            case DisquuunExecuteType.ASYNC:
                                {
                                    reloadedSocket.Async(commandAndData.commands, commandAndData.data, commandAndData.Callback);
                                    return;
                                }
                            case DisquuunExecuteType.LOOP:
                                {
                                    reloadedSocket.Loop(commandAndData.commands, commandAndData.data, commandAndData.Callback);
                                    return;
                                }
                            case DisquuunExecuteType.PIPELINE:
                                {
                                    reloadedSocket.Execute(commandAndData.commands, commandAndData.data, commandAndData.Callback);
                                    return;
                                }
                        }
                    }
                }
            }
        }

        public int AvailableSocketNum()
        {
            lock (lockObject)
            {
                var availableSocketCount = 0;
                for (var i = 0; i < sockets.Length; i++)
                {
                    var socket = sockets[i];
                    if (socket == null)
                    {
                        continue;
                    }
                    if (socket.IsChoosable())
                    {
                        availableSocketCount++;
                    }
                }
                return availableSocketCount;
            }
        }

        public int StackedCommandCount()
        {
            lock (lockObject)
            {
                return stackSocket.QueueCount();
            }
        }
    }
}