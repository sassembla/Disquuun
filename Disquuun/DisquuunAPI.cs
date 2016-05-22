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
		
		public static byte[] GetJob (string[] queueIds, object[] args) {
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
			return ToBytes(DisqueCommand.QLEN, queueId);
		}
		
		public static byte[] Qstat (string queueId) {
			return ToBytes(DisqueCommand.QSTAT, queueId);
		}
		
		public static byte[] Qpeek (string queueId, int count) {
			return ToBytes(DisqueCommand.QPEEK, queueId, count);
		}
		
		public static byte[] Enqueue (string[] jobIds) {
			return ToBytes(DisqueCommand.ENQUEUE, jobIds);
		}
		
		public static byte[] Dequeue (string[] jobIds) {
			return ToBytes(DisqueCommand.DEQUEUE, jobIds);
		}
		
		public static byte[] DelJob (string[] jobIds) {
			return ToBytes(DisqueCommand.DELJOB, jobIds);
		}
		
		public static byte[] Show (string jobId) {
			return ToBytes(DisqueCommand.SHOW, jobId);
		}
		
		public static byte[] Qscan (object[] args) {
			return ToBytes(DisqueCommand.QSCAN, args);
		}
		
		public static byte[] Jscan (int cursor, object[] args) {
			return ToBytes(DisqueCommand.JSCAN, cursor, args);
		}
		
		public static byte[] Pause (string queueId, string option1, string[] options) {
			return ToBytes(DisqueCommand.JSCAN, queueId, option1, options);
		}
		
		
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
		
		public struct ScanResult {
			public readonly bool isDone;
			public readonly DisquuunResult[] data;
			public ScanResult (bool isDone, DisquuunResult[] data) {
				this.isDone = isDone;
				this.data = data;
			}
			public ScanResult (bool isDone) {
				this.isDone = isDone;
				this.data = null;
			}
		}
		
		public static ScanResult ScanBuffer (DisqueCommand command, byte[] sourceBuffer, long length) {
			var cursor = 0;
			
			switch (command) {
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
							// + count
							var lineEndCursor = ReadLine2(sourceBuffer, cursor, length);// ReadLine2が成立すれば、次のCRLFまでは読める。
							if (lineEndCursor == -1) return new ScanResult(false); 
							cursor = cursor + 1;// add header byte size = 1.
							
							// var idStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
							// Disquuun.Log("idStr:" + idStr);
							
							var countBuffer = new byte[lineEndCursor - cursor];
							Array.Copy(sourceBuffer, cursor, countBuffer, 0, lineEndCursor - cursor);
							
							cursor = lineEndCursor + 2;// CR + LF
							
							return new ScanResult(true, new DisquuunResult[]{new DisquuunResult(countBuffer)});
						}
						default: {
							Disquuun.Log("command:" + command + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer));
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
								// * count.
								var lineEndCursor = ReadLine2(sourceBuffer, cursor, length);
								if (lineEndCursor == -1) return new ScanResult(false);
								
								cursor = cursor + 1;// add header byte size = 1.
								
								var bulkCountStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
								// TestLogger.Log("bulkCountStr:" + bulkCountStr);
								var bulkCountNum = Convert.ToInt32(bulkCountStr);
								
								cursor = lineEndCursor + 2;// CR + LF
								
								
								// trigger when GETJOB NOHANG
								if (bulkCountNum < 0) return new ScanResult(true, new DisquuunResult[]{});
								
								
								jobDatas = new DisquuunResult[bulkCountNum];
								for (var i = 0; i < bulkCountNum; i++) {
									var itemCount = 0;
									
									{
										// * count.
										var lineEndCursor2 = ReadLine2(sourceBuffer, cursor, length);
										if (lineEndCursor2 == -1) return new ScanResult(false);
									
										cursor = cursor + 1;// add header byte size = 1.
										
										var bulkCountStr2 = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
										
										itemCount = Convert.ToInt32(bulkCountStr2);
										// Disquuun.Log("itemCount:" + itemCount);
										
										cursor = lineEndCursor2 + 2;// CR + LF
									}
									
									// queueName
									{
										// $ count.
										var lineEndCursor3 = ReadLine2(sourceBuffer, cursor, length);
										if (lineEndCursor3 == -1) return new ScanResult(false);
										
										cursor = cursor + 1;// add header byte size = 1											
										
										var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor3 - cursor);
										var strNum = Convert.ToInt32(countStr);
										
										cursor = lineEndCursor3 + 2;// CR + LF
										
										// $ bulk.
										if (ShortageOfReadableLength(sourceBuffer, cursor, strNum)) return new ScanResult(false);
										// var nameStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
										// Disquuun.Log("nameStr:" + nameStr);
										
										cursor = cursor + strNum + 2;// CR + LF
									}
									
									// jobId
									byte[] jobIdBytes;
									{
										// $ count.
										var lineEndCursor3 = ReadLine2(sourceBuffer, cursor, length);
										if (lineEndCursor3 == -1) return new ScanResult(false);
										
										cursor = cursor + 1;// add header byte size = 1.
										
										var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor3 - cursor);
										var strNum = Convert.ToInt32(countStr);
										// Disquuun.Log("id strNum:" + strNum);
										
										cursor = lineEndCursor3 + 2;// CR + LF
										
										
										// $ bulk.
										if (ShortageOfReadableLength(sourceBuffer, cursor, strNum)) return new ScanResult(false);
										jobIdBytes = new byte[strNum];
										Array.Copy(sourceBuffer, cursor, jobIdBytes, 0, strNum);
										// var jobIdStr = Encoding.UTF8.GetString(jobIdBytes);
										// Disquuun.Log("jobIdStr:" + jobIdStr);
										
										cursor = cursor + strNum + 2;// CR + LF
									}
									
									
									// jobData
									if (itemCount == 3) {
										// $ count.
										var lineEndCursor3 = ReadLine2(sourceBuffer, cursor, length);
										if (lineEndCursor3 == -1) return new ScanResult(false);
										
										cursor = cursor + 1;// add header byte size = 1.
										
										var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor3 - cursor);
										var strNum = Convert.ToInt32(countStr);
										
										cursor = lineEndCursor3 + 2;// CR + LF
										
										
										// $ bulk.
										if (ShortageOfReadableLength(sourceBuffer, cursor, strNum)) return new ScanResult(false);
										var dataBytes = new byte[strNum];
										Array.Copy(sourceBuffer, cursor, dataBytes, 0, strNum);
										
										cursor = cursor + strNum + 2;// CR + LF
										
										jobDatas[i] = new DisquuunResult(jobIdBytes, dataBytes);
										continue;
									}
									
									TestLogger.Log("そのうち対応する、countersとかオプションつけたやつ。");
									// using additional info flag for getjob... not yet applied.
									// if (itemCount == 7) {
									// 	byte[] nackCountBytes;
									// 	{
									// 		// $
									// 		var lineEndCursor3 = ReadLine2(sourceBuffer, cursor, length);
									// 		cursor = cursor + 1;// add header byte size = 1.
											
									// 		var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor3 - cursor);
									// 		var strNum = Convert.ToInt32(countStr);
									// 		// Disquuun.Log("data strNum:" + strNum);
											
									// 		cursor = lineEndCursor3 + 2;// CR + LF
											
									// 		// ignore params. 
										
									// 		cursor = cursor + strNum + 2;// CR + LF
										
									// 		// :
									// 		var lineEndCursor4 = ReadLine2(sourceBuffer, cursor, length);
									// 		cursor = cursor + 1;// add header byte size = 1.
											
									// 		nackCountBytes = new byte[lineEndCursor4 - cursor];
									// 		Array.Copy(sourceBuffer, cursor, nackCountBytes, 0, nackCountBytes.Length);
											
									// 		cursor = lineEndCursor4 + 2;// CR + LF
									// 	}
										
									// 	byte[] additionalDeliveriesCountBytes;
									// 	{
									// 		// $
									// 		var lineEndCursor3 = ReadLine2(sourceBuffer, cursor, length);
									// 		cursor = cursor + 1;// add header byte size = 1.
											
									// 		var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor3 - cursor);
									// 		var strNum = Convert.ToInt32(countStr);
									// 		// Disquuun.Log("data strNum:" + strNum);
											
									// 		cursor = lineEndCursor3 + 2;// CR + LF
											
									// 		// ignore params. 
										
									// 		cursor = cursor + strNum + 2;// CR + LF
										
									// 		// :
									// 		var lineEndCursor4 = ReadLine2(sourceBuffer, cursor, length);
									// 		cursor = cursor + 1;// add header byte size = 1.
											
									// 		additionalDeliveriesCountBytes = new byte[lineEndCursor4 - cursor];
									// 		Array.Copy(sourceBuffer, cursor, additionalDeliveriesCountBytes, 0, additionalDeliveriesCountBytes.Length);
											
									// 		jobDatas[i] = new DisquuunResult(jobIdBytes, dataBytes, nackCountBytes, additionalDeliveriesCountBytes);
											
									// 		cursor = lineEndCursor4 + 2;// CR + LF
									// 	}
									// }
								}
							}
							
							if (jobDatas != null && 0 < jobDatas.Length) return new ScanResult(true, jobDatas);
							break;
						}
						// case ByteError: {
						// 	// -
						// 	var lineEndCursor = ReadLine2(sourceBuffer, cursor, length);
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
							Disquuun.Log("command:" + command + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer));
							break;
						}
					}
					break;
				}
				case DisqueCommand.ACKJOB:
				case DisqueCommand.FASTACK: {
					switch (sourceBuffer[cursor]) {
						case ByteInt: {
							// : count.
							var lineEndCursor = ReadLine2(sourceBuffer, cursor, length);
							if (lineEndCursor == -1) return new ScanResult(false); 
							cursor = cursor + 1;// add header byte size = 1.
							
							// var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
							// Disquuun.Log("countStr:" + countStr);
							
							var countBuffer = new byte[lineEndCursor - cursor];
							Array.Copy(sourceBuffer, cursor, countBuffer, 0, countBuffer.Length);
							
							var byteData = new DisquuunResult(countBuffer);
							
							cursor = lineEndCursor + 2;// CR + LF
							return new ScanResult(true, new DisquuunResult[]{byteData});
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
							Disquuun.Log("command:" + command + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer));
							break;
						}
					}
					break;
				}
				case DisqueCommand.INFO: {
					switch (sourceBuffer[cursor]) {
						case ByteBulk: {
							
							var countNum = 0;
							{// readbulk count.
								// $
								var lineEndCursor = ReadLine2(sourceBuffer, cursor, length);
								if (lineEndCursor == -1) return new ScanResult(false);
								cursor = cursor + 1;// add header byte size = 1.
								
								var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
								countNum = Convert.ToInt32(countStr);
								
								cursor = lineEndCursor + 2;// CR + LF
							}
							
							{// readbulk string.
								if (ShortageOfReadableLength(sourceBuffer, cursor, countNum)) return new ScanResult(false);
								
								var newBuffer = new byte[countNum];
								Array.Copy(sourceBuffer, cursor, newBuffer, 0, countNum);
								
								cursor = cursor + countNum + 2;// CR + LF
								
								return new ScanResult(true, new DisquuunResult[]{new DisquuunResult(newBuffer)});
							}
						}
						default: {
							Disquuun.Log("command:" + command + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer));
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
								var lineEndCursor = ReadLine2(sourceBuffer, cursor, length);
								if (lineEndCursor == -1) return new ScanResult(false);
								cursor = cursor + 1;// add header byte size = 1.
								
								// var bulkCountStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
								// Disquuun.Log("bulkCountStr:" + bulkCountStr);
								
								cursor = lineEndCursor + 2;// CR + LF
							}
							
							{
								// : format version
								var lineEndCursor = ReadLine2(sourceBuffer, cursor, length);
								if (lineEndCursor == -1) return new ScanResult(false);
								
								cursor = cursor + 1;// add header byte size = 1.
								
								version = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
								// Disquuun.Log(":version:" + version);
								
								cursor = lineEndCursor + 2;// CR + LF
							}
							
							{
								// $ this node id
								var lineEndCursor = ReadLine2(sourceBuffer, cursor, length);
								if (lineEndCursor == -1) return new ScanResult(false);
								
								cursor = cursor + 1;// add header byte size = 1.
								
								var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
								var strNum = Convert.ToInt32(countStr);
								// Disquuun.Log("id strNum:" + strNum);
								
								cursor = lineEndCursor + 2;// CR + LF
								
								if (ShortageOfReadableLength(sourceBuffer, cursor, strNum)) return new ScanResult(false);
								thisNodeId = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
								// Disquuun.Log("thisNodeId:" + thisNodeId);
								
								cursor = cursor + strNum + 2;// CR + LF
							}
							
							{
								// * node ids
								var lineEndCursor = ReadLine2(sourceBuffer, cursor, length);
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
										var lineEndCursor2 = ReadLine2(sourceBuffer, cursor, length);
										if (lineEndCursor2 == -1) return new ScanResult(false);
										cursor = cursor + 1;// add header byte size = 1.
										
										var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
										var strNum = Convert.ToInt32(countStr);
										
										cursor = lineEndCursor2 + 2;// CR + LF
										
										if (ShortageOfReadableLength(sourceBuffer, cursor, strNum)) return new ScanResult(false);
										idStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
										nodeIdsAndInfos.Add(idStr);
										
										cursor = cursor + strNum + 2;// CR + LF
									}
									
									{
										var lineEndCursor2 = ReadLine2(sourceBuffer, cursor, length);
										if (lineEndCursor2 == -1) return new ScanResult(false);
										cursor = cursor + 1;// add header byte size = 1.
										
										var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
										var strNum = Convert.ToInt32(countStr);
										
										cursor = lineEndCursor2 + 2;// CR + LF
										
										if (ShortageOfReadableLength(sourceBuffer, cursor, strNum)) return new ScanResult(false);
										var ipStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
										nodeIdsAndInfos.Add(ipStr);
										
										cursor = cursor + strNum + 2;// CR + LF
									}
									
									{
										var lineEndCursor2 = ReadLine2(sourceBuffer, cursor, length);
										if (lineEndCursor2 == -1) return new ScanResult(false);
										cursor = cursor + 1;// add header byte size = 1.
										
										var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
										var strNum = Convert.ToInt32(countStr);
										
										cursor = lineEndCursor2 + 2;// CR + LF
										
										if (ShortageOfReadableLength(sourceBuffer, cursor, strNum)) return new ScanResult(false);
										var portStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
										nodeIdsAndInfos.Add(portStr);
										
										cursor = cursor + strNum + 2;// CR + LF
									}
									
									{
										var lineEndCursor2 = ReadLine2(sourceBuffer, cursor, length);
										if (lineEndCursor2 == -1) return new ScanResult(false);
										cursor = cursor + 1;// add header byte size = 1.
										
										var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
										var strNum = Convert.ToInt32(countStr);
										
										cursor = lineEndCursor2 + 2;// CR + LF
										
										if (ShortageOfReadableLength(sourceBuffer, cursor, strNum)) return new ScanResult(false);
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
							
							return new ScanResult(true, byteDatas);
						}
						default: {
							Disquuun.Log("command:" + command + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer));
							
							break;
						}
					}
					break;
				}
				case DisqueCommand.QLEN: {
					switch (sourceBuffer[cursor]) {
						case ByteInt: {
							// : format version
							var lineEndCursor = ReadLine2(sourceBuffer, cursor, length);
							if (lineEndCursor == -1) return new ScanResult(false);
							cursor = cursor + 1;// add header byte size = 1.
							
							var countBuffer = new byte[lineEndCursor - cursor];
							Array.Copy(sourceBuffer, cursor, countBuffer, 0, countBuffer.Length);
							
							var byteData = new DisquuunResult(countBuffer);
							
							cursor = lineEndCursor + 2;// CR + LF
							
							return new ScanResult(true, new DisquuunResult[]{byteData});
						}
						default: {
							Disquuun.Log("command:" + command + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer));
							
							break;
						}
					}
					break;
				}
				default: {
					Disquuun.Log("command:" + command);
					break;
				}
			}
			return new ScanResult(false);
		}
		
		private static bool ShortageOfReadableLength (byte[] source, int cursor, int length) {
			if (cursor + length < source.Length) return false;
			return true;
		}
		
		public static DisquuunResult[] EvaluateSingleCommand (DisqueCommand currentCommand, byte[] sourceBuffer) {
			var cursor = 0;
			/*
				get data then react.
			*/
			switch (currentCommand) {
				
				// case DisqueCommand.WORKING: {
				// 	switch (sourceBuffer[cursor]) {
				// 		case ByteInt: {
				// 			// :Int count
				// 			var lineEndCursor = ReadLine(sourceBuffer, cursor);
				// 			cursor = cursor + 1;// add header byte size = 1.
							
				// 			// var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
				// 			// Disquuun.Log("countStr:" + countStr);
							
				// 			var countBuffer = new byte[lineEndCursor - cursor];
				// 			Array.Copy(sourceBuffer, cursor, countBuffer, 0, countBuffer.Length);
							
				// 			var byteData = new DisquuunResult(countBuffer);
							
				// 			cursor = lineEndCursor + 2;// CR + LF
				// 			return new DisquuunResult[]{byteData};
				// 		}
				// 		// case ByteError: {
				// 		// 	// -NOJOB Job not known in the context of this node.
				// 		// 	var lineEndCursor = ReadLine(sourceBuffer, cursor);
				// 		// 	cursor = cursor + 1;// add header byte size = 1.
							
				// 		// 	if (Failed != null) {
				// 		// 		var errorStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
				// 		// 		// Disquuun.Log("errorStr:" + errorStr);
				// 		// 		Failed(currentCommand, errorStr);
				// 		// 	}
							
				// 		// 	cursor = lineEndCursor + 2;// CR + LF
				// 		// 	break;
				// 		// }
				// 		default: {
				// 			Disquuun.Log("currentCommand:" + currentCommand + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer));
							
				// 			break;
				// 		}
				// 	}
				// 	break;
				// }
				// case DisqueCommand.NACK: {
				// 	switch (sourceBuffer[cursor]) {
				// 		case ByteInt: {
				// 			// :Int count
				// 			var lineEndCursor = ReadLine(sourceBuffer, cursor);
				// 			cursor = cursor + 1;// add header byte size = 1.
							
				// 			// var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
				// 			// Disquuun.Log("countStr:" + countStr);
							
				// 			var countBuffer = new byte[lineEndCursor - cursor];
				// 			Array.Copy(sourceBuffer, cursor, countBuffer, 0, countBuffer.Length);
							
				// 			var byteData = new DisquuunResult(countBuffer);
							
				// 			cursor = lineEndCursor + 2;// CR + LF
				// 			return new DisquuunResult[]{byteData};
				// 		}
				// 		// case ByteError: {
				// 		// 	// -
				// 		// 	var lineEndCursor = ReadLine(sourceBuffer, cursor);
				// 		// 	cursor = cursor + 1;// add header byte size = 1.
							
				// 		// 	if (Failed != null) {
				// 		// 		var errorStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
				// 		// 		// Disquuun.Log("errorStr:" + errorStr);
				// 		// 		Failed(currentCommand, errorStr);
				// 		// 	}
							
				// 		// 	cursor = lineEndCursor + 2;// CR + LF
				// 		// 	break;
				// 		// }
				// 		default: {
				// 			Disquuun.Log("currentCommand:" + currentCommand + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer));
							
				// 			break;
				// 		}
				// 	}
				// 	break;
				// }
				
				// case DisqueCommand.HELLO: {
				// 	switch (sourceBuffer[cursor]) {
				// 		case ByteMultiBulk: {
				// 			string version;
				// 			string thisNodeId;
				// 			List<string> nodeIdsAndInfos = new List<string>();
				// 			/*
				// 				:*3
				// 					:1 version [0][0]
									
				// 					$40 this node ID [0][1]
				// 						002698920b158ba29ff8d41d3e5303ceaf0e8d45
									
				// 					*4 [1~n][0~3]
				// 						$40
				// 							002698920b158ba29ff8d41d3e5303ceaf0e8d45
										
				// 						$0
				// 							""
										
				// 						$4
				// 							7711
										
				// 						$1
				// 							1
				// 			*/
							
				// 			{
				// 				// *
				// 				var lineEndCursor = ReadLine(sourceBuffer, cursor);
				// 				cursor = cursor + 1;// add header byte size = 1.
								
				// 				// var bulkCountStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
				// 				// Disquuun.Log("bulkCountStr:" + bulkCountStr);
								
				// 				cursor = lineEndCursor + 2;// CR + LF
				// 			}
							
				// 			{
				// 				// : format version
				// 				var lineEndCursor = ReadLine(sourceBuffer, cursor);
				// 				cursor = cursor + 1;// add header byte size = 1.
								
				// 				version = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
				// 				// Disquuun.Log(":version:" + version);
								
				// 				cursor = lineEndCursor + 2;// CR + LF
				// 			}
							
				// 			{
				// 				// $ this node id
				// 				var lineEndCursor = ReadLine(sourceBuffer, cursor);
				// 				cursor = cursor + 1;// add header byte size = 1.
								
				// 				var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
				// 				var strNum = Convert.ToInt32(countStr);
				// 				// Disquuun.Log("id strNum:" + strNum);
								
				// 				cursor = lineEndCursor + 2;// CR + LF
								
				// 				thisNodeId = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
				// 				// Disquuun.Log("thisNodeId:" + thisNodeId);
								
				// 				cursor = cursor + strNum + 2;// CR + LF
				// 			}
							
				// 			{
				// 				// * node ids
				// 				var lineEndCursor = ReadLine(sourceBuffer, cursor);
				// 				cursor = cursor + 1;// add header byte size = 1.
								
				// 				var bulkCountStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor - cursor);
				// 				var bulkCountNum = Convert.ToInt32(bulkCountStr);
				// 				// Disquuun.Log("bulkCountNum:" + bulkCountNum);
								
				// 				cursor = lineEndCursor + 2;// CR + LF
								
				// 				// nodeId, ip, port, priority.
				// 				for (var i = 0; i < bulkCountNum/4; i++) {
				// 					var idStr = string.Empty;
									
				// 					// $ nodeId
				// 					{
				// 						var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
				// 						cursor = cursor + 1;// add header byte size = 1.
										
				// 						var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
				// 						var strNum = Convert.ToInt32(countStr);
										
				// 						cursor = lineEndCursor2 + 2;// CR + LF
										
				// 						idStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
				// 						nodeIdsAndInfos.Add(idStr);
										
				// 						cursor = cursor + strNum + 2;// CR + LF
				// 					}
									
				// 					{
				// 						var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
				// 						cursor = cursor + 1;// add header byte size = 1.
										
				// 						var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
				// 						var strNum = Convert.ToInt32(countStr);
										
				// 						cursor = lineEndCursor2 + 2;// CR + LF
										
				// 						var ipStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
				// 						nodeIdsAndInfos.Add(ipStr);
										
				// 						cursor = cursor + strNum + 2;// CR + LF
				// 					}
									
				// 					{
				// 						var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
				// 						cursor = cursor + 1;// add header byte size = 1.
										
				// 						var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
				// 						var strNum = Convert.ToInt32(countStr);
										
				// 						cursor = lineEndCursor2 + 2;// CR + LF
										
				// 						var portStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
				// 						nodeIdsAndInfos.Add(portStr);
										
				// 						cursor = cursor + strNum + 2;// CR + LF
				// 					}
									
				// 					{
				// 						var lineEndCursor2 = ReadLine(sourceBuffer, cursor);
				// 						cursor = cursor + 1;// add header byte size = 1.
										
				// 						var countStr = Encoding.UTF8.GetString(sourceBuffer, cursor, lineEndCursor2 - cursor);
				// 						var strNum = Convert.ToInt32(countStr);
										
				// 						cursor = lineEndCursor2 + 2;// CR + LF
										
				// 						var priorityStr = Encoding.UTF8.GetString(sourceBuffer, cursor, strNum);
				// 						nodeIdsAndInfos.Add(priorityStr);
										
				// 						cursor = cursor + strNum + 2;// CR + LF
				// 					}
				// 				}
				// 			}
							
							
				// 			var versionBytes = Encoding.UTF8.GetBytes(version);
				// 			var thisNodeIdBytes = Encoding.UTF8.GetBytes(thisNodeId);
							
				// 			var byteDatas = new DisquuunResult[1 + nodeIdsAndInfos.Count/4];
				// 			byteDatas[0] = new DisquuunResult(versionBytes,thisNodeIdBytes);
							
				// 			for (var index = 0; index < nodeIdsAndInfos.Count/4; index++) {
				// 				var nodeId = Encoding.UTF8.GetBytes(nodeIdsAndInfos[index*4 + 0]);
				// 				var ip = Encoding.UTF8.GetBytes(nodeIdsAndInfos[index*4 + 1]);
				// 				var port = Encoding.UTF8.GetBytes(nodeIdsAndInfos[index*4 + 2]);
				// 				var priority = Encoding.UTF8.GetBytes(nodeIdsAndInfos[index*4 + 3]);
								
				// 				byteDatas[index + 1] = new DisquuunResult(nodeId, ip, port, priority);
				// 			}
							
				// 			return byteDatas;
				// 		}
				// 		default: {
				// 			Disquuun.Log("currentCommand:" + currentCommand + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer));
							
				// 			break;
				// 		}
				// 	}
				// 	break;
				// }
				case DisqueCommand.QLEN: {
					// switch (sourceBuffer[cursor]) {
					// 	case ByteInt: {
					// 		// : format version
					// 		var lineEndCursor = ReadLine(sourceBuffer, cursor);
					// 		cursor = cursor + 1;// add header byte size = 1.
							
					// 		var countBuffer = new byte[lineEndCursor - cursor];
					// 		Array.Copy(sourceBuffer, cursor, countBuffer, 0, countBuffer.Length);
							
					// 		var byteData = new DisquuunResult(countBuffer);
							
					// 		cursor = lineEndCursor + 2;// CR + LF
							
					// 		return new DisquuunResult[]{byteData};
					// 	}
					// 	default: {
					// 		Disquuun.Log("currentCommand:" + currentCommand + " unhandled:" + sourceBuffer[cursor] + " data:" + Encoding.UTF8.GetString(sourceBuffer));
							
					// 		break;
					// 	}
					// }
					break;
				}
				case DisqueCommand.QSTAT: {
					var data = Encoding.UTF8.GetString(sourceBuffer);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					
					break;
				}
				case DisqueCommand.QPEEK: {
					var data = Encoding.UTF8.GetString(sourceBuffer);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					
					break;
				}
				case DisqueCommand.ENQUEUE: {
					var data = Encoding.UTF8.GetString(sourceBuffer);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					
					break;
				}
				case DisqueCommand.DEQUEUE: {
					var data = Encoding.UTF8.GetString(sourceBuffer);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					
					break;
				}
				case DisqueCommand.DELJOB: {
					var data = Encoding.UTF8.GetString(sourceBuffer);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					
					break;
				}
				case DisqueCommand.SHOW: {
					var data = Encoding.UTF8.GetString(sourceBuffer);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					
					break;
				}
				case DisqueCommand.QSCAN: {
					var data = Encoding.UTF8.GetString(sourceBuffer);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					
					break;
				}
				case DisqueCommand.JSCAN: {
					var data = Encoding.UTF8.GetString(sourceBuffer);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					
					break;
				}
				case DisqueCommand.PAUSE: {
					var data = Encoding.UTF8.GetString(sourceBuffer);
					Disquuun.Log("not yet applied:" + currentCommand + " data:" + data);
					
					break;
				}
				default: {
					Disquuun.Log("unknown command:" + currentCommand);
					break;
				}
			}
			
			return null;
		}
		
		
		public static int ReadLine2 (byte[] bytes, int cursor, long length) {
			while (cursor < length) {
				if (bytes[cursor] == ByteLF) return cursor - 1;
				cursor++;
			}
			
			// Disquuun.Log("overflow detected.");
			return -1;
		}
	}
}