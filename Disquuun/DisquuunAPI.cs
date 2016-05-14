using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace DisquuunCore {
	
	public static class DisquuunAPI {
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
		public static byte ByteCR = Convert.ToByte('\r');
		public static byte ByteLF = Convert.ToByte('\n');
		
		private static byte[] BytesMultiBulk = new byte[]{ByteMultiBulk};
		private static byte[] BytesCRLF = new byte[]{ByteCR, ByteLF};
		private static byte[] BytesBulk = new byte[]{ByteBulk};
	
		
		/*
			Disque APIs.
			一時的にstaticにしておくが、disquuunインスタンスから叩けるようにしておくのが理想。
			
			connectedとかconnect failedとかをどう隠蔽するのかっていうのは考えものだな、、
		*/
		public static byte[] AddJob (string queueName, byte[] data, int timeout=0, params object[] args) {
			// ADDJOB queue_name job <ms-timeout> 
			// [REPLICATE <count>] [DELAY <sec>] [RETRY <sec>] [TTL <sec>] [MAXLEN <count>] [ASYNC]
			
			var newArgs = new object[1 + args.Length];
			newArgs[0] = timeout;
			for (var i = 1; i < newArgs.Length; i++) newArgs[i] = args[i-1];
			
			var byteBuffer = new MemoryStream();

			var contentCount = 1;// count of command.
			
			if (!string.IsNullOrEmpty(queueName)) {
				contentCount++;
			}

			if (0 < data.Length) {
				contentCount++;
			}

			if (0 < newArgs.Length) {
				contentCount = contentCount + newArgs.Length;
			}

			// "*" + contentCount.ToString() + "\r\n"
			{
				var contentCountBytes = Encoding.UTF8.GetBytes(contentCount.ToString());
				
				byteBuffer.Write(BytesMultiBulk, 0, BytesMultiBulk.Length);
				byteBuffer.Write(contentCountBytes, 0, contentCountBytes.Length);
				byteBuffer.Write(BytesCRLF, 0, BytesCRLF.Length);
			}

			// "$" + cmd.Length + "\r\n" + cmd + "\r\n"
			{
				var commandBytes = Encoding.UTF8.GetBytes(DisqueCommand.ADDJOB.ToString());
				var commandCountBytes = Encoding.UTF8.GetBytes(DisqueCommand.ADDJOB.ToString().Length.ToString());
			
				byteBuffer.Write(BytesBulk, 0, BytesBulk.Length);
				byteBuffer.Write(commandCountBytes, 0, commandCountBytes.Length);
				byteBuffer.Write(BytesCRLF, 0, BytesCRLF.Length);
				byteBuffer.Write(commandBytes, 0, commandBytes.Length);
				byteBuffer.Write(BytesCRLF, 0, BytesCRLF.Length);
			}

			// "$" + queueId.Length + "\r\n" + queueId + "\r\n"
			if (!string.IsNullOrEmpty(queueName)) {
				var queueIdBytes = Encoding.UTF8.GetBytes(queueName);
				var queueIdCountBytes = Encoding.UTF8.GetBytes(queueName.Length.ToString());
				
				byteBuffer.Write(BytesBulk, 0, BytesBulk.Length);
				byteBuffer.Write(queueIdCountBytes, 0, queueIdCountBytes.Length);
				byteBuffer.Write(BytesCRLF, 0, BytesCRLF.Length);
				byteBuffer.Write(queueIdBytes, 0, queueIdBytes.Length);
				byteBuffer.Write(BytesCRLF, 0, BytesCRLF.Length);
			}

			// "$" + data.Length + "\r\n" + data + "\r\n"
			if (0 < data.Length) {
				var dataCountBytes = Encoding.UTF8.GetBytes(data.Length.ToString());
				
				byteBuffer.Write(BytesBulk, 0, BytesBulk.Length);
				byteBuffer.Write(dataCountBytes, 0, dataCountBytes.Length);
				byteBuffer.Write(BytesCRLF, 0, BytesCRLF.Length);
				byteBuffer.Write(data, 0, data.Length);
				byteBuffer.Write(BytesCRLF, 0, BytesCRLF.Length);
			}

			// "$" + option.Length + "\r\n" + option + "\r\n"
			if (0 < newArgs.Length) {
				foreach (var option in newArgs) {
					var optionBytes = Encoding.UTF8.GetBytes(option.ToString());
					var optionCountBytes = Encoding.UTF8.GetBytes(newArgs.Length.ToString());
				
					byteBuffer.Write(BytesBulk, 0, BytesBulk.Length);
					byteBuffer.Write(optionCountBytes, 0, optionCountBytes.Length);
					byteBuffer.Write(BytesCRLF, 0, BytesCRLF.Length);
					byteBuffer.Write(optionBytes, 0, optionBytes.Length);
					byteBuffer.Write(BytesCRLF, 0, BytesCRLF.Length);
				}	
			}
			
			return byteBuffer.ToArray();
		}
		
		public static byte[] GetJob (string[] queueIds, params object[] args) {
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
			return ToBytes(DisqueCommand.GETJOB, parameters);
		}
		
		public static byte[] AckJob (string[] jobIds) {
			// jobid1 jobid2 ... jobidN
			return ToBytes(DisqueCommand.ACKJOB, jobIds);
		}

		public static byte[] FastAck (string[] jobIds) {
			// jobid1 jobid2 ... jobidN
			return ToBytes(DisqueCommand.FASTACK, jobIds);
		}

		public static byte[] Working (string jobId) {
			// jobid
			return ToBytes(DisqueCommand.WORKING, jobId);
		}

		public static byte[] Nack (string[] jobIds) {
			// <job-id> ... <job-id>
			return ToBytes(DisqueCommand.NACK, jobIds);
		}
		
		public static byte[] Info () {
			return ToBytes(DisqueCommand.INFO);
		}
		
		public static byte[] Hello () {
			return ToBytes(DisqueCommand.HELLO);
		}
		
		public static byte[] Qlen (string queueId) {
			// QLEN,// <queue-name>
			return ToBytes(DisqueCommand.QLEN, queueId);
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
		
		
		private static byte[] ToBytes (DisqueCommand commandEnum, params object[] args) {
			int length = 1 + args.Length;
			
			var command = commandEnum.ToString();
			string strCommand;
			
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
			
			byte[] bytes = Encoding.UTF8.GetBytes(strCommand.ToCharArray());
			
			return bytes;
		}
		
		public static DisquuunResult[] EvaluateSingleCommand (DisqueCommand currentCommand, int bytesTransferred, byte[] sourceBuffer) {
			var cursor = 0;
			/*
				get data then react.
			*/
			switch (currentCommand) {
				case DisqueCommand.ADDJOB: {
					switch (sourceBuffer[cursor]) {
						// case ByteError: {
						// 	// -
						// 	var lineEndCursor = ReadLine(sourceBuffer, cursor);
						// 	cursor = cursor + 1;// add header byte size = 1.
							
						// 	if (Failed != null) {
						// 		var errorStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
						// 		// Disquuun.Log("errorStr:" + errorStr);
						// 		Failed(currentCommand, errorStr);
						// 	}
							
						// 	cursor = lineEndCursor + 2;// CR + LF
						// 	break;
						// }
						case ByteStatus: {
							// +
							var lineEndCursor = ReadLine(sourceBuffer, cursor);
							cursor = cursor + 1;// add header byte size = 1.
							
							// var idStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
							// Disquuun.Log("idStr:" + idStr);
							
							var countBuffer = new byte[lineEndCursor - cursor];
							Array.Copy(sourceBuffer, cursor, countBuffer, 0, lineEndCursor - cursor);
							
							cursor = lineEndCursor + 2;// CR + LF
							return new DisquuunResult[]{new DisquuunResult(countBuffer)};
						}
						default: {
							Disquuun.Log("currentCommand:" + currentCommand + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor));
							cursor = bytesTransferred;
							break;
						}
					}
					break;
				}
				case DisqueCommand.GETJOB: {
					switch (sourceBuffer[cursor]) {
						case ByteMultiBulk: {
							DisquuunResult[] jobDatas = null;
							{
								// *
								var lineEndCursor = ReadLine(sourceBuffer, cursor);
								cursor = cursor + 1;// add header byte size = 1.
								
								var bulkCountStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
								var bulkCountNum = Convert.ToInt32(bulkCountStr);
								
								// Disquuun.Log("bulkCountNum:" + bulkCountNum);
								cursor = lineEndCursor + 2;// CR + LF
								
								// trigger when GETJOB NOHANG
								if (bulkCountNum < 0) return new DisquuunResult[]{};
								
								
								jobDatas = new DisquuunResult[bulkCountNum];
								for (var i = 0; i < bulkCountNum; i++) {
									var itemCount = 0;
									{
										// *
										var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
										cursor = cursor + 1;// add header byte size = 1.
										
										var bulkCountStr2 = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
										itemCount = Convert.ToInt32(bulkCountStr2);
										
										// Disquuun.Log("itemCount:" + itemCount);
										
										cursor = lineEndCursor2 + 2;// CR + LF
									}
									
									// queueName
									{
										// $
										var lineEndCursor3 = ReadLine(sourceBuffer, cursor);
										cursor = cursor + 1;// add header byte size = 1.
										
										var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor3 - cursor);
										var strNum = Convert.ToInt32(countStr);
										// Disquuun.Log("id strNum:" + strNum);
										
										cursor = lineEndCursor3 + 2;// CR + LF
										
										// var nameStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
										// Disquuun.Log("nameStr:" + nameStr);
										
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
										// Disquuun.Log("id strNum:" + strNum);
										
										cursor = lineEndCursor3 + 2;// CR + LF
										
										jobIdIndex = cursor;
										jobIdLength = strNum;
										
										// var jobIdStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
										// Disquuun.Log("jobIdStr:" + jobIdStr);
										
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
										// Disquuun.Log("data strNum:" + strNum);
										
										cursor = lineEndCursor3 + 2;// CR + LF
										
										jobIdBytes = new byte[jobIdLength];
										Array.Copy(sourceBuffer, jobIdIndex, jobIdBytes, 0, jobIdLength);
										
										dataBytes = new byte[strNum];
										Array.Copy(sourceBuffer, cursor, dataBytes, 0, strNum);
										
										cursor = cursor + strNum + 2;// CR + LF
									}
									
									if (itemCount == 3) {
										jobDatas[i] = new DisquuunResult(jobIdBytes, dataBytes);
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
											// Disquuun.Log("data strNum:" + strNum);
											
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
											// Disquuun.Log("data strNum:" + strNum);
											
											cursor = lineEndCursor3 + 2;// CR + LF
											
											// ignore params. 
										
											cursor = cursor + strNum + 2;// CR + LF
										
											// :
											var lineEndCursor4 = ReadLine(sourceBuffer, cursor);
											cursor = cursor + 1;// add header byte size = 1.
											
											additionalDeliveriesCountBytes = new byte[lineEndCursor4 - cursor];
											Array.Copy(sourceBuffer, cursor, additionalDeliveriesCountBytes, 0, additionalDeliveriesCountBytes.Length);
											
											jobDatas[i] = new DisquuunResult(jobIdBytes, dataBytes, nackCountBytes, additionalDeliveriesCountBytes);
											
											cursor = lineEndCursor4 + 2;// CR + LF
										}
									}
								}
							}
							
							if (jobDatas != null && 0 < jobDatas.Length) return jobDatas;
							break;
						}
						// case ByteError: {
						// 	// -
						// 	var lineEndCursor = ReadLine(sourceBuffer, cursor);
						// 	cursor = cursor + 1;// add header byte size = 1.
							
						// 	if (Failed != null) {
						// 		var errorStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
						// 		// Disquuun.Log("errorStr:" + errorStr);
						// 		Failed(currentCommand, errorStr);
						// 	}
							
						// 	cursor = lineEndCursor + 2;// CR + LF
						// 	break;
						// }
						default: {
							Disquuun.Log("currentCommand:" + currentCommand + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor));
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
							
							// var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
							// Disquuun.Log("countStr:" + countStr);
							
							var countBuffer = new byte[lineEndCursor - cursor];
							Array.Copy(sourceBuffer, cursor, countBuffer, 0, countBuffer.Length);
							
							var byteData = new DisquuunResult(countBuffer);
							
							cursor = lineEndCursor + 2;// CR + LF
							return new DisquuunResult[]{byteData};
						}
						// case ByteError: {
						// 	// -
						// 	var lineEndCursor = ReadLine(sourceBuffer, cursor);
						// 	cursor = cursor + 1;// add header byte size = 1.
							
						// 	if (Failed != null) {
						// 		var errorStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
						// 		// Disquuun.Log("errorStr:" + errorStr);
						// 		Failed(currentCommand, errorStr);
						// 	}
							
						// 	cursor = lineEndCursor + 2;// CR + LF
						// 	break;
						// }
						default: {
							Disquuun.Log("currentCommand:" + currentCommand + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor));
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
							
							// var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
							// Disquuun.Log("countStr:" + countStr);
							
							var countBuffer = new byte[lineEndCursor - cursor];
							Array.Copy(sourceBuffer, cursor, countBuffer, 0, countBuffer.Length);
							
							var byteData = new DisquuunResult(countBuffer);
							
							cursor = lineEndCursor + 2;// CR + LF
							return new DisquuunResult[]{byteData};
						}
						// case ByteError: {
						// 	// -NOJOB Job not known in the context of this node.
						// 	var lineEndCursor = ReadLine(sourceBuffer, cursor);
						// 	cursor = cursor + 1;// add header byte size = 1.
							
						// 	if (Failed != null) {
						// 		var errorStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
						// 		// Disquuun.Log("errorStr:" + errorStr);
						// 		Failed(currentCommand, errorStr);
						// 	}
							
						// 	cursor = lineEndCursor + 2;// CR + LF
						// 	break;
						// }
						default: {
							Disquuun.Log("currentCommand:" + currentCommand + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor));
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
							
							// var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
							// Disquuun.Log("countStr:" + countStr);
							
							var countBuffer = new byte[lineEndCursor - cursor];
							Array.Copy(sourceBuffer, cursor, countBuffer, 0, countBuffer.Length);
							
							var byteData = new DisquuunResult(countBuffer);
							
							cursor = lineEndCursor + 2;// CR + LF
							return new DisquuunResult[]{byteData};
						}
						// case ByteError: {
						// 	// -
						// 	var lineEndCursor = ReadLine(sourceBuffer, cursor);
						// 	cursor = cursor + 1;// add header byte size = 1.
							
						// 	if (Failed != null) {
						// 		var errorStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
						// 		// Disquuun.Log("errorStr:" + errorStr);
						// 		Failed(currentCommand, errorStr);
						// 	}
							
						// 	cursor = lineEndCursor + 2;// CR + LF
						// 	break;
						// }
						default: {
							Disquuun.Log("currentCommand:" + currentCommand + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor));
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
							
							var newBuffer = new byte[countNum];
							Array.Copy(sourceBuffer, cursor, newBuffer, 0, countNum);
							
							cursor = cursor + countNum + 2;// CR + LF
							
							return new DisquuunResult[]{new DisquuunResult(newBuffer)};
						}
						default: {
							Disquuun.Log("currentCommand:" + currentCommand + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor));
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
								// Disquuun.Log("bulkCountStr:" + bulkCountStr);
								
								cursor = lineEndCursor + 2;// CR + LF
							}
							
							{
								// : format version
								var lineEndCursor = ReadLine(sourceBuffer, cursor);
								cursor = cursor + 1;// add header byte size = 1.
								
								version = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
								// Disquuun.Log(":version:" + version);
								
								cursor = lineEndCursor + 2;// CR + LF
							}
							
							{
								// $ this node id
								var lineEndCursor = ReadLine(sourceBuffer, cursor);
								cursor = cursor + 1;// add header byte size = 1.
								
								var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
								var strNum = Convert.ToInt32(countStr);
								// Disquuun.Log("id strNum:" + strNum);
								
								cursor = lineEndCursor + 2;// CR + LF
								
								thisNodeId = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
								// Disquuun.Log("thisNodeId:" + thisNodeId);
								
								cursor = cursor + strNum + 2;// CR + LF
							}
							
							{
								// * node ids
								var lineEndCursor = ReadLine(sourceBuffer, cursor);
								cursor = cursor + 1;// add header byte size = 1.
								
								var bulkCountStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
								var bulkCountNum = Convert.ToInt32(bulkCountStr);
								// Disquuun.Log("bulkCountNum:" + bulkCountNum);
								
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
							
							
							var versionBytes = Encoding.UTF8.GetBytes(version);
							var thisNodeIdBytes = Encoding.UTF8.GetBytes(thisNodeId);
							
							var byteDatas = new DisquuunResult[1 + nodeIdsAndInfos.Count/4];
							byteDatas[0] = new DisquuunResult(versionBytes,thisNodeIdBytes);
							
							for (var index = 0; index < nodeIdsAndInfos.Count/4; index++) {
								var nodeId = Encoding.UTF8.GetBytes(nodeIdsAndInfos[index*4 + 0]);
								var ip = Encoding.UTF8.GetBytes(nodeIdsAndInfos[index*4 + 1]);
								var port = Encoding.UTF8.GetBytes(nodeIdsAndInfos[index*4 + 2]);
								var priority = Encoding.UTF8.GetBytes(nodeIdsAndInfos[index*4 + 3]);
								
								byteDatas[index + 1] = new DisquuunResult(nodeId, ip, port, priority);
							}
							
							return byteDatas;
						}
						default: {
							Disquuun.Log("currentCommand:" + currentCommand + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor));
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
							
							var byteData = new DisquuunResult(countBuffer);
							
							cursor = lineEndCursor + 2;// CR + LF
							
							return new DisquuunResult[]{byteData};
						}
						default: {
							Disquuun.Log("currentCommand:" + currentCommand + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor));
							cursor = bytesTransferred;
							break;
						}
					}
					break;
				}
				case DisqueCommand.QSTAT: {
					var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					cursor = bytesTransferred;
					break;
				}
				case DisqueCommand.QPEEK: {
					var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					cursor = bytesTransferred;
					break;
				}
				case DisqueCommand.ENQUEUE: {
					var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					cursor = bytesTransferred;
					break;
				}
				case DisqueCommand.DEQUEUE: {
					var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					cursor = bytesTransferred;
					break;
				}
				case DisqueCommand.DELJOB: {
					var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					cursor = bytesTransferred;
					break;
				}
				case DisqueCommand.SHOW: {
					var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					cursor = bytesTransferred;
					break;
				}
				case DisqueCommand.QSCAN: {
					var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					cursor = bytesTransferred;
					break;
				}
				case DisqueCommand.JSCAN: {
					var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					cursor = bytesTransferred;
					break;
				}
				case DisqueCommand.PAUSE: {
					var data = Encoding.UTF8.GetString(sourceBuffer, cursor, bytesTransferred - cursor);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					cursor = bytesTransferred;
					break;
				}
				default: {
					Disquuun.Log("unknown command:" + currentCommand);
					break;
				}
			}
			
			return null;
		}
		
	
		public static int ReadLine (byte[] bytes, int cursor) {
			do {
				if (bytes[cursor] == ByteLF) break;
				cursor++;
			} while (cursor < bytes.Length);
			
			return cursor - 1;
		}
	}
}