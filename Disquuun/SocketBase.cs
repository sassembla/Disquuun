
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DisquuunCore
{
    public class SocketBase
    {
        private Queue<StackCommandData> stackedDataQueue;
        private object baseLock;

        public void ReadyStack()
        {
            this.stackedDataQueue = new Queue<StackCommandData>();
            this.baseLock = new object();
        }

        public int QueueCount()
        {
            lock (baseLock)
            {
                return stackedDataQueue.Count;
            }
        }

        public bool IsQueued()
        {
            lock (baseLock)
            {
                if (0 < stackedDataQueue.Count)
                {
                    return true;
                }
            }
            return false;
        }

        public StackCommandData Dequeue()
        {
            lock (baseLock)
            {
                return stackedDataQueue.Dequeue();
            }
        }

        public virtual DisquuunResult[] DEPRECATED_Sync(DisqueCommand command, byte[] data)
        {
            throw new Exception("deprecated & all sockets are using.");
        }

        public virtual void Async(DisqueCommand[] commands, byte[] data, Func<DisqueCommand, DisquuunResult[], bool> Callback)
        {
            lock (baseLock)
            {
                this.stackedDataQueue.Enqueue(new StackCommandData(DisquuunExecuteType.ASYNC, commands, data, Callback));
            }
        }

        public virtual void Loop(DisqueCommand[] commands, byte[] data, Func<DisqueCommand, DisquuunResult[], bool> Callback)
        {
            lock (baseLock)
            {
                this.stackedDataQueue.Enqueue(new StackCommandData(DisquuunExecuteType.LOOP, commands, data, Callback));
            }
        }

        public virtual void Execute(DisqueCommand[] commands, byte[] wholeData, Func<DisqueCommand, DisquuunResult[], bool> Callback)
        {
            lock (baseLock)
            {
                this.stackedDataQueue.Enqueue(new StackCommandData(DisquuunExecuteType.PIPELINE, commands, wholeData, Callback));
            }
        }
    }
}
