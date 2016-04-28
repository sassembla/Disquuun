using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using XrossPeerUtility;

public class Disquuun {
	public readonly string connectionId;
	
	private readonly Action<string> Connected;
	private readonly Action<DisqueCommand, byte[], byte[]> Received;
	private readonly Action<DisqueCommand, byte[]> Failed;
	private readonly Action<Exception> Error;
	private readonly Action<string> Closed;
	
	public enum ConnectionState {
		OPENING,
		OPENED,
		CLOSING,
		CLOSED
	}
	
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
	
	private SocketToken socketToken;
	
	public struct SocketToken {
		public ConnectionState state;
		
		public readonly Socket socket;
		
		public readonly SocketAsyncEventArgs connectArgs;
		public readonly SocketAsyncEventArgs sendArgs;
		public readonly SocketAsyncEventArgs receiveArgs;
		
		public Queue<DisqueCommand> stack;
		
		public SocketToken (Socket socket, SocketAsyncEventArgs connectArgs, SocketAsyncEventArgs sendArgs, SocketAsyncEventArgs receiveArgs) {
			this.state = ConnectionState.OPENING;
			this.socket = socket;
			
			this.connectArgs = connectArgs;
			this.sendArgs = sendArgs;
			this.receiveArgs = receiveArgs;
			
			this.stack = new Queue<DisqueCommand>();
			
			this.connectArgs.UserToken = this;
			this.sendArgs.UserToken = this;
			this.receiveArgs.UserToken = this;
		}
	}
	
	
	public Disquuun (
		string connectionId,
		string host,
		int port,
		long bufferSize,
		Action<string> Connected=null,
		Action<DisqueCommand, byte[], byte[]> Received=null,
		Action<DisqueCommand, byte[]> Failed=null,
		Action<Exception> Error=null,
		Action<string> Closed=null
	) {
		this.connectionId = connectionId;
		
		this.Connected = Connected;
		this.Received = Received;
		this.Failed = Failed;
		this.Error = Error;
		this.Closed = Closed;
		
		var endPoint = new IPEndPoint(IPAddress.Parse(host), port);
		var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		
		{
			var connectArgs = new SocketAsyncEventArgs();
			connectArgs.AcceptSocket = clientSocket;
			connectArgs.RemoteEndPoint = endPoint;
			connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnected);
			
			var sendArgs = new SocketAsyncEventArgs();
			sendArgs.AcceptSocket = clientSocket;
			sendArgs.RemoteEndPoint = endPoint;
			sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSend);
			
			var receiveArgs = new SocketAsyncEventArgs();
			byte[] receiveBuffer2 = new byte[bufferSize];
			receiveArgs.SetBuffer(receiveBuffer2, 0, receiveBuffer2.Length);
			receiveArgs.AcceptSocket = clientSocket;
			receiveArgs.RemoteEndPoint = endPoint;
			receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnReceived);
						
			socketToken = new SocketToken(clientSocket, connectArgs, sendArgs, receiveArgs);
			
			if (!clientSocket.ConnectAsync(socketToken.connectArgs)) OnConnected(clientSocket, connectArgs);
		}
	}
	
	/*
		handlers
	*/
	
	private void OnConnected (object unused, SocketAsyncEventArgs args) {
		var token = (SocketToken)args.UserToken;
		switch (token.state) {
			case ConnectionState.OPENING: {
				if (args.SocketError != SocketError.Success) {
					token.state = ConnectionState.CLOSED;
					var error = new Exception("connect error:" + args.SocketError.ToString());
					if (Error != null) Error(error);
					if (Closed != null) Closed(connectionId);
					return;
				}
				
				token.state = ConnectionState.OPENED;
				
				// ready receive data.
				token.socket.ReceiveAsync(token.receiveArgs);
				
				if (Connected != null) Connected(connectionId); 
				return;
			}
			default: {
				throw new Exception("unknown connect error:" + token.state);
			}
		}
	}
	
	private void OnClosed (object unused, SocketAsyncEventArgs args) {
		var token = (SocketToken)args.UserToken;
		switch (token.state) {
			case ConnectionState.CLOSED: {
				// do nothing.
				break;
			}
			default: {
				token.state = ConnectionState.CLOSED;
				if (Closed != null) Closed(this.connectionId);
				break;
			}
		}
	}
	
	private void OnSend (object unused, SocketAsyncEventArgs args) {
		var socketError = args.SocketError;
		switch (socketError) {
			case SocketError.Success: {
				// do nothing.
				break;
			}
			default: {
				if (Error != null) {
					var error = new Exception("send error:" + socketError.ToString());
					Error(error);
				}
				break;
			}
		}
	}
	
	private void OnReceived (object unused, SocketAsyncEventArgs args) {
		var token = (SocketToken)args.UserToken;
		
		// in Disque, 1 receive per 1 send at least.
		var command = token.stack.Dequeue();
		
		if (args.SocketError != SocketError.Success) { 
			switch (token.state) {
				case ConnectionState.CLOSING:
				case ConnectionState.CLOSED: {
					// already closing, ignore.
					return;
				}
				default: {
					// show error, then close or continue receiving.
					var error = new Exception("receive error:" + args.SocketError.ToString());
					if (Error != null) Error(error);
					
					// connection is already closed.
					if (!SocketConnected(token.socket)) {
						Disconnect();
						return;
					}
					
					// continue receiving data. go to below.
					break;
				}
			}
		}
		
		if (0 < args.BytesTransferred) {
			// ここで複数件受け取る可能性がある。
			// で、あれば、delimiterとかがここに入ってくる。データが切れることはあるのかな、、バッファオーバーしたらぶっちぎれるんだよな、、そのへんまずやってみるか。
			XrossPeer.TimeAssert(Develop.TIME_ASSERT, "複数件突っ込まれるのでは = データが複数入るのでは、、？ YES。 途中でちぎれるのでは、、? <- わからん、、");
			XrossPeer.TimeAssert(Develop.TIME_ASSERT, "データのもち越しどうなるんだろう");
			XrossPeer.TimeAssert(Develop.TIME_ASSERT, "ジョブの種類についてはDequeueで対応できるはず");
			
			var cursor = DisqueFilter.Evaluate(command, token.stack, args.BytesTransferred, args.Buffer, Received, Failed);
			
			XrossPeer.Log("args.BytesTransferred:" + args.BytesTransferred + " vs cursor:" + cursor);
		}
		
		// 同じデータが出るようになれば、末尾にデータが追加されたのが見れるんだと思うんだけど。
		// ここからoffsetを指定して追加とかできるんだろうか。三つくらいなら余裕で入りそうなもんなんだよな。
		// args.SetBuffer(0, args.BytesTransferred);
		
		
		// Debug.LogError("received! BufferList:" + args.BufferList);
		
		// // Debug.LogError("received! args:" + args.Completed);
		// Debug.LogError("received! Count:" + args.Count);
		// args.Count = 0;//x
		
		// var receiveArgs = new SocketAsyncEventArgs();
		
		// args.SetBuffer(0, 102400);
		// receiveArgs.UserToken = userToken;
		// receiveArgs.AcceptSocket = userToken.socket;
		// // receiveArgs.RemoteEndPoint = endPoint;
		
		// receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnIO);
		// userToken.receiveArgs = receiveArgs;
		// args.SetBuffer(0, args.Buffer.Length);
		
		token.socket.ReceiveAsync(args);
	}
	
	
	
	/*
		disque protocol symbols
	*/
	public enum CommandString {
		Error = '-',
		Status = '+',
		Bulk = '$',
		MultiBulk = '*',
		Int = ':'
	}
	
	/*
		chars
	*/
	public const char CharError = (char)CommandString.Error;
	public const char CharStatus = (char)CommandString.Status;
	public const char CharBulk = (char)CommandString.Bulk;
	public const char CharMultiBulk = (char)CommandString.MultiBulk;
	public const char CharInt = (char)CommandString.Int;
	public const string CharEOL = "\r\n";
	
	/*
		bytes
	*/
	public const byte ByteError		= 45;
	public const byte ByteStatus	= 43;
	public const byte ByteBulk		= 36;
	public const byte ByteMultiBulk	= 42;
	public const byte ByteInt		= 58;
	public static byte ByteEOL = Convert.ToByte('\n');
	
	
	public static Encoding enc = new UTF8Encoding(false);
	
	
	private static int ReadLine (byte[] bytes, int cursor) {
		do {
			if (bytes[cursor] == ByteEOL) break;
			cursor++;
		} while (cursor < bytes.Length);
		
		return cursor - 1;
	}
	
	/*
		仮に、フィルタとして定義してみる。
		このフィルタを通過する、みたいな形に収められると思う。staticになるんじゃねーかな。ハンドラもしない。
	*/
	public static class DisqueFilter {
		
		/*
			disque commandsをどうやって提供しようかな。
			stringを元に組み立てられる感じなんで、その機構ができれば良いんだけど、
			stringを持つの面倒臭いんだよな。あと、
			切り分けをどうするかっていうのがある。
			
			socketを扱う部分はあくまでも基礎で、その上でDisqueのコマンドを扱える、っていうのが大事。
		*/
		
		
		public static int Evaluate(DisqueCommand currentCommand, Queue<DisqueCommand> commands, int bytesTransferred, byte[] sourceBuffer, Action<DisqueCommand, byte[], byte[]> Received, Action<DisqueCommand, byte[]> Failed) {
			int cursor = 0;
			
			while (cursor < bytesTransferred) {
				if (0 < cursor && 0 < commands.Count) currentCommand = commands.Dequeue();
				
				XrossPeer.TimeAssert(Develop.TIME_ASSERT, "データの先頭しか受け取れないケースとかがありそうな気がする、発生を検知したい。");
				
				// ここでコマンドごとに不思議ちゃんになろう。
				
				switch (currentCommand) {
					case DisqueCommand.ADDJOB: {
						switch (sourceBuffer[cursor]) {
							case ByteStatus: {
								// +
								var lineEndCursor = ReadLine(sourceBuffer, cursor);
								cursor = cursor + 1;// add header byte size = 1.
								
								if (Received != null) {
									var idStr = enc.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
									// XrossPeer.Log("idStr:" + idStr);
									Received(currentCommand, enc.GetBytes(idStr), null);
								}
								
								cursor = lineEndCursor + 2;// CR + LF
								break;
							}
							case ByteError: {
								// -
								var lineEndCursor = ReadLine(sourceBuffer, cursor);
								cursor = cursor + 1;// add header byte size = 1.
								
								if (Received != null) {
									var errorStr = enc.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
									// XrossPeer.Log("errorStr:" + errorStr);
									Failed(currentCommand, enc.GetBytes(errorStr));
								}
								
								cursor = lineEndCursor + 2;// CR + LF
								break;
							}
							default: {
								break;
							}
						}
						
						cursor = bytesTransferred;
						break;
					}
					case DisqueCommand.GETJOB: {
						{
							// *
							var lineEndCursor = ReadLine(sourceBuffer, cursor);
							cursor = cursor + 1;// add header byte size = 1.
							
							var bulkCountStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
							var bulkCountNum = Convert.ToInt32(bulkCountStr);
							// XrossPeer.Log("bulkCountNum:" + bulkCountNum);
							cursor = lineEndCursor + 2;// CR + LF
							
							for (var i = 0; i < bulkCountNum; i++) {
								{
									// *
									var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
									cursor = cursor + 1;// add header byte size = 1.
									
									var bulkCountStr2 = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
									var bulkCountNum2 = Convert.ToInt32(bulkCountStr2);
									// XrossPeer.Log("bulkCountNum2:" + bulkCountNum2);
									cursor = lineEndCursor2 + 2;// CR + LF
								}
								
								// queueName
								{
									// $
									var lineEndCursor3 = ReadLine(sourceBuffer, cursor);
									cursor = cursor + 1;// add header byte size = 1.
									
									var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor3 - cursor);
									var strNum = Convert.ToInt32(countStr);
									// XrossPeer.Log("id strNum:" + strNum);
									
									cursor = lineEndCursor3 + 2;// CR + LF
									
									var nameStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
									// XrossPeer.Log("nameStr:" + nameStr);
									
									cursor = cursor + strNum + 2;// CR + LF
								}
								
								var jobIdIndex = 0;
								var jobIdLength = 0;
								
								// jobId
								{
									// $
									var lineEndCursor3 = ReadLine(sourceBuffer, cursor);
									cursor = cursor + 1;// add header byte size = 1.
									
									var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor3 - cursor);
									var strNum = Convert.ToInt32(countStr);
									// XrossPeer.Log("id strNum:" + strNum);
									
									cursor = lineEndCursor3 + 2;// CR + LF
									
									jobIdIndex = cursor;
									jobIdLength = strNum;
									// var jobIdSrt = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
									// XrossPeer.Log("jobIdSrt:" + jobIdSrt);
									
									cursor = cursor + strNum + 2;// CR + LF
								}
								
								// jobData
								{
									// $
									var lineEndCursor3 = ReadLine(sourceBuffer, cursor);
									cursor = cursor + 1;// add header byte size = 1.
									
									var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor3 - cursor);
									var strNum = Convert.ToInt32(countStr);
									// XrossPeer.Log("data strNum:" + strNum);
									
									cursor = lineEndCursor3 + 2;// CR + LF
									
									if (Received != null) {
										var jobIdBytes = new byte[jobIdLength];
										Array.Copy(sourceBuffer, jobIdIndex, jobIdBytes, 0, jobIdLength);
										
										var dataBytes = new byte[strNum];
										Array.Copy(sourceBuffer, cursor, dataBytes, 0, strNum);
										
										Received(currentCommand, jobIdBytes, dataBytes);
									}
									
									cursor = cursor + strNum + 2;// CR + LF
								}
							} 
						}
						break;
					}
					case DisqueCommand.ACKJOB:
					case DisqueCommand.FASTACK: {
						{
							// :Identity count
							var lineEndCursor = ReadLine(sourceBuffer, cursor);
							cursor = cursor + 1;// add header byte size = 1.
							
							if (Received != null) { 	
								// var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
								// var countNum = Convert.ToInt32(countStr);
								var countBuffer = new byte[lineEndCursor - cursor];
								Array.Copy(sourceBuffer, cursor, countBuffer, 0, countBuffer.Length);
								Received(currentCommand, countBuffer, null);	
							}
							
							// XrossPeer.Log(":countNum:" + countNum);
							cursor = lineEndCursor + 2;// CR + LF
						}
						break;
					}
					case DisqueCommand.WORKING: {
						XrossPeer.LogError("not yet applied:" + currentCommand);
						cursor = bytesTransferred;
						break;
					}
					case DisqueCommand.NACK: {
						XrossPeer.LogError("not yet applied:" + currentCommand);
						cursor = bytesTransferred;
						break;
					}
					case DisqueCommand.INFO: {
						// $
						var lineEndCursor = ReadLine(sourceBuffer, cursor);
						cursor = cursor + 1;// add header byte size = 1.
						
						var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
						var countNum = Convert.ToInt32(countStr);
						
						cursor = lineEndCursor + 2;// CR + LF
						
						if (Received != null) {
							var newBuffer = new byte[countNum];
							// var dataStr = Encoding.UTF8.GetString(args.Buffer, cursor, countNum);	
							Array.Copy(sourceBuffer, cursor, newBuffer, 0, countNum);
							Received(currentCommand, newBuffer, null);
						}
						
						cursor = cursor + countNum + 2;// CR + LF
						break;
					}
					case DisqueCommand.HELLO: {
						var strBuilder = new StringBuilder();
						{
							// *
							var lineEndCursor = ReadLine(sourceBuffer, cursor);
							cursor = cursor + 1;// add header byte size = 1.
							
							var bulkCountStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
							var bulkCountNum = Convert.ToInt32(bulkCountStr);
							// XrossPeer.Log("bulkCountNum:" + bulkCountNum);
							cursor = lineEndCursor + 2;// CR + LF
						}
						
						{
							// :Identity count
							var lineEndCursor = ReadLine(sourceBuffer, cursor);
							cursor = cursor + 1;// add header byte size = 1.
							
							var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
							var countNum = Convert.ToInt32(countStr);
							// XrossPeer.Log(":countNum:" + countNum);
							cursor = lineEndCursor + 2;// CR + LF
						}
						
						{
							// $
							var lineEndCursor = ReadLine(sourceBuffer, cursor);
							cursor = cursor + 1;// add header byte size = 1.
							
							var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
							var strNum = Convert.ToInt32(countStr);
							// XrossPeer.Log("id strNum:" + strNum);
							
							cursor = lineEndCursor + 2;// CR + LF
							
							var idStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
							strBuilder.Append(idStr + "\n");
							// XrossPeer.Log("idStr:" + idStr);
							
							cursor = cursor + strNum + 2;// CR + LF
						}
						
						{
							// *
							var lineEndCursor = ReadLine(sourceBuffer, cursor);
							cursor = cursor + 1;// add header byte size = 1.
							
							var bulkCountStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
							var bulkCountNum = Convert.ToInt32(bulkCountStr);
							// XrossPeer.Log("bulkCountNum:" + bulkCountNum);
							cursor = lineEndCursor + 2;// CR + LF
							
							
							for (var i = 0; i < bulkCountNum; i++) {
								// $
								var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
								cursor = cursor + 1;// add header byte size = 1.
								
								var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
								var strNum = Convert.ToInt32(countStr);
								// XrossPeer.Log("id strNum:" + strNum);
								
								cursor = lineEndCursor2 + 2;// CR + LF
								
								var idStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
								strBuilder.Append(idStr + "\n");
								// XrossPeer.Log("idStr:" + idStr);
								
								cursor = cursor + strNum + 2;// CR + LF
							}
						}
						if (Received != null) {
							var newBuffer = enc.GetBytes(strBuilder.ToString());
							Received(currentCommand, newBuffer, null);
						} 
						break;
					}
					case DisqueCommand.QLEN: {
						XrossPeer.LogError("not yet applied:" + currentCommand);
						cursor = bytesTransferred;
						break;
					}
					case DisqueCommand.QSTAT: {
						XrossPeer.LogError("not yet applied:" + currentCommand);
						cursor = bytesTransferred;
						break;
					}
					case DisqueCommand.QPEEK: {
						XrossPeer.LogError("not yet applied:" + currentCommand);
						cursor = bytesTransferred;
						break;
					}
					case DisqueCommand.ENQUEUE: {
						XrossPeer.LogError("not yet applied:" + currentCommand);
						cursor = bytesTransferred;
						break;
					}
					case DisqueCommand.DEQUEUE: {
						XrossPeer.LogError("not yet applied:" + currentCommand);
						cursor = bytesTransferred;
						break;
					}
					case DisqueCommand.DELJOB: {
						XrossPeer.LogError("not yet applied:" + currentCommand);
						cursor = bytesTransferred;
						break;
					}
					case DisqueCommand.SHOW: {
						XrossPeer.LogError("not yet applied:" + currentCommand);
						cursor = bytesTransferred;
						break;
					}
					case DisqueCommand.QSCAN: {
						XrossPeer.LogError("not yet applied:" + currentCommand);
						cursor = bytesTransferred;
						break;
					}
					case DisqueCommand.JSCAN: {
						XrossPeer.LogError("not yet applied:" + currentCommand);
						cursor = bytesTransferred;
						break;
					}
					case DisqueCommand.PAUSE: {
						XrossPeer.LogError("not yet applied:" + currentCommand);
						cursor = bytesTransferred;
						break;
					}
					default: {
						XrossPeer.Log("unknown command:" + currentCommand);
						break;
					}
				}
			}
            return cursor;
        }
    }
	
	
	/*
		Disque commands.
	*/
	
	public void AddJob (string queueName, byte[] data, int timeout=0, params object[] args) {
		// ADDJOB queue_name job <ms-timeout> 
		// [REPLICATE <count>] [DELAY <sec>] [RETRY <sec>] [TTL <sec>] [MAXLEN <count>] [ASYNC]
		XrossPeer.Log("byteをそのまま送りたいんだが、っていうやつ。byteArrayをそのままではうまく変形できない。");
		var dataStr = enc.GetString(data);
		SendBytes(DisqueCommand.ADDJOB, queueName, dataStr, timeout);
	}
	
	public void GetJob (string[] queueIds, params object[] args) {
		// [NOHANG] [TIMEOUT <ms-timeout>] [COUNT <count>] [WITHCOUNTERS] 
		// FROM queue1 queue2 ... queueN
		var parameters = new object[args.Length + 1 + queueIds.Length];
		for (var i = 0; i < parameters.Length; i++) {
			if (i < args.Length) {
				parameters[i] = args[i];
				continue;
			}
			if (i == args.Length) {
				parameters[i] = "FROM";
				continue;
			}
			parameters[i] = queueIds[i - (args.Length + 1)];
		}
		SendBytes(DisqueCommand.GETJOB, parameters);
	}
	
	public void AckJob (string[] jobIds) {
		// jobid1 jobid2 ... jobidN
		SendBytes(DisqueCommand.ACKJOB, jobIds);
	}

	public void FastAck (string[] jobIds) {
		// jobid1 jobid2 ... jobidN
		SendBytes(DisqueCommand.FASTACK, jobIds);
	}

	public void Working (string jobId) {
		// jobid
	}

	public void Nack (params string[] jobIds) {
		// <job-id> ... <job-id>	
	}
	
	public void Info () {
		XrossPeer.TimeAssert(Develop.TIME_ASSERT, "stringはあとでなんとかするとして");
		SendBytes(DisqueCommand.INFO);
	}
	
	public void Hello () {
		SendBytes(DisqueCommand.HELLO);
	}
	
	
	
	
	private void SendBytes (DisqueCommand commandEnum, params object[] args) {
		int length = 1 + args.Length;
		
		var command = commandEnum.ToString();
		string strCommand;
		
		XrossPeer.TimeAssert(Develop.TIME_ASSERT, "自前のbyte memory streamを使うかな。stringBuffer重たいんで使いたくない。あとでベンチ。");
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(CharMultiBulk).Append(length).Append(CharEOL);
			
			sb.Append(CharBulk).Append(enc.GetByteCount(command)).Append(CharEOL).Append(command).Append(CharEOL);

			XrossPeer.TimeAssert(Develop.TIME_ASSERT, "重そう");
			foreach (var arg in args) {
				var str = String.Format(CultureInfo.InvariantCulture, "{0}", arg);// やっぱりこれだとbyteを変換できないんだね。
				sb.Append(CharBulk)
					.Append(enc.GetByteCount(str))
					.Append(CharEOL)
					.Append(str)
					.Append(CharEOL);
			}
			strCommand = sb.ToString();
		}
		// XrossPeer.Log("strCommand:" + strCommand);
		
		byte[] bytes = Encoding.UTF8.GetBytes(strCommand.ToCharArray());
		
		socketToken.stack.Enqueue(commandEnum);
		socketToken.sendArgs.SetBuffer(bytes, 0, bytes.Length);
		
		if (!socketToken.socket.SendAsync(socketToken.sendArgs)) OnSend(socketToken.socket, socketToken.sendArgs);
	}
	
	public void Disconnect () {
		switch (socketToken.state) {
			case ConnectionState.CLOSING:
			case ConnectionState.CLOSED: {
				// do nothing
				break;
			}
			default: {
				socketToken.state = ConnectionState.CLOSING;
				
				var closeEventArgs = new SocketAsyncEventArgs();
				closeEventArgs.UserToken = socketToken;
				closeEventArgs.AcceptSocket = socketToken.socket;
				closeEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnClosed);
				
				if (!socketToken.socket.DisconnectAsync(closeEventArgs)) OnClosed(socketToken.socket, closeEventArgs);
				break;
			}
		}
	}
	
	
	
	/*
		utils
	*/
	
	private static bool SocketConnected (Socket s) {
		bool part1 = s.Poll(1000, SelectMode.SelectRead);
		bool part2 = (s.Available == 0);
		
		if (part1 && part2) return false;
		
		return true;
    }
	
	private static ulong GetHash (string str) {
		using (var md5 = MD5.Create()) {
			using (var stream = new MemoryStream()) {
				using (var writer = new StreamWriter(stream)) {
					writer.Write(str);
					writer.Flush();
					stream.Position = 0;
					var hashed = md5.ComputeHash(stream);
					return BitConverter.ToUInt64(hashed, 0);
				}
			}
		}
	}
}