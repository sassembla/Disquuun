using System;
using System.IO;
using System.Linq;
using System.Text;
using DisquuunCore;
using DisquuunCore.Deserialize;

public class DisquuunTests {

    public static void RunDisquuunTests () {
		try {
			
			new Test1_AllAPIs();
			
		} catch (Exception e) {
			TestLogger.Log("e:" + e);	
		}
	}
	
	public class Test1_AllAPIs : TestBase {
		public override Action[] Ready () {
			return new Action[]{
				// add -> get -> ack.
				() => {
					jobQueueId = Guid.NewGuid().ToString();
				},
				() => disquuun.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0),
				() => disquuun.GetJob(new string[]{jobQueueId}),
				() => disquuun.AckJob(new string[]{latestGotJobId}),
				() => AssertResult("ACKJOB:1", latestResult, "add -> get -> ack."),
				
				// add -> get -> fastack.
				() => {
					jobQueueId = Guid.NewGuid().ToString();
				},
				() => disquuun.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0),
				() => disquuun.GetJob(new string[]{jobQueueId}),
				() => disquuun.FastAck(new string[]{latestGotJobId}),
				() => AssertResult("FASTACK:1", latestResult, "add -> get -> fastack."),
				
				// empty queue, will waiting data.
				() => {
					jobQueueId = Guid.NewGuid().ToString();
				},
				() => disquuun.GetJob(new string[]{jobQueueId}),
				() => disquuun2.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0),
				() => disquuun.FastAck(new string[]{latestGotJobId}),
				() => AssertResult("FASTACK:1", latestResult, "empty queue, will waiting data."),
				
				// non exist queue. never back until created.
				() => {
					jobQueueId = Guid.NewGuid().ToString();
				},
				() => disquuun.GetJob(new string[]{jobQueueId}),
				() => disquuun2.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0),
				() => disquuun.FastAck(new string[]{latestGotJobId}),
				() => AssertResult("FASTACK:1", latestResult, "non exist queue. never back until created."),
				
				// info blocking with empty queue.
				() => {
					jobQueueId = Guid.NewGuid().ToString();
				},
				() => disquuun.GetJob(new string[]{jobQueueId}),
				() => disquuun.Info(),
				() => disquuun2.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0),
				() => AssertResult("INFO:", latestResult, "info blocking with empty queue.1"),
				() => disquuun.FastAck(new string[]{latestGotJobId}),
				() => AssertResult("FASTACK:1", latestResult, "info blocking with empty queue.2"),
				
				// non blocking with empty queue.
				// () => disquuun.GetJob(new string[]{"testS"}, "NOHANG"),
				// () => disquuun.Info(),
				
				// info
				() => disquuun.Info(),
				() => AssertResult("INFO:", latestResult, "info"),
				
				// hello
				() => disquuun.Hello(),
				() => AssertResult("HELLO:", latestResult, "hello"),
				
				// qlen returns 0.
				() => {
					jobQueueId = Guid.NewGuid().ToString();
				},
				() => disquuun.Qlen(jobQueueId),
				() => AssertResult("QLEN:0", latestResult, "qlen returns 0."),
				
				// qlen returns 1.
				() => {
					jobQueueId = Guid.NewGuid().ToString();
				},
				() => disquuun.AddJob(jobQueueId, new byte[]{1}),
				() => disquuun.Qlen(jobQueueId),
				() => AssertResult("QLEN:1", latestResult, "qlen returns 1."),
				() => disquuun.GetJob(new string[]{jobQueueId}),
				() => disquuun.FastAck(new string[]{latestGotJobId}),
				
				// working return error with unused jobid.
				() => disquuun.Working("dummyJobId"),
				() => AssertFailureResult("BADID Invalid Job ID format.", latestError, "working return error with unused job id."),
				
				// working return \"seconds\".
				// () => disquuun.AddJob("working return \"seconds\"", new byte[]{1,3,5,7,9}),
				// () => disquuun.Working(latestAddedJobId),
				// () => AssertResult("WORKING:300", latestResult, "working return \"seconds\"."),
				// () => disquuun.GetJob(new string[]{"working return \"seconds\""}),
				// () => disquuun.FastAck(new string[]{latestGotJobId}),
				
				// nack return error with dummy job id.
				() => disquuun.Nack(new string[]{"dummyJobId"}),
				() => AssertFailureResult("BADID Invalid Job ID format.", latestError, "nack return error with dummy job id."),
				
				// nack succeeded with status.
				// まだうまくいってない気がする。
				// () => disquuun.AddJob("testNack", new byte[]{1,3,5,7,9}),
				// () => disquuun.Nack(new string[]{latestAddedJobId}),
				// () => AssertResult("NACK:1", latestResult, "nack succeeded with status."),
				// () => disquuun.GetJob(new string[]{"testNack"}),
				// () => disquuun.FastAck(new string[]{latestGotJobId}),
				
				
				// some job.
				() => {
					jobQueueId = Guid.NewGuid().ToString();
				},
				() => {
					for (var i = 0; i < 2; i++) {
						disquuun2.AddJob(jobQueueId, new byte[]{0});
					}
				},
				() => disquuun.GetJob(new string[]{jobQueueId}, "COUNT", 10),
				() => disquuun.FastAck(latestWholeGotJobId),
				() => AssertResult("FASTACK:2", latestResult, "some job."),
				
				// mass job.
				// () => {
				// 	jobQueueId = Guid.NewGuid().ToString();
				// },
				// () => {
				// 	for (var i = 0; i < 10; i++) {
				// 		disquuun2.AddJob(jobQueueId, new byte[]{0});
				// 	}
				// },
				// () => disquuun.GetJob(new string[]{jobQueueId}, "COUNT", 10),
				// () => disquuun.FastAck(latestWholeGotJobId),
				// () => AssertResult("FASTACK:10", latestResult, "mass job."),
				
				// () => 
				// () => 
				// () => 
				// () => 
				
				// multiple data in same time.
				() => {
					disquuun.Info();
					disquuun.Info();
					disquuun.Info();
					disquuun.Info();
					disquuun.Info();
					disquuun.Info();
				},
				
				() => {
					TestLogger.Log("---------------------------last info.---------------------------");
					disquuun2.Info();
				},
				() => {
					disquuun.Disconnect();
					disquuun2.Disconnect();
					TestLogger.Log("done.");
				}
			};
		}
	}
	
	public class TestBase {
		private int index;
		
		public string latestAddedJobId;
		public string latestGotJobId;
		public string[] latestWholeGotJobId;
		
		public string latestResult;
		
		public string latestError;
		
		public string jobQueueId;
	
		public Action[] acts;
		
		public Disquuun disquuun;
		public Disquuun disquuun2;
		
		public TestBase () {
			disquuun2 = new Disquuun(
				Guid.NewGuid().ToString(),
				"127.0.0.1",
				7711,
				5000,
				(connectionId) => {
					Setup();
				},
				(command, byteDatas) => {
					switch (command) {
						case Disquuun.DisqueCommand.INFO: {
							var infoStr = DisquuunDeserializer.Info(byteDatas);
							TestLogger.Log("disquuun2 infoStr:" + infoStr);
							break;
						}
						// default:
					}
				}
			);
		}
		
		private void Setup () {
			var conId = Guid.NewGuid().ToString();
			disquuun = new Disquuun(
				conId,
				"127.0.0.1",
				7711,
				102400,
				(connectionId) => {
					acts = Ready();
					index = 0;
					new Updater("disquuunTestThread_" + conId, Run);
				},
				(command, byteDatas) => {
					// TestLogger.Log("// data received:" + command + " byteDatas:" + byteDatas.Length);
					
					switch (command) {
						case Disquuun.DisqueCommand.ADDJOB: {
							var addedJobId = DisquuunDeserializer.AddJob(byteDatas);
							// TestLogger.Log("addedJobId:" + addedJobId);
							
							latestAddedJobId = addedJobId;
							latestResult = "ADDJOB:";
							break;
						}
						case Disquuun.DisqueCommand.GETJOB: {
							var jobDatas = DisquuunDeserializer.GetJob(byteDatas);
							foreach (var jobData in jobDatas) {
								var gotJobIdStr = jobData.jobId;
								// TestLogger.Log("gotJobIdStr:" + gotJobIdStr);
								
								latestGotJobId = gotJobIdStr;
							}
							latestWholeGotJobId = jobDatas.Select(j => j.jobId).ToArray();
							latestResult = "GETJOB:" + jobDatas.Length;
							break;
						}
						case Disquuun.DisqueCommand.ACKJOB: {
							var result = DisquuunDeserializer.AckJob(byteDatas);
							// TestLogger.Log("ackjob result:" + result);
							latestResult = "ACKJOB:" + result;
							break;
						}
						case Disquuun.DisqueCommand.FASTACK: {
							var result = DisquuunDeserializer.FastAck(byteDatas);
							// TestLogger.Log("fastack result:" + result);
							latestResult = "FASTACK:" + result;
							break;
						}
						case Disquuun.DisqueCommand.WORKING: {
							var postponeSec = DisquuunDeserializer.Working(byteDatas);
							// TestLogger.Log("working postponeSec:" + postponeSec);
							latestResult = "WORKING:" + postponeSec;
							break;
						}
						case Disquuun.DisqueCommand.NACK: {
							var result = DisquuunDeserializer.Nack(byteDatas);
							// TestLogger.Log("nack result:" + result);
							latestResult = "NACK:" + result;
							break;
						}			
						case Disquuun.DisqueCommand.INFO: {
							var infoStr = DisquuunDeserializer.Info(byteDatas);
							// TestLogger.Log("infoStr:" + infoStr);
							latestResult = "INFO:";
							break;
						}
						case Disquuun.DisqueCommand.HELLO: {
							var helloData = DisquuunDeserializer.Hello(byteDatas);
							// TestLogger.Log("helloData	vr:" + helloData.version);
							// TestLogger.Log("helloData	id:" + helloData.sourceNodeId);
							
							// TestLogger.Log("helloData	node Id:" + helloData.nodeDatas[0].nodeId);
							// TestLogger.Log("helloData	node ip:" + helloData.nodeDatas[0].ip);
							// TestLogger.Log("helloData	node pt:" + helloData.nodeDatas[0].port);
							// TestLogger.Log("helloData	node pr:" + helloData.nodeDatas[0].priority);
							latestResult = "HELLO:";
							break;
						}
						case Disquuun.DisqueCommand.QLEN: {
							var qLengthInt = DisquuunDeserializer.Qlen(byteDatas);
							// TestLogger.Log("qLengthInt:" + qLengthInt);
							latestResult = "QLEN:" + qLengthInt;
							break;
						}
						
						// QSTAT,// <queue-name>
						// QPEEK,// <queue-name> <count>
						// ENQUEUE,// <job-id> ... <job-id>
						// DEQUEUE,// <job-id> ... <job-id>
						// DELJOB,// <job-id> ... <job-id>
						// SHOW,// <job-id>
						// QSCAN,// [COUNT <count>] [BUSYLOOP] [MINLEN <len>] [MAXLEN <len>] [IMPORTRATE <rate>]
						// JSCAN,// [<cursor>] [COUNT <count>] [BUSYLOOP] [QUEUE <queue>] [STATE <state1> STATE <state2> ... STATE <stateN>] [REPLY all|id]
						// PAUSE,
						default: {
							// ignored
							break;
						}
					}
				},
				(failedCommand, reason) => {
					// TestLogger.Log("failedCommand:" + failedCommand + " reason:" + reason);
					latestError = reason;
				}
			);
		}
		
		public virtual Action[] Ready () {
			return null;
		}
		
        private bool Run() {
			acts[index]();
			index++;
            return true;
        }
		
		public void AssertResult(string expectedJobResult, string actualLatestJobResult, string message) {
			if (expectedJobResult == actualLatestJobResult) TestLogger.Log("PASSED:" + message);
			else TestLogger.Log("FAILED:" + message + " actual:" + actualLatestJobResult);
		}
		
		public void AssertFailureResult(string expectedJobFailedResult, string actualLatestJobFailedResult, string message) {
			if (expectedJobFailedResult == actualLatestJobFailedResult) TestLogger.Log("PASSED:" + message);
			else TestLogger.Log("FAILED:" + message + " actual:" + actualLatestJobFailedResult);
		}
    }
}

public class TestLogger {
	private const string logPath = "test.log";
	
	public static void Log (string message) {
		WriteLog(message);
	}
	
	public static void LogWarning (string message) {
		WriteLog("WARNING:" + message);
	}
	
	public static void LogError (string message) {
		WriteLog("ERROR:" + message);
		WriteLog("stacktrace:" + Environment.StackTrace);
	}
	
	public static void WriteLog (string message) {

		// file write
		using (var fs = new FileStream(
			logPath,
			FileMode.Append,
			FileAccess.Write,
			FileShare.ReadWrite)
		) {
			using (var sr = new StreamWriter(fs)) {
				sr.WriteLine("log:" + message);
			}
		}
	}
} 