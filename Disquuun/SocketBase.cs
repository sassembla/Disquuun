
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DisquuunCore
{
    public class SocketBase
    {
        private ConcurrentQueue<StackCommandData> stackedDataQueue;

        public void ReadyStack()
        {
            this.stackedDataQueue = new ConcurrentQueue<StackCommandData>();
        }

        public int QueueCount()
        {
            return stackedDataQueue.Count;
        }

        public bool IsQueued()
        {
            if (0 < stackedDataQueue.Count)
            {
                return true;
            }
            return false;
        }

        public StackCommandData Dequeue()
        {
            StackCommandData data;
            stackedDataQueue.TryDequeue(out data);
            return data;
        }

        public virtual DisquuunResult[] DEPRECATED_Sync(DisqueCommand command, byte[] data)
        {
            throw new Exception("deprecated & all sockets are using.");
        }

        public virtual void Async(DisqueCommand[] commands, byte[] data, Func<DisqueCommand, DisquuunResult[], bool> Callback)
        {
            this.stackedDataQueue.Enqueue(new StackCommandData(DisquuunExecuteType.ASYNC, commands, data, Callback));
        }

        public virtual void Loop(DisqueCommand[] commands, byte[] data, Func<DisqueCommand, DisquuunResult[], bool> Callback)
        {
            this.stackedDataQueue.Enqueue(new StackCommandData(DisquuunExecuteType.LOOP, commands, data, Callback));
        }

        public virtual void Execute(DisqueCommand[] commands, byte[] wholeData, Func<DisqueCommand, DisquuunResult[], bool> Callback)
        {
            this.stackedDataQueue.Enqueue(new StackCommandData(DisquuunExecuteType.PIPELINE, commands, wholeData, Callback));
        }
    }
}