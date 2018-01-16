using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace DisquuunCore
{
    public enum DisqueCommand
    {
        ADDJOB,// queue_name job <ms-timeout> [REPLICATE <count>] [DELAY <sec>] [RETRY <sec>] [TTL <sec>] [MAXLEN <count>] [ASYNC]
        GETJOB,// [NOHANG] [TIMEOUT <ms-timeout>] [COUNT <count>] [WITHCOUNTERS] FROM queue1 queue2 ... queueN
        ACKJOB,// jobid1 jobid2 ... jobidN
        FASTACK,// jobid1 jobid2 ... jobidN
        WORKING,// jobid
        NACK,// <job-id> ... <job-id>
        INFO,
        HELLO,
        QLEN,// <queue-name>
        QSTAT,// <queue-name>
        QPEEK,// <queue-name> <count>
        ENQUEUE,// <job-id> ... <job-id>
        DEQUEUE,// <job-id> ... <job-id>
        DELJOB,// <job-id> ... <job-id>
        SHOW,// <job-id>
        QSCAN,// [COUNT <count>] [BUSYLOOP] [MINLEN <len>] [MAXLEN <len>] [IMPORTRATE <rate>]
        JSCAN,// [<cursor>] [COUNT <count>] [BUSYLOOP] [QUEUE <queue>] [STATE <state1> STATE <state2> ... STATE <stateN>] [REPLY all|id]
        PAUSE,// <queue-name> option1 [option2 ... optionN]
    }

    /**
		data structure for input.
	*/
    public class DisquuunInput
    {
        public readonly DisqueCommand command;
        public readonly byte[] data;
        public readonly DisquuunSocketPool socketPool;

        public DisquuunInput(DisqueCommand command, byte[] data, DisquuunSocketPool socketPool)
        {
            this.command = command;
            this.data = data;
            this.socketPool = socketPool;
        }
    }


    /**
		data structure for result.
	*/
    public struct DisquuunResult
    {
        public ArraySegment<byte>[] bytesArray;

        public DisquuunResult(params ArraySegment<byte>[] bytesArray)
        {
            this.bytesArray = bytesArray;
        }
    }

    public enum DisquuunExecuteType
    {
        ASYNC,
        LOOP,
        PIPELINE
    }

    public class Disquuun
    {
        public readonly string connectionId;

        public readonly IPEndPoint endPoint;

        public ConnectionState connectionState;

        public readonly long bufferSize;
        private readonly Action<string> ConnectionOpened;
        private readonly Action<string, Exception> ConnectionFailed;

        private DisquuunSocketPool socketPool;

        private object lockObject = new object();

        public enum ConnectionState
        {
            OPENING,
            OPENED,
            OPENED_RECOVERING,
            ALLCLOSING,
            ALLCLOSED
        }
        private Hashtable defaultConnectionIndexies;
        public Disquuun(
            string host,
            int port,
            long bufferSize,
            int defaultConnectionCount,
            Action<string> ConnectionOpenedAct = null,
            Action<string, Exception> ConnectionFailedAct = null,
            Func<int, Tuple<bool, int>> OnSocketShortage = null
        )
        {
            this.connectionId = Guid.NewGuid().ToString();

            this.endPoint = new IPEndPoint(IPAddress.Parse(host), port);
            this.bufferSize = bufferSize;

            this.connectionState = ConnectionState.OPENING;

            /*
				ConnectionOpened handler treats all connections are opened.
			*/
            if (ConnectionOpenedAct != null)
            {
                this.ConnectionOpened = ConnectionOpenedAct;
            }
            else
            {
                this.ConnectionOpened = conId => { };
            }

            /*
				ConnectionFailed handler only treats connection error.
				
				other runtime errors will emit in API handler.
			*/
            if (ConnectionFailedAct != null)
            {
                this.ConnectionFailed = ConnectionFailedAct;
            }
            else
            {
                this.ConnectionFailed = (info, e) => { };
            }

            defaultConnectionIndexies = new Hashtable();
            for (var i = 0; i < defaultConnectionCount; i++)
            {
                defaultConnectionIndexies.Add(i, false);
            }

            this.socketPool = new DisquuunSocketPool(
                defaultConnectionCount,
                this.OnSocketOpened,
                this.OnSocketConnectionFailed,
                OnSocketShortage,
                endPoint,
                bufferSize
            );

            this.socketPool.Connect();
        }

        public int StackedCommandCount()
        {
            return socketPool.StackedCommandCount();
        }

        private void OnSocketOpened(DisquuunSocket source, int socketIndex)
        {
            if (!(bool)defaultConnectionIndexies[socketIndex])
            {
                lock (lockObject)
                {
                    defaultConnectionIndexies[socketIndex] = true;
                }

                for (var i = 0; i < defaultConnectionIndexies.Count; i++)
                {
                    if (!(bool)defaultConnectionIndexies[i])
                    {
                        return;
                    }
                }
            }

            connectionState = ConnectionState.OPENED;
            ConnectionOpened(connectionId);
        }

        private void OnSocketConnectionFailed(DisquuunSocket source, string info, Exception e)
        {
            connectionState = ConnectionState.OPENED_RECOVERING;
            DisquuunLogger.Log("OnSocketConnectionFailedで、失敗原因にかんれんして、可能であれば動作を継続する必要がありそう。info:" + info + " error:" + e);

            if (ConnectionFailed != null)
            {
                ConnectionFailed("OnSocketConnectionFailed:" + info, e);
            }
        }

        public ConnectionState State()
        {
            return connectionState;
        }

        public void Disconnect()
        {
            connectionState = ConnectionState.ALLCLOSING;
            socketPool.Disconnect();
        }

        public int AvailableSocketNum()
        {
            return socketPool.AvailableSocketNum();
        }


        /*
            Disque API gateway
        */
        public DisquuunInput AddJob(string queueName, byte[] data, int timeout = 0, params object[] args)
        {
            var bytes = DisquuunAPI.AddJob(queueName, data, timeout, args);

            return new DisquuunInput(DisqueCommand.ADDJOB, bytes, socketPool);
        }

        public DisquuunInput GetJob(string[] queueIds, params object[] args)
        {
            var bytes = DisquuunAPI.GetJob(queueIds, args);

            return new DisquuunInput(DisqueCommand.GETJOB, bytes, socketPool);
        }

        public DisquuunInput AckJob(string[] jobIds)
        {
            var bytes = DisquuunAPI.AckJob(jobIds);

            return new DisquuunInput(DisqueCommand.ACKJOB, bytes, socketPool);
        }

        public DisquuunInput FastAck(string[] jobIds)
        {
            var bytes = DisquuunAPI.FastAck(jobIds);

            return new DisquuunInput(DisqueCommand.FASTACK, bytes, socketPool);
        }

        public DisquuunInput Working(string jobId)
        {
            var bytes = DisquuunAPI.Working(jobId);

            return new DisquuunInput(DisqueCommand.WORKING, bytes, socketPool);
        }

        public DisquuunInput Nack(string[] jobIds)
        {
            var bytes = DisquuunAPI.Nack(jobIds);

            return new DisquuunInput(DisqueCommand.NACK, bytes, socketPool);
        }

        public DisquuunInput Info()
        {
            var data = DisquuunAPI.Info();

            return new DisquuunInput(DisqueCommand.INFO, data, socketPool);
        }

        public DisquuunInput Hello()
        {
            var bytes = DisquuunAPI.Hello();

            return new DisquuunInput(DisqueCommand.HELLO, bytes, socketPool);
        }

        public DisquuunInput Qlen(string queueId)
        {
            var bytes = DisquuunAPI.Qlen(queueId);

            return new DisquuunInput(DisqueCommand.QLEN, bytes, socketPool);
        }

        public DisquuunInput Qstat(string queueId)
        {
            var bytes = DisquuunAPI.Qstat(queueId);

            return new DisquuunInput(DisqueCommand.QSTAT, bytes, socketPool);
        }

        public DisquuunInput Qpeek(string queueId, int count)
        {
            var bytes = DisquuunAPI.Qpeek(queueId, count);

            return new DisquuunInput(DisqueCommand.QPEEK, bytes, socketPool);
        }

        public DisquuunInput Enqueue(params string[] jobIds)
        {
            var bytes = DisquuunAPI.Enqueue(jobIds);

            return new DisquuunInput(DisqueCommand.ENQUEUE, bytes, socketPool);
        }

        public DisquuunInput Dequeue(params string[] jobIds)
        {
            var bytes = DisquuunAPI.Dequeue(jobIds);

            return new DisquuunInput(DisqueCommand.DEQUEUE, bytes, socketPool);
        }

        public DisquuunInput DelJob(params string[] jobIds)
        {
            var bytes = DisquuunAPI.DelJob(jobIds);

            return new DisquuunInput(DisqueCommand.DELJOB, bytes, socketPool);
        }

        public DisquuunInput Show(string jobId)
        {
            var bytes = DisquuunAPI.Show(jobId);

            return new DisquuunInput(DisqueCommand.SHOW, bytes, socketPool);
        }

        public DisquuunInput Qscan(params object[] args)
        {
            var bytes = DisquuunAPI.Qscan(args);

            return new DisquuunInput(DisqueCommand.QSCAN, bytes, socketPool);
        }

        public DisquuunInput Jscan(int cursor = 0, params object[] args)
        {
            var bytes = DisquuunAPI.Jscan(cursor, args);

            return new DisquuunInput(DisqueCommand.JSCAN, bytes, socketPool);
        }

        public DisquuunInput Pause(string queueId, string option1, params string[] options)
        {
            var bytes = DisquuunAPI.Pause(queueId, option1, options);

            return new DisquuunInput(DisqueCommand.PAUSE, bytes, socketPool);
        }

        /*
            pipelines
        */
        private List<List<DisquuunInput>> pipelineStack = new List<List<DisquuunInput>>();
        private int currentPipelineIndex = -1;

        public List<List<DisquuunInput>> Pipeline(params DisquuunInput[] disquuunInput)
        {
            lock (lockObject)
            {
                if (0 < disquuunInput.Length)
                {
                    if (pipelineStack.Count == 0)
                    {
                        currentPipelineIndex = 0;
                    }

                    if (pipelineStack.Count < currentPipelineIndex + 1)
                    {
                        pipelineStack.Add(new List<DisquuunInput>());
                    }
                    pipelineStack[currentPipelineIndex].AddRange(disquuunInput);
                }
                return pipelineStack;
            }
        }

        public void RevolvePipeline()
        {
            lock (lockObject)
            {
                if (currentPipelineIndex == -1)
                {
                    return;
                }
                if (pipelineStack.Count == 0)
                {
                    return;
                }

                if (0 < pipelineStack[currentPipelineIndex].Count)
                {
                    currentPipelineIndex++;
                }
            }
        }
    }

    public static class DisquuunLogger
    {
        public static StringBuilder builder = new StringBuilder();
        const string header = "disquuun_log:";
        public static void Log(string message, bool write = false)
        {
            // TestLogger.Log(message, write);
            // Console.WriteLine("log:" + message);

            builder.AppendLine(header + message);

            if (write)
            {
                using (var sw = new StreamWriter("/Users/passepied/Desktop/Disquuun/log", true))
                {
                    sw.WriteLine(builder.ToString());
                }
                builder.Clear();
            }
        }
    }
}
