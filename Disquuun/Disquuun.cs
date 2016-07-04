using System;
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
		public ArraySegment<byte>[] bytesArray;
		
		public DisquuunResult (params ArraySegment<byte>[] bytesArray) {
			this.bytesArray = bytesArray;
		}
	}
	
    public class Disquuun {
		public readonly string connectionId;
		
		public readonly long bufferSize;
		public readonly IPEndPoint endPoint;
		
		public ConnectionState connectionState;
		
		
		private readonly Action<string> ConnectionOpened;
		private readonly Action<string, Exception> ConnectionFailed;
		private readonly Action ConnectionIncreased;
		
		private DisquuunSocket[] socketPool;
		private object lockObject = new object();
		
		private readonly int minConnectionCount;

		public enum ConnectionState {
			OPENED,
			ALLCLOSING,
			ALLCLOSED
		}
		
		public Disquuun (
			string host,
			int port,
			long bufferSize,
			int minConnectionCount,
			Action<string> ConnectionOpenedAct=null,
			Action<string, Exception> ConnectionFailedAct=null,
			Action ConnectionIncreasedAct=null
		) {
			this.connectionId = Guid.NewGuid().ToString();
			
			this.bufferSize = bufferSize;
			this.endPoint = new IPEndPoint(IPAddress.Parse(host), port);
			
			this.connectionState = ConnectionState.ALLCLOSED;
			
			/*
				ConnectionOpened handler treats all connections are opened.
			*/
			if (ConnectionOpenedAct != null) this.ConnectionOpened = ConnectionOpenedAct;
			else this.ConnectionOpened = conId => {};
			
			/*
				ConnectionFailed handler only treats connection error.
				
				other runtime errors will emit in API handler.
			*/
			if (ConnectionFailedAct != null) this.ConnectionFailed = ConnectionFailedAct;
			else this.ConnectionFailed = (info, e) => {};
			

			if (ConnectionIncreasedAct != null) this.ConnectionIncreased = ConnectionIncreasedAct;
			else this.ConnectionIncreased = () => {};

			this.minConnectionCount = minConnectionCount;

			socketPool = new DisquuunSocket[minConnectionCount];
			for (var i = 0; i < minConnectionCount; i++) socketPool[i] = new DisquuunSocket(endPoint, bufferSize, OnSocketOpened, OnSocketConnectionFailed, -(i+1));
		}
		
		private void OnSocketOpened (DisquuunSocket source, string socketId) {
			lock (lockObject) {
				var availableSocketCount = 0;
				for (var i = 0; i < socketPool.Length; i++) {
					var socket = socketPool[i];
					if (socket == null) return;
					if (socket.State() == DisquuunSocket.SocketState.OPENED) availableSocketCount++;
				}

				if (availableSocketCount == minConnectionCount) ConnectionOpened(connectionId);
			}
		}
		
		private void OnSocketConnectionFailed (DisquuunSocket source, string info, Exception e) {
			UpdateState();
			if (ConnectionFailed != null) ConnectionFailed("OnSocketConnectionFailed:" + info, e); 
		}
		
		public void UpdateState () {
			lock (lockObject) {
				var availableSocketCount = 0;
				for (var i = 0; i < socketPool.Length; i++) {
					var socket = socketPool[i];
					if (socket.State() == DisquuunSocket.SocketState.OPENED) availableSocketCount++;
				}
				
				switch (availableSocketCount) {
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
			lock (lockObject) {
				foreach (var socket in socketPool) socket.Disconnect(force);
			}
		}
		private int newSocketCount = 0;

		private DisquuunSocket ChooseAvailableSocket () {
			try {
			lock (lockObject) {
				for (var i = 0; i < socketPool.Length; i++) {
					var socket = socketPool[i];
					if (socket.IsChoosable()) {
						socket.SetBusy();
						return socket;
					}
				}
				
				newSocketCount++;
				return new DisquuunSocket(endPoint, bufferSize, OnSocketConnectionFailed, newSocketCount);
			}
			} catch (Exception e) {
				Disquuun.Log("ChooseAvailableSocket before error,", true);
				Disquuun.Log("ChooseAvailableSocket e:" + e.Message, true);
				throw e;
			}
		}
		
		
		
		
		/*
			Disque API gateway
		*/
		public DisquuunInput AddJob (string queueName, byte[] data, int timeout=0, params object[] args) {
			var bytes = DisquuunAPI.AddJob(queueName, data, timeout, args);
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.ADDJOB, bytes, socket);
		}
		
		public DisquuunInput GetJob (string[] queueIds, params object[] args) {
			var bytes = DisquuunAPI.GetJob(queueIds, args);
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.GETJOB, bytes, socket);
		}
		
		public DisquuunInput AckJob (string[] jobIds) {
			var bytes = DisquuunAPI.AckJob(jobIds);
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.ACKJOB, bytes, socket);
		}

		public DisquuunInput FastAck (string[] jobIds) {
			var bytes = DisquuunAPI.FastAck(jobIds);
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.FASTACK, bytes, socket);
		}

		public DisquuunInput Working (string jobId) {
			var bytes = DisquuunAPI.Working(jobId);
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.WORKING, bytes, socket);
		}

		public DisquuunInput Nack (string[] jobIds) {
			var bytes = DisquuunAPI.Nack(jobIds);
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.NACK, bytes, socket);
		}
		
		public DisquuunInput Info () {
			var data = DisquuunAPI.Info();
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.INFO, data, socket);
		}
		
		public DisquuunInput Hello () {
			var bytes = DisquuunAPI.Hello();
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.HELLO, bytes, socket);
		}
		
		public DisquuunInput Qlen (string queueId) {
			var bytes = DisquuunAPI.Qlen(queueId);
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.QLEN, bytes, socket);
		}
		
		public DisquuunInput Qstat (string queueId) {
			var bytes = DisquuunAPI.Qstat(queueId);
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.QSTAT, bytes, socket);
		}
		
		public DisquuunInput Qpeek (string queueId, int count) {
			var bytes = DisquuunAPI.Qpeek(queueId, count);
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.QPEEK, bytes, socket);
		}
		
		public DisquuunInput Enqueue (params string[] jobIds) {
			var bytes = DisquuunAPI.Enqueue(jobIds);
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.ENQUEUE, bytes, socket);
		}
		
		public DisquuunInput Dequeue (params string[] jobIds) {
			var bytes = DisquuunAPI.Dequeue(jobIds);
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.DEQUEUE, bytes, socket);
		}
		
		public DisquuunInput DelJob (params string[] jobIds) {
			var bytes = DisquuunAPI.DelJob(jobIds);
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.DELJOB, bytes, socket);
		}
		
		public DisquuunInput Show (string jobId) {
			var bytes = DisquuunAPI.Show(jobId);
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.SHOW, bytes, socket);
		}
		
		public DisquuunInput Qscan (params object[] args) {
			var bytes = DisquuunAPI.Qscan(args);
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.QSCAN, bytes, socket);
		}
		
		public DisquuunInput Jscan (int cursor=0, params object[] args) {
			var bytes = DisquuunAPI.Jscan(cursor, args);
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.JSCAN, bytes, socket);
		}
		
		public DisquuunInput Pause (string queueId, string option1, params string[] options) {
			var bytes = DisquuunAPI.Pause(queueId, option1, options);
			
			var socket = ChooseAvailableSocket();
			
			return new DisquuunInput(DisqueCommand.PAUSE, bytes, socket);
		}
		
		public static void Log (string message, bool write=false) {
			TestLogger.Log(message, write);
		}
	}
}
