using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DisquuunCore {
    public class Disquuun {
		public readonly string connectionId;
		
		private readonly Action<string> Connected;
		private readonly Action<DisqueCommand, ByteDatas[]> Received;
		private readonly Action<DisqueCommand, string> Failed;
		private readonly Action<Exception> Error;
		private readonly Action<string> Closed;
		
		public readonly long BufferSize;
		
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
		
		public class SocketToken {
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
			
			this.BufferSize = bufferSize;
			
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
				byte[] receiveBuffer = new byte[bufferSize];
				receiveArgs.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
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
			if (args.SocketError != SocketError.Success) { 
				switch (token.state) {
					case ConnectionState.CLOSING:
					case ConnectionState.CLOSED: {
						// already closing, ignore.
						return;
					}
					default: {
						// show error, then close or continue receiving.
						
						if (Error != null) {
							var error = new Exception("receive error:" + args.SocketError.ToString() + " size:" + args.BytesTransferred);
							Error(error);
						}
						
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
				if (0 < token.stack.Count) { 
					var dataSource = args.Buffer;
					var bytesAmount = args.BytesTransferred;
					
					var rest = args.AcceptSocket.Available;
					if (0 < rest) {
						var restBuffer = new byte[rest];
						var additionalReadResult = token.socket.Receive(restBuffer, SocketFlags.None);
						
						var baseLength = dataSource.Length;
						Array.Resize(ref dataSource, baseLength + rest);
						
						for (var i = 0; i < rest; i++) dataSource[baseLength + i] = restBuffer[i];
						bytesAmount = dataSource.Length;
					}
					
					DisqueFilter.Evaluate(token.stack, bytesAmount, dataSource, Received, Failed);
				}
			}
			
			// continue to receive.
			if (!token.socket.ReceiveAsync(args)) OnReceived(null, args);
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
		
		/*
			transform disque result to byte datas. 
		*/
		public static class DisqueFilter {
			
			public static void Evaluate (Queue<DisqueCommand> commands, int bytesTransferred, byte[] sourceBuffer, Action<DisqueCommand, ByteDatas[]> Received, Action<DisqueCommand, string> Failed) {
				int cursor = 0;
				
				while (cursor < bytesTransferred) {
					if (commands.Count == 0) {
						// shortage of command. 
						break;
					} 
					var currentCommand = commands.Dequeue();
					
					// Log("receiving currentCommand:" + currentCommand);
					
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
										var errorStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// Log("errorStr:" + errorStr);
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
										// var idStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// Log("idStr:" + idStr);
										
										var countBuffer = new byte[lineEndCursor - cursor];
										Array.Copy(sourceBuffer, cursor, countBuffer, 0, lineEndCursor - cursor);
										
										Received(currentCommand, new ByteDatas[]{new ByteDatas(countBuffer)});
									}
									
									cursor = lineEndCursor + 2;// CR + LF
									break;
								}
								default: {
									Log("currentCommand:" + currentCommand + " unhanlded:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor));
									cursor = bytesTransferred;
									break;
								}
							}
							break;
						}
						case DisqueCommand.GETJOB: {
							switch (sourceBuffer[cursor]) {
								case ByteMultiBulk: {
									ByteDatas[] jobDatas = null;
									{
										// *
										var lineEndCursor = ReadLine(sourceBuffer, cursor);
										cursor = cursor + 1;// add header byte size = 1.
										
										var bulkCountStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										var bulkCountNum = Convert.ToInt32(bulkCountStr);
										
										// Log("bulkCountNum:" + bulkCountNum);
										cursor = lineEndCursor + 2;// CR + LF
										
										if (bulkCountNum < 0) {// trigger when GETJOB NOHANG
											if (Received != null) {
												Received(currentCommand, new ByteDatas[]{});
											}
											break;
										}
										
										jobDatas = new ByteDatas[bulkCountNum];
										for (var i = 0; i < bulkCountNum; i++) {
											var itemCount = 0;
											{
												// *
												var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
												cursor = cursor + 1;// add header byte size = 1.
												
												var bulkCountStr2 = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
												itemCount = Convert.ToInt32(bulkCountStr2);
												
												// Log("itemCount:" + itemCount);
												
												cursor = lineEndCursor2 + 2;// CR + LF
											}
											
											// queueName
											{
												// $
												var lineEndCursor3 = ReadLine(sourceBuffer, cursor);
												cursor = cursor + 1;// add header byte size = 1.
												
												var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor3 - cursor);
												var strNum = Convert.ToInt32(countStr);
												// Log("id strNum:" + strNum);
												
												cursor = lineEndCursor3 + 2;// CR + LF
												
												// var nameStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
												// Log("nameStr:" + nameStr);
												
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
												// Log("id strNum:" + strNum);
												
												cursor = lineEndCursor3 + 2;// CR + LF
												
												jobIdIndex = cursor;
												jobIdLength = strNum;
												
												// var jobIdStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
												// Log("jobIdStr:" + jobIdStr);
												
												cursor = cursor + strNum + 2;// CR + LF
											}
											
											// jobData
											byte[] jobIdBytes;
											byte[] dataBytes;
											{
												// $
												var lineEndCursor3 = ReadLine(sourceBuffer, cursor);
												cursor = cursor + 1;// add header byte size = 1.
												
												var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor3 - cursor);
												var strNum = Convert.ToInt32(countStr);
												// Log("data strNum:" + strNum);
												
												cursor = lineEndCursor3 + 2;// CR + LF
												
												jobIdBytes = new byte[jobIdLength];
												Array.Copy(sourceBuffer, jobIdIndex, jobIdBytes, 0, jobIdLength);
												
												dataBytes = new byte[strNum];
												Array.Copy(sourceBuffer, cursor, dataBytes, 0, strNum);
												
												cursor = cursor + strNum + 2;// CR + LF
											}
											
											if (itemCount == 3) {
												jobDatas[i] = new ByteDatas(jobIdBytes, dataBytes);
												continue;
											}
											
											
											if (itemCount == 7) {
												byte[] nackCountBytes;
												{
													// $
													var lineEndCursor3 = ReadLine(sourceBuffer, cursor);
													cursor = cursor + 1;// add header byte size = 1.
													
													var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor3 - cursor);
													var strNum = Convert.ToInt32(countStr);
													// Log("data strNum:" + strNum);
													
													cursor = lineEndCursor3 + 2;// CR + LF
													
													// ignore params. 
												
													cursor = cursor + strNum + 2;// CR + LF
												
													// :
													var lineEndCursor4 = ReadLine(sourceBuffer, cursor);
													cursor = cursor + 1;// add header byte size = 1.
													
													nackCountBytes = new byte[lineEndCursor4 - cursor];
													Array.Copy(sourceBuffer, cursor, nackCountBytes, 0, nackCountBytes.Length);
													
													cursor = lineEndCursor4 + 2;// CR + LF
												}
												
												byte[] additionalDeliveriesCountBytes;
												{
													// $
													var lineEndCursor3 = ReadLine(sourceBuffer, cursor);
													cursor = cursor + 1;// add header byte size = 1.
													
													var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor3 - cursor);
													var strNum = Convert.ToInt32(countStr);
													// Log("data strNum:" + strNum);
													
													cursor = lineEndCursor3 + 2;// CR + LF
													
													// ignore params. 
												
													cursor = cursor + strNum + 2;// CR + LF
												
													// :
													var lineEndCursor4 = ReadLine(sourceBuffer, cursor);
													cursor = cursor + 1;// add header byte size = 1.
													
													additionalDeliveriesCountBytes = new byte[lineEndCursor4 - cursor];
													Array.Copy(sourceBuffer, cursor, additionalDeliveriesCountBytes, 0, additionalDeliveriesCountBytes.Length);
													
													jobDatas[i] = new ByteDatas(jobIdBytes, dataBytes, nackCountBytes, additionalDeliveriesCountBytes);
													
													cursor = lineEndCursor4 + 2;// CR + LF
												}
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
								case ByteError: {
									// -
									var lineEndCursor = ReadLine(sourceBuffer, cursor);
									cursor = cursor + 1;// add header byte size = 1.
									
									if (Failed != null) {
										var errorStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// Log("errorStr:" + errorStr);
										Failed(currentCommand, errorStr);
									}
									
									cursor = lineEndCursor + 2;// CR + LF
									break;
								}
								default: {
									Log("currentCommand:" + currentCommand + " unhanlded:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor));
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
										// var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// Log("countStr:" + countStr);
										
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
										var errorStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// Log("errorStr:" + errorStr);
										Failed(currentCommand, errorStr);
									}
									
									cursor = lineEndCursor + 2;// CR + LF
									break;
								}
								default: {
									Log("currentCommand:" + currentCommand + " unhanlded:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor));
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
										// var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// Log("countStr:" + countStr);
										
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
										var errorStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// Log("errorStr:" + errorStr);
										Failed(currentCommand, errorStr);
									}
									
									cursor = lineEndCursor + 2;// CR + LF
									break;
								}
								default: {
									Log("currentCommand:" + currentCommand + " unhanlded:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor));
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
										// var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// Log("countStr:" + countStr);
										
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
										var errorStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// Log("errorStr:" + errorStr);
										Failed(currentCommand, errorStr);
									}
									
									cursor = lineEndCursor + 2;// CR + LF
									break;
								}
								default: {
									Log("currentCommand:" + currentCommand + " unhanlded:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor));
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
									
									var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
									var countNum = Convert.ToInt32(countStr);
									
									cursor = lineEndCursor + 2;// CR + LF
									
									if (Received != null) {
										// var dataStr = Encoding.UTF8.GetString(sourceBuffer, cursor, countNum);
										// Log("dataStr:" + dataStr);
										
										var newBuffer = new byte[countNum];
										Array.Copy(sourceBuffer, cursor, newBuffer, 0, countNum);
										
										Received(currentCommand, new ByteDatas[]{new ByteDatas(newBuffer)});
									}
									
									cursor = cursor + countNum + 2;// CR + LF
									break;
								}
								// case ByteMultiBulk: {
								// 	// var str = 
								// 	cursor = bytesTransferred;
								// 	break;
								// }
								default: {
									Log("currentCommand:" + currentCommand + " unhanlded:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor));
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
										
										// var bulkCountStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// Log("bulkCountStr:" + bulkCountStr);
										
										cursor = lineEndCursor + 2;// CR + LF
									}
									
									{
										// : format version
										var lineEndCursor = ReadLine(sourceBuffer, cursor);
										cursor = cursor + 1;// add header byte size = 1.
										
										version = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										// Log(":version:" + version);
										
										cursor = lineEndCursor + 2;// CR + LF
									}
									
									{
										// $ this node id
										var lineEndCursor = ReadLine(sourceBuffer, cursor);
										cursor = cursor + 1;// add header byte size = 1.
										
										var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										var strNum = Convert.ToInt32(countStr);
										// Log("id strNum:" + strNum);
										
										cursor = lineEndCursor + 2;// CR + LF
										
										thisNodeId = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
										// Log("thisNodeId:" + thisNodeId);
										
										cursor = cursor + strNum + 2;// CR + LF
									}
									
									{
										// * node ids
										var lineEndCursor = ReadLine(sourceBuffer, cursor);
										cursor = cursor + 1;// add header byte size = 1.
										
										var bulkCountStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
										var bulkCountNum = Convert.ToInt32(bulkCountStr);
										Log("bulkCountNum:" + bulkCountNum);
										
										cursor = lineEndCursor + 2;// CR + LF
										
										// nodeId, ip, port, priority.
										for (var i = 0; i < bulkCountNum/4; i++) {
											var idStr = string.Empty;
											
											// $ nodeId
											{
												var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
												cursor = cursor + 1;// add header byte size = 1.
												
												var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
												var strNum = Convert.ToInt32(countStr);
												
												cursor = lineEndCursor2 + 2;// CR + LF
												
												idStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
												nodeIdsAndInfos.Add(idStr);
												
												cursor = cursor + strNum + 2;// CR + LF
											}
											
											{
												var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
												cursor = cursor + 1;// add header byte size = 1.
												
												var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
												var strNum = Convert.ToInt32(countStr);
												
												cursor = lineEndCursor2 + 2;// CR + LF
												
												var ipStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
												nodeIdsAndInfos.Add(ipStr);
												
												cursor = cursor + strNum + 2;// CR + LF
											}
											
											{
												var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
												cursor = cursor + 1;// add header byte size = 1.
												
												var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
												var strNum = Convert.ToInt32(countStr);
												
												cursor = lineEndCursor2 + 2;// CR + LF
												
												var portStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
												nodeIdsAndInfos.Add(portStr);
												
												cursor = cursor + strNum + 2;// CR + LF
											}
											
											{
												var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
												cursor = cursor + 1;// add header byte size = 1.
												
												var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
												var strNum = Convert.ToInt32(countStr);
												
												cursor = lineEndCursor2 + 2;// CR + LF
												
												var priorityStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
												nodeIdsAndInfos.Add(priorityStr);
												
												cursor = cursor + strNum + 2;// CR + LF
											}
										}
									}
									
									if (Received != null) {
										var versionBytes = Encoding.UTF8.GetBytes(version);
										var thisNodeIdBytes = Encoding.UTF8.GetBytes(thisNodeId);
										
										var byteDatas = new ByteDatas[1 + nodeIdsAndInfos.Count/4];
										byteDatas[0] = new ByteDatas(versionBytes,thisNodeIdBytes);
										
										for (var index = 0; index < nodeIdsAndInfos.Count/4; index++) {
											var nodeId = Encoding.UTF8.GetBytes(nodeIdsAndInfos[index*4 + 0]);
											var ip = Encoding.UTF8.GetBytes(nodeIdsAndInfos[index*4 + 1]);
											var port = Encoding.UTF8.GetBytes(nodeIdsAndInfos[index*4 + 2]);
											var priority = Encoding.UTF8.GetBytes(nodeIdsAndInfos[index*4 + 3]);
											
											byteDatas[index + 1] = new ByteDatas(nodeId, ip, port, priority);
										}
										Received(currentCommand, byteDatas);
									}
									break;
								}
								default: {
									Log("currentCommand:" + currentCommand + " unhanlded:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor));
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
									Log("currentCommand:" + currentCommand + " unhanlded:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor));
									cursor = bytesTransferred;
									break;
								}
							}
							break;
						}
						case DisqueCommand.QSTAT: {
							var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							Log("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						case DisqueCommand.QPEEK: {
							var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							Log("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						case DisqueCommand.ENQUEUE: {
							var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							Log("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						case DisqueCommand.DEQUEUE: {
							var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							Log("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						case DisqueCommand.DELJOB: {
							var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							Log("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						case DisqueCommand.SHOW: {
							var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							Log("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						case DisqueCommand.QSCAN: {
							var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							Log("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						case DisqueCommand.JSCAN: {
							var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							Log("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						case DisqueCommand.PAUSE: {
							var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
							Log("not yet applied:" + currentCommand + " data:" + data);
							cursor = bytesTransferred;
							break;
						}
						default: {
							Log("unknown command:" + currentCommand);
							break;
						}
					}
				}
			}
			
			public static int ReadLine (byte[] bytes, int cursor) {
				do {
					if (bytes[cursor] == ByteEOL) break;
					cursor++;
				} while (cursor < bytes.Length);
				
				return cursor - 1;
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
			var dataStr = Encoding.UTF8.GetString(data);
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
			// foreach (var i in parameters) {
			// 	Log("i:" + i);
			// }
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
				
				sb.Append(CharBulk).Append(Encoding.UTF8.GetByteCount(command)).Append(CharEOL).Append(command).Append(CharEOL);
				
				foreach (var arg in args) {
					var str = String.Format(CultureInfo.InvariantCulture, "{0}", arg);
					sb.Append(CharBulk)
						.Append(Encoding.UTF8.GetByteCount(str))
						.Append(CharEOL)
						.Append(str)
						.Append(CharEOL);
				}
				strCommand = sb.ToString();
			}
			// Log("strCommand:" + strCommand);
			
			// 結局byteに変換してるんだよな~ なので
			
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
		
		private static void Log (string message) {
			// TestLogger.Log(message);
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
	}
}