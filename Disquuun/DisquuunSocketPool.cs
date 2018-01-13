
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace DisquuunCore
{
    public class DisquuunSocketPool
    {
        private DisquuunSocket[] sockets;// ここを可変式にすべき

        private SocketBase disquuunDataStack;

        private object poolLock = new object();

        public DisquuunSocketPool(int defaultConnectionCount, Action<DisquuunSocket, int> OnSocketOpened, Action<DisquuunSocket, string, Exception> OnSocketConnectionFailed)
        {
            this.disquuunDataStack = new SocketBase();

            this.sockets = new DisquuunSocket[defaultConnectionCount];
            for (var i = 0; i < sockets.Length; i++)
            {
                this.sockets[i] = new DisquuunSocket(i, OnSocketOpened, this.OnReloaded, OnSocketConnectionFailed);
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
            lock (poolLock)
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
            lock (poolLock)
            {
                for (var i = 0; i < sockets.Length; i++)
                {
                    var socket = sockets[i];

                    if (socket.IsAvailable())
                    {
                        socket.SetBusy();
                        return socket;
                    }
                }

                // DisquuunLogger.Log("stacked. " + disquuunDataStack.QueueCount());
                return disquuunDataStack;
            }
        }

        public void OnReloaded(DisquuunSocket reloadedSocket)
        {
            lock (poolLock)
            {
                if (disquuunDataStack.IsQueued())
                {
                    if (reloadedSocket.IsAvailable())
                    {
                        reloadedSocket.SetBusy();

                        var commandAndData = disquuunDataStack.Dequeue();
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
            lock (poolLock)
            {
                var availableSocketCount = 0;
                for (var i = 0; i < sockets.Length; i++)
                {
                    var socket = sockets[i];
                    if (socket == null)
                    {
                        // not yet generated.
                        continue;
                    }
                    if (socket.IsAvailable())
                    {
                        availableSocketCount++;
                    }
                }
                return availableSocketCount;
            }
        }

        public int StackedCommandCount()
        {
            lock (poolLock)
            {
                return disquuunDataStack.QueueCount();
            }
        }
    }
}