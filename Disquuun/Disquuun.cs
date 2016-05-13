using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace DisquuunCore {
    public enum DisqueCommand {		
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
	public class DisquuunInput	{
		public readonly DisqueCommand command;
		public readonly byte[] data;
		public readonly DisquuunSocket socket;
		
		public DisquuunInput (DisqueCommand command, byte[] data, DisquuunSocket socket) {
			this.command = command;
			this.data = data;
			this.socket = socket;
		}
	}
	
	/**
		data structure for result.
	*/
	public struct DisquuunResult {
		public byte[][] bytesArray;
		
		public DisquuunResult (params byte[][] bytesArray) {
			this.bytesArray = bytesArray;
		}
	}
	
    public class Disquuun {
		public readonly string connectionId;
		
		public readonly long BufferSize;
		public readonly IPEndPoint endPoint;
		
		public ConnectionState connectionState;
		
		
		private readonly Action<Exception> ConnectionFailed;
		
		private List<DisquuunSocket> socketPool;
		
		public enum ConnectionState {
			OPENED,
			ALLCLOSING,
			ALLCLOSED
		}
		
		
		public Disquuun (
			string host,
			int port,
			long bufferSize,
			int maxConnectionCount,
			Action<Exception> ConnectionFailed=null
		) {
			this.connectionId = Guid.NewGuid().ToString();
			
			this.BufferSize = bufferSize;
			this.endPoint = new IPEndPoint(IPAddress.Parse(host), port);
			
			this.connectionState = ConnectionState.ALLCLOSED;
			
			/*
				set handlers for connection error.
				other runtime errors will emit in API handler.
			*/
			this.ConnectionFailed = ConnectionFailed;
			
			socketPool = new List<DisquuunSocket>();
			for (var i = 0; i < maxConnectionCount; i++) {
				var socketObj = new DisquuunSocket(endPoint, bufferSize, OnSocketConnectionFailed);
				socketPool.Add(socketObj);
			}
		}
		
		
		private void OnSocketConnectionFailed (DisquuunSocket source, Exception e) {
			lock (socketPool) {
				socketPool.Remove(source);
				if (ConnectionFailed != null) ConnectionFailed(e); 
			}
			
			UpdateState();
		}
		
		public void UpdateState () {
			lock (socketPool) {
				var connectionCount = socketPool
					.Where(s => s.State() == DisquuunSocket.SocketState.OPENED)
					.ToArray().Length;
				
				switch (connectionCount) {
					case 0: {
						connectionState = ConnectionState.ALLCLOSED;
						break;
					}
					default: {// 1 or more socket opened.
						connectionState = ConnectionState.OPENED;
						break;
					}
				}
			}
		}
		
		public ConnectionState State () {
			UpdateState();
			return connectionState;
		}
		
		
		public void Disconnect (bool force=false) {
			connectionState = ConnectionState.ALLCLOSING;
			foreach (var socket in socketPool) socket.Disconnect(force);
		}
		
		/*
			API gateway
		*/
		public DisquuunInput AddJob (string queueName, byte[] data, int timeout=0, params object[] args) {
			var bytes = DisquuunAPI.AddJob(queueName, data, timeout, args);
			return null;
		}
		
		public DisquuunInput GetJob (string[] queueIds, params object[] args) {
			// return DisquuunAPI.GetJob(queueIds, args);
			return null;
		}
		
		public DisquuunInput AckJob (string[] jobIds) {
			// return DisquuunAPI.AckJob(jobIds);
			return null;
		}

		public DisquuunInput FastAck (string[] jobIds) {
			// return DisquuunAPI.FastAck(jobIds);
			return null;
		}

		public DisquuunInput Working (string jobId) {
			// return DisquuunAPI.Working(jobId);
			return null;
		}

		public DisquuunInput Nack (string[] jobIds) {
			// return DisquuunAPI.Nack(jobIds);
			return null;
		}
		
		public DisquuunInput Info () {
			var data = DisquuunAPI.Info();
			
			TestLogger.Log("busyじゃないSocketを探して渡す。この部分が重そうだな〜〜ガトリングガンみたいな感じに次を用意しとくか。");
			var socket = socketPool[0];
			
			return new DisquuunInput(DisqueCommand.INFO, data, socket);
		}
		
		public DisquuunInput Hello () {
			// return DisquuunAPI.Hello();
			return null;
		}
		
		public DisquuunInput Qlen (string queueId) {
			// return DisquuunAPI.Qlen(queueId);
			return null;
		}
		
		/*
			QSTAT,// <queue-name>
			QPEEK,// <queue-name> <count>
			ENQUEUE,// <job-id> ... <job-id>
			DEQUEUE,// <job-id> ... <job-id>
			DELJOB,// <job-id> ... <job-id>
			SHOW,// <job-id>
			QSCAN,// [COUNT <count>] [BUSYLOOP] [MINLEN <len>] [MAXLEN <len>] [IMPORTRATE <rate>]
			JSCAN,// [<cursor>] [COUNT <count>] [BUSYLOOP] [QUEUE <queue>] [STATE <state1> STATE <state2> ... STATE <stateN>] [REPLY all|id]
			PAUSE,// <queue-name> option1 [option2 ... optionN]
		*/
		
		
		
		public static void Log (string message) {
			TestLogger.Log(message);
		}
	}
}