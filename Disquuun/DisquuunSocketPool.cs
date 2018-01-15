
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace DisquuunCore
{
    public class DisquuunSocketPool
    {
        private Hashtable sockets;

        private SocketBase disquuunDataStack;

        private object poolLock = new object();

        private readonly IPEndPoint endPoint;
        private readonly long bufferSize;

        public DisquuunSocketPool(
            int defaultConnectionCount,
            Action<DisquuunSocket, int> OnSocketOpened,
            Action<DisquuunSocket, string, Exception> OnSocketConnectionFailed,
            Func<int, bool> OnSocketShortage,
            IPEndPoint endPoint,
            long bufferSize)
        {
            this.disquuunDataStack = new SocketBase();
            disquuunDataStack.ReadyStack();
            if (OnSocketShortage != null)
            {
                this.OnSocketShortage = OnSocketShortage;
            }

            this.sockets = new Hashtable();
            for (var i = 0; i < defaultConnectionCount; i++)
            {
                this.sockets.Add(i, new DisquuunSocket(i, OnSocketOpened, this.OnReloaded, OnSocketConnectionFailed));
            }

            this.endPoint = endPoint;
            this.bufferSize = bufferSize;
        }
        private Func<int, bool> OnSocketShortage;

        public void Connect()
        {
            for (var i = 0; i < sockets.Count; i++)
            {
                ((DisquuunSocket)this.sockets[i]).Connect(endPoint, bufferSize);
            }
        }

        public void Disconnect()
        {
            lock (poolLock)
            {
                for (var i = 0; i < sockets.Count; i++)
                {
                    var socket = (DisquuunSocket)sockets[i];
                    socket.Disconnect();
                }
            }
        }
        private int alertedSocketCount;

        public SocketBase ChooseAvailableSocket()
        {
            lock (poolLock)
            {
                for (var i = 0; i < sockets.Count; i++)
                {
                    var socket = (DisquuunSocket)sockets[i];

                    if (socket.IsAvailable())
                    {
                        socket.SetBusy();
                        return socket;
                    }
                }

                // no socket available, stack.
                if (alertedSocketCount != sockets.Count)
                {
                    alertedSocketCount = sockets.Count;
                    if (OnSocketShortage != null)
                    {
                        var shouldAddSocket = OnSocketShortage(alertedSocketCount);
                        if (shouldAddSocket)
                        {
                            AddNewSocket();
                        }
                    }
                }

                return disquuunDataStack;
            }
        }

        private void AddNewSocket()
        {
            var newSock = new DisquuunSocket(
                -1,
                (newSocket, index) =>
                {
                    // connected, add to hashtable.
                    lock (poolLock)
                    {
                        var count = sockets.Count;
                        sockets.Add(count, newSocket);
                    }
                },
                this.OnReloaded,
                (newSocket, reason, err) => { }
            );

            newSock.Connect(endPoint, bufferSize);
        }

        public void OnReloaded(DisquuunSocket reloadedSocket)
        {
            // consume stacked command if need.
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
                for (var i = 0; i < sockets.Count; i++)
                {
                    var socket = (DisquuunSocket)sockets[i];
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