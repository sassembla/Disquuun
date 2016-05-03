using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace DisquuunCore {
	public class Disquuun {
		public readonly string connectionId;
		
		private readonly Action<string> Connected;
		private readonly Action<DisqueCommand, ByteDatas[]> Received;
		private readonly Action<DisqueCommand, string> Failed;
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
			Action<DisqueCommand, ByteDatas[]> Received=null,
			Action<DisqueCommand, string> Failed=null,
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
						if (!IsSocketConnected(token.socket)) {
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
				// あと、サイズが0で帰ってくることがあるんかな、、
				
				// TestLogger.Log("複数件突っ込まれるのでは = データが複数入るのでは、、？ YES。 途中でちぎれるのでは、、? <- わからん、、");
				// TestLogger.Log("データのもち越しどうなるんだろう");
				
				var cursor = DisqueFilter.Evaluate(command, token.stack, args.BytesTransferred, args.Buffer, Received, Failed);
				if (cursor != args.BytesTransferred) TestLogger.Log("command:" + command + " args.BytesTransferred:" + args.BytesTransferred + " vs cursor:" + cursor);
			}
			
			// continue to receive.
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
		
		public const string DISQUE_GETJOB_KEYWORD_FROM = "FROM";
		
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
			transform disque result to byte datas. 
		*/
		public static class DisqueFilter {
			public static int Evaluate(DisqueCommand currentCommand, Queue<DisqueCommand> commands, int bytesTransferred, byte[] sourceBuffer, Action<DisqueCommand, ByteDatas[]> Received, Action<DisqueCommand, string> Failed) {
				int cursor = 0;
				
				while (cursor < bytesTransferred) {
					if (0 < cursor && 0 < commands.Count) currentCommand = commands.Dequeue();
					
					// TestLogger.Log("currentCommand:" + currentCommand);
					
					// データの先頭しか受け取れないケースとかがありそうな気がする、発生を検知したい。バッファサイズ小さくして試すか。
					if (sourceBuffer.Length - 1 <= bytesTransferred) {
						TestLogger.Log("too much size data comming. んでどうなるんだろう。 bytesTransferred:" + bytesTransferred + " vs sourceBuffer.Length:" + sourceBuffer.Length);
					}
					
					/*
						get data then react.
					*/
					switch (currentCommand) {
						case DisqueCommand.ADDJOB: {
							switch (sourceBuffer[cursor]) {
								case ByteError: {
									// -
									var lineEndCursor = ReadLine(sourceBuffer, cursor);
									cursor = cursor + 1;// add header byte size = 1.
									
									if (Failed != null) {
										var errorStr = enc.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// TestLogger.Log("errorStr:" + errorStr);
										Failed(currentCommand, errorStr);
									}
									
									cursor = lineEndCursor + 2;// CR + LF
									break;
								}
								case ByteStatus: {
									// +
									var lineEndCursor = ReadLine(sourceBuffer, cursor);
									cursor = cursor + 1;// add header byte size = 1.
									
									if (Received != null) {
										// var idStrBytes = new byte[lineEndCursor - cursor];
										// var idStr = enc.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// TestLogger.Log("idStr:" + idStr);
										
										var countBuffer = new byte[lineEndCursor - cursor];
										Array.Copy(sourceBuffer, cursor, countBuffer, 0, lineEndCursor - cursor);
										
										Received(currentCommand, new ByteDatas[]{new ByteDatas(countBuffer)});
									}
									
									cursor = lineEndCursor + 2;// CR + LF
									break;
								}
								default: {
									TestLogger.LogError("currentCommand:" + currentCommand + " unhanlded:" + sourceBuffer[cursor]);
									cursor = bytesTransferred;
									break;
								}
							}
							break;
						}
						case DisqueCommand.GETJOB: {
							ByteDatas[] jobDatas = null;
							switch (sourceBuffer[cursor]) {
								case ByteMultiBulk: {
									{
										// *
										var lineEndCursor = ReadLine(sourceBuffer, cursor);
										cursor = cursor + 1;// add header byte size = 1.
										
										var bulkCountStr = enc.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										var bulkCountNum = Convert.ToInt32(bulkCountStr);
										// TestLogger.Log("bulkCountNum:" + bulkCountNum);
										cursor = lineEndCursor + 2;// CR + LF
										
										jobDatas = new ByteDatas[bulkCountNum];
										
										for (var i = 0; i < bulkCountNum; i++) {
											{
												// *
												var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
												cursor = cursor + 1;// add header byte size = 1.
												
												var bulkCountStr2 = enc.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
												var bulkCountNum2 = Convert.ToInt32(bulkCountStr2);
												// TestLogger.Log("bulkCountNum2:" + bulkCountNum2);
												cursor = lineEndCursor2 + 2;// CR + LF
											}
											
											// queueName
											{
												// $
												var lineEndCursor3 = ReadLine(sourceBuffer, cursor);
												cursor = cursor + 1;// add header byte size = 1.
												
												var countStr = enc.GetString(sourceBuffer, cursor, lineEndCursor3 - cursor);
												var strNum = Convert.ToInt32(countStr);
												// TestLogger.Log("id strNum:" + strNum);
												
												cursor = lineEndCursor3 + 2;// CR + LF
												
												var nameStr = enc.GetString(sourceBuffer, cursor, strNum);
												// TestLogger.Log("nameStr:" + nameStr);
												
												cursor = cursor + strNum + 2;// CR + LF
											}
											
											var jobIdIndex = 0;
											var jobIdLength = 0;
											
											// jobId
											{
												// $
												var lineEndCursor3 = ReadLine(sourceBuffer, cursor);
												cursor = cursor + 1;// add header byte size = 1.
												
												var countStr = enc.GetString(sourceBuffer, cursor, lineEndCursor3 - cursor);
												var strNum = Convert.ToInt32(countStr);
												// TestLogger.Log("id strNum:" + strNum);
												
												cursor = lineEndCursor3 + 2;// CR + LF
												
												jobIdIndex = cursor;
												jobIdLength = strNum;
												// var jobIdSrt = enc.GetString(sourceBuffer, cursor, strNum);
												// TestLogger.Log("jobIdSrt:" + jobIdSrt);
												
												cursor = cursor + strNum + 2;// CR + LF
											}
											
											// jobData
											{
												// $
												var lineEndCursor3 = ReadLine(sourceBuffer, cursor);
												cursor = cursor + 1;// add header byte size = 1.
												
												var countStr = enc.GetString(sourceBuffer, cursor, lineEndCursor3 - cursor);
												var strNum = Convert.ToInt32(countStr);
												// TestLogger.Log("data strNum:" + strNum);
												
												cursor = lineEndCursor3 + 2;// CR + LF
												
												var jobIdBytes = new byte[jobIdLength];
												Array.Copy(sourceBuffer, jobIdIndex, jobIdBytes, 0, jobIdLength);
												
												var dataBytes = new byte[strNum];
												Array.Copy(sourceBuffer, cursor, dataBytes, 0, strNum);
												
												jobDatas[i] = new ByteDatas(jobIdBytes, dataBytes);
												
												cursor = cursor + strNum + 2;// CR + LF
											}	
										}
									}
									
									if (Received != null) {
										if (jobDatas != null) {
											if (0 < jobDatas.Length) {
												Received(currentCommand, jobDatas);
											} 	
										}
									}
									break;
								}
								default: {
									TestLogger.LogError("currentCommand:" + currentCommand + " unhanlded:" + sourceBuffer[cursor]);
									cursor = bytesTransferred;
									break;
								}
							}
							break;
						}
						case DisqueCommand.ACKJOB:
						case DisqueCommand.FASTACK: {
							switch (sourceBuffer[cursor]) {
								case ByteInt: {
									// :Identity count
									var lineEndCursor = ReadLine(sourceBuffer, cursor);
									cursor = cursor + 1;// add header byte size = 1.
									
									if (Received != null) { 	
										// var countStr = enc.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// var countNum = Convert.ToInt32(countStr);
										// TestLogger.Log("countNum:" + countNum);
										
										var countBuffer = new byte[lineEndCursor - cursor];
										Array.Copy(sourceBuffer, cursor, countBuffer, 0, countBuffer.Length);
										
										var byteData = new ByteDatas(countBuffer);
										
										Received(currentCommand, new ByteDatas[]{byteData});
									}
									
									cursor = lineEndCursor + 2;// CR + LF
									break;
								}
								default: {
									TestLogger.LogError("currentCommand:" + currentCommand + " unhanlded:" + sourceBuffer[cursor]);
									cursor = bytesTransferred;
									break;
								}
							}
							break;
						}
						case DisqueCommand.WORKING: {
							switch (sourceBuffer[cursor]) {
								case ByteInt: {
									// :Int count
									var lineEndCursor = ReadLine(sourceBuffer, cursor);
									cursor = cursor + 1;// add header byte size = 1.
									
									if (Received != null) { 	
										// var countStr = enc.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// var countNum = Convert.ToInt32(countStr);
										// TestLogger.Log("countNum:" + countNum);
										
										var countBuffer = new byte[lineEndCursor - cursor];
										Array.Copy(sourceBuffer, cursor, countBuffer, 0, countBuffer.Length);
										
										var byteData = new ByteDatas(countBuffer);
										
										Received(currentCommand, new ByteDatas[]{byteData});
									}
									
									cursor = lineEndCursor + 2;// CR + LF
									break;
								}
								case ByteError: {
									// -NOJOB Job not known in the context of this node.
									var lineEndCursor = ReadLine(sourceBuffer, cursor);
									cursor = cursor + 1;// add header byte size = 1.
									
									if (Failed != null) {
										var errorStr = enc.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// TestLogger.Log("errorStr:" + errorStr);
										Failed(currentCommand, errorStr);
									}
									
									cursor = lineEndCursor + 2;// CR + LF
									break;
								}
								default: {
									TestLogger.LogError("currentCommand:" + currentCommand + " unhanlded:" + sourceBuffer[cursor]);
									cursor = bytesTransferred;
									break;
								}
							}
							break;
						}
						case DisqueCommand.NACK: {
							switch (sourceBuffer[cursor]) {
								case ByteInt: {
									// :Int count
									var lineEndCursor = ReadLine(sourceBuffer, cursor);
									cursor = cursor + 1;// add header byte size = 1.
									
									if (Received != null) { 	
										// var countStr = enc.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// var countNum = Convert.ToInt32(countStr);
										// TestLogger.Log("countNum:" + countNum);
										
										var countBuffer = new byte[lineEndCursor - cursor];
										Array.Copy(sourceBuffer, cursor, countBuffer, 0, countBuffer.Length);
										
										var byteData = new ByteDatas(countBuffer);
										
										Received(currentCommand, new ByteDatas[]{byteData});
									}
									
									cursor = lineEndCursor + 2;// CR + LF
									break;
								}
								case ByteError: {
									// -
									var lineEndCursor = ReadLine(sourceBuffer, cursor);
									cursor = cursor + 1;// add header byte size = 1.
									
									if (Failed != null) {
										var errorStr = enc.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// TestLogger.Log("errorStr:" + errorStr);
										Failed(currentCommand, errorStr);
									}
									
									cursor = lineEndCursor + 2;// CR + LF
									break;
								}
								default: {
									TestLogger.LogError("currentCommand:" + currentCommand + " unhanlded:" + sourceBuffer[cursor]);
									cursor = bytesTransferred;
									break;
								}
							}
							break;
						}
						case DisqueCommand.INFO: {
							switch (sourceBuffer[cursor]) {
								case ByteBulk: {
									// $
									var lineEndCursor = ReadLine(sourceBuffer, cursor);
									cursor = cursor + 1;// add header byte size = 1.
									
									var countStr = enc.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
									var countNum = Convert.ToInt32(countStr);
									
									cursor = lineEndCursor + 2;// CR + LF
									
									if (Received != null) {
										// var dataStr = enc.GetString(sourceBuffer, cursor, countNum);
										// TestLogger.Log("dataStr:" + dataStr);
										
										var newBuffer = new byte[countNum];
										Array.Copy(sourceBuffer, cursor, newBuffer, 0, countNum);
										
										Received(currentCommand, new ByteDatas[]{new ByteDatas(newBuffer)});
									}
									
									cursor = cursor + countNum + 2;// CR + LF
									break;
								}
								default: {
									TestLogger.LogError("currentCommand:" + currentCommand + " unhanlded:" + sourceBuffer[cursor]);
									cursor = bytesTransferred;
									break;
								}
							}
							break;
						}
						case DisqueCommand.HELLO: {
							switch (sourceBuffer[cursor]) {
								case ByteMultiBulk: {
									string version;
									string thisNodeId;
									List<string> nodeIdsAndInfos = new List<string>();
									/*
										:*3
											:1 version [0][0]
											
											$40 this node ID [0][1]
												002698920b158ba29ff8d41d3e5303ceaf0e8d45
											
											*4 [1~n][0~3]
												$40
													002698920b158ba29ff8d41d3e5303ceaf0e8d45
												
												$0
													""
												
												$4
													7711
												
												$1
													1
									*/
									
									{
										// *
										var lineEndCursor = ReadLine(sourceBuffer, cursor);
										cursor = cursor + 1;// add header byte size = 1.
										
										var bulkCountStr = enc.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										var bulkCountNum = Convert.ToInt32(bulkCountStr);
										// TestLogger.Log("bulkCountNum:" + bulkCountNum);
										cursor = lineEndCursor + 2;// CR + LF
									}
									
									{
										// : format version
										var lineEndCursor = ReadLine(sourceBuffer, cursor);
										cursor = cursor + 1;// add header byte size = 1.
										
										version = enc.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// var countNum = Convert.ToInt32(countStr);
										// TestLogger.Log(":countNum:" + countNum);
										cursor = lineEndCursor + 2;// CR + LF
									}
									
									{
										// $ this node id
										var lineEndCursor = ReadLine(sourceBuffer, cursor);
										cursor = cursor + 1;// add header byte size = 1.
										
										var countStr = enc.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										var strNum = Convert.ToInt32(countStr);
										// TestLogger.Log("id strNum:" + strNum);
										
										cursor = lineEndCursor + 2;// CR + LF
										
										thisNodeId = enc.GetString(sourceBuffer, cursor, strNum);
										// TestLogger.Log("idStr:" + idStr);
										
										cursor = cursor + strNum + 2;// CR + LF
									}
									
									{
										// * node ids
										var lineEndCursor = ReadLine(sourceBuffer, cursor);
										cursor = cursor + 1;// add header byte size = 1.
										
										var bulkCountStr = enc.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										var bulkCountNum = Convert.ToInt32(bulkCountStr);
										// TestLogger.Log("bulkCountNum:" + bulkCountNum);
										
										cursor = lineEndCursor + 2;// CR + LF
										
										// nodeId, ip, port, priority.
										for (var i = 0; i < bulkCountNum/4; i++) {
											var idStr = string.Empty;
											
											// $ nodeId
											{
												var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
												cursor = cursor + 1;// add header byte size = 1.
												
												var countStr = enc.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
												var strNum = Convert.ToInt32(countStr);
												
												cursor = lineEndCursor2 + 2;// CR + LF
												
												idStr = enc.GetString(sourceBuffer, cursor, strNum);
												nodeIdsAndInfos.Add(idStr);
												
												cursor = cursor + strNum + 2;// CR + LF
											}
											
											{
												var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
												cursor = cursor + 1;// add header byte size = 1.
												
												var countStr = enc.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
												var strNum = Convert.ToInt32(countStr);
												
												cursor = lineEndCursor2 + 2;// CR + LF
												
												var ipStr = enc.GetString(sourceBuffer, cursor, strNum);
												nodeIdsAndInfos.Add(ipStr);
												
												cursor = cursor + strNum + 2;// CR + LF
											}
											
											{
												var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
												cursor = cursor + 1;// add header byte size = 1.
												
												var countStr = enc.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
												var strNum = Convert.ToInt32(countStr);
												
												cursor = lineEndCursor2 + 2;// CR + LF
												
												var portStr = enc.GetString(sourceBuffer, cursor, strNum);
												nodeIdsAndInfos.Add(portStr);
												
												cursor = cursor + strNum + 2;// CR + LF
											}
											
											{
												var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
												cursor = cursor + 1;// add header byte size = 1.
												
												var countStr = enc.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
												var strNum = Convert.ToInt32(countStr);
												
												cursor = lineEndCursor2 + 2;// CR + LF
												
												var priorityStr = enc.GetString(sourceBuffer, cursor, strNum);
												nodeIdsAndInfos.Add(priorityStr);
												
												cursor = cursor + strNum + 2;// CR + LF
											}
										}
									}
									
									if (Received != null) {
										var versionBytes = enc.GetBytes(version);
										var thisNodeIdBytes = enc.GetBytes(thisNodeId);
										
										var byteDatas = new ByteDatas[1 + nodeIdsAndInfos.Count/4];
										byteDatas[0] = new ByteDatas(versionBytes,thisNodeIdBytes);
										
										for (var index = 0; index < nodeIdsAndInfos.Count/4; index++) {
											var nodeId = enc.GetBytes(nodeIdsAndInfos[index*4 + 0]);
											var ip = enc.GetBytes(nodeIdsAndInfos[index*4 + 1]);
											var port = enc.GetBytes(nodeIdsAndInfos[index*4 + 2]);
											var priority = enc.GetBytes(nodeIdsAndInfos[index*4 + 3]);
											
											byteDatas[index + 1] = new ByteDatas(nodeId, ip, port, priority);
										}
										Received(currentCommand, byteDatas);
									}
									break;
								}
								default: {
									TestLogger.LogError("currentCommand:" + currentCommand + " unhanlded:" + sourceBuffer[cursor]);
									cursor = bytesTransferred;
									break;
								}
							}
							break;
						}
						case DisqueCommand.QLEN: {
							switch (sourceBuffer[cursor]) {
								case ByteInt: {
									// : format version
									var lineEndCursor = ReadLine(sourceBuffer, cursor);
									cursor = cursor + 1;// add header byte size = 1.
									
									var countBuffer = new byte[lineEndCursor - cursor];
									Array.Copy(sourceBuffer, cursor, countBuffer, 0, countBuffer.Length);
									
									var byteData = new ByteDatas(countBuffer);
									
									if (Received != null) {
										Received(currentCommand, new ByteDatas[]{byteData});
									}
									
									cursor = lineEndCursor + 2;// CR + LF
									break;
								}
								default: {
									TestLogger.LogError("currentCommand:" + currentCommand + " unhanlded:" + sourceBuffer[cursor]);
									cursor = bytesTransferred;
									break;
								}
							}
							break;
						}
						case DisqueCommand.QSTAT: {
							var data = enc.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							TestLogger.LogError("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						case DisqueCommand.QPEEK: {
							var data = enc.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							TestLogger.LogError("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						case DisqueCommand.ENQUEUE: {
							var data = enc.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							TestLogger.LogError("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						case DisqueCommand.DEQUEUE: {
							var data = enc.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							TestLogger.LogError("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						case DisqueCommand.DELJOB: {
							var data = enc.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							TestLogger.LogError("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						case DisqueCommand.SHOW: {
							var data = enc.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							TestLogger.LogError("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						case DisqueCommand.QSCAN: {
							var data = enc.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							TestLogger.LogError("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						case DisqueCommand.JSCAN: {
							var data = enc.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							TestLogger.LogError("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						case DisqueCommand.PAUSE: {
							var data = enc.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							TestLogger.LogError("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						default: {
							TestLogger.Log("unknown command:" + currentCommand);
							break;
						}
					}
				}
				return cursor;
			}
		}
		
		/**
			data structure for vector.
		*/
		public struct ByteDatas {
			public byte[][] bytesArray;
			
			public ByteDatas (params byte[][] bytesArray) {
				this.bytesArray = bytesArray;
			}
		}
		
		/*
			Disque commands.
		*/
		public void AddJob (string queueName, byte[] data, int timeout=0, params object[] args) {
			// ADDJOB queue_name job <ms-timeout> 
			// [REPLICATE <count>] [DELAY <sec>] [RETRY <sec>] [TTL <sec>] [MAXLEN <count>] [ASYNC]
			
			// byteをそのまま送りたいんだが、っていうやつ。byteArrayをそのままではうまく変形できない。
			// あと、いろいろ配列で渡さないといけないんだけど、それも辛い。
			// この段階で
			var dataStr = enc.GetString(data);
			// var newArgs = new object[1 + args.Length];
			// newArgs[0] = timeout;
			// for (var i = 1; i < newArgs.Length; i++) newArgs[i] = args[i-1];
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
					parameters[i] = DISQUE_GETJOB_KEYWORD_FROM;
					continue;
				}
				parameters[i] = queueIds[i - (args.Length + 1)];
			}
			foreach (var i in parameters) {
				TestLogger.Log("i:" + i);
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
			SendBytes(DisqueCommand.WORKING, jobId);
		}

		public void Nack (string[] jobIds) {
			// <job-id> ... <job-id>
			SendBytes(DisqueCommand.NACK, jobIds);
		}
		
		public void Info () {
			SendBytes(DisqueCommand.INFO);
		}
		
		public void Hello () {
			SendBytes(DisqueCommand.HELLO);
		}
		
		public void Qlen (string queueId) {
			// QLEN,// <queue-name>
			SendBytes(DisqueCommand.QLEN, queueId);
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
		
		
		/*
			API core
		*/
		private void SendBytes (DisqueCommand commandEnum, params object[] args) {
			int length = 1 + args.Length;
			
			var command = commandEnum.ToString();
			string strCommand;
			
			// 自前のbyte memory streamを使うかな。StringBuilder 重たいんで使いたくない。あとでベンチ。
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(CharMultiBulk).Append(length).Append(CharEOL);
				
				sb.Append(CharBulk).Append(enc.GetByteCount(command)).Append(CharEOL).Append(command).Append(CharEOL);
				
				foreach (var arg in args) {
					var str = String.Format(CultureInfo.InvariantCulture, "{0}", arg);
					sb.Append(CharBulk)
						.Append(enc.GetByteCount(str))
						.Append(CharEOL)
						.Append(str)
						.Append(CharEOL);
				}
				strCommand = sb.ToString();
			}
			// TestLogger.Log("strCommand:" + strCommand);
			
			// 結局byteに変換してるんだよな。
			byte[] bytes = enc.GetBytes(strCommand.ToCharArray());
			
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
		
		private static bool IsSocketConnected (Socket s) {
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
}