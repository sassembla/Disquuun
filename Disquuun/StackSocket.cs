
using System;
using System.Collections.Generic;

namespace DisquuunCore
{
    public class SocketBase
    {
        private object stackLockObject = new object();

        private Queue<StackCommandData> stackedDataQueue;

        public int QueueCount()
        {
            lock (stackLockObject)
            {
                return stackedDataQueue.Count;
            }
        }

        public bool IsQueued()
        {
            lock (stackLockObject)
            {
                if (0 < stackedDataQueue.Count)
                {
                    return true;
                }
                return false;
            }
        }
        public StackCommandData Dequeue()
        {
            lock (stackLockObject)
            {
                return stackedDataQueue.Dequeue();
            }
        }

        public SocketBase()
        {
            this.stackedDataQueue = new Queue<StackCommandData>();
        }

        public virtual DisquuunResult[] DEPRECATED_Sync(DisqueCommand command, byte[] data)
        {
            throw new Exception("deprecated & all sockets are using.");
        }

        public virtual void Async(Queue<DisqueCommand> commands, byte[] data, Func<DisqueCommand, DisquuunResult[], bool> Callback)
        {
            lock (stackLockObject)
            {
                this.stackedDataQueue.Enqueue(new StackCommandData(DisquuunExecuteType.ASYNC, commands, data, Callback));
            }
        }

        public virtual void Loop(Queue<DisqueCommand> commands, byte[] data, Func<DisqueCommand, DisquuunResult[], bool> Callback)
        {
            lock (stackLockObject)
            {
                this.stackedDataQueue.Enqueue(new StackCommandData(DisquuunExecuteType.LOOP, commands, data, Callback));
            }
        }

        public virtual void Execute(Queue<DisqueCommand> commands, byte[] wholeData, Func<DisqueCommand, DisquuunResult[], bool> Callback)
        {
            lock (stackLockObject)
            {
                this.stackedDataQueue.Enqueue(new StackCommandData(DisquuunExecuteType.PIPELINE, commands, wholeData, Callback));
            }
        }
    }
}