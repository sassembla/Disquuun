using System;
using System.Linq;
using System.Threading;
using DisquuunCore;
using DisquuunCore.Deserialize;

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
	
	public TestLogger testLogger;
	
	public TestBase () {
		testLogger = new TestLogger();
		
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
						testLogger.Log("disquuun2 infoStr:" + infoStr);
						break;
					}
					// default:
				}
			}
		);
	}
	
	/*
		threadやめよう、無理だ。
		ienunにしようかな。
		runnerとして、特定のmethodを提供する感じにするか。1receiveを単位にする。successとfail、ともに1receiveと捉える。
	*/
	private void Setup () {
		var conId = Guid.NewGuid().ToString();
		
		disquuun = new Disquuun(
			conId,
			"127.0.0.1",
			7711,
			102400,
			(connectionId) => {
				acts = Ready(connectionId);
				index = 0;
				new TestUpdater("disquuunTestThread_" + connectionId, Run);
			},
			(command, byteDatas) => {
				// testLogger.Log("// data received:" + command + " byteDatas:" + byteDatas.Length);
				
				switch (command) {
					case Disquuun.DisqueCommand.ADDJOB: {
						var addedJobId = DisquuunDeserializer.AddJob(byteDatas);
						// testLogger.Log("addedJobId:" + addedJobId);
						
						latestAddedJobId = addedJobId;
						latestResult = "ADDJOB:";
						break;
					}
					case Disquuun.DisqueCommand.GETJOB: {
						var jobDatas = DisquuunDeserializer.GetJob(byteDatas);
						foreach (var jobData in jobDatas) {
							var gotJobIdStr = jobData.jobId;
							// testLogger.Log("gotJobIdStr:" + gotJobIdStr);
							
							latestGotJobId = gotJobIdStr;
						}
						latestWholeGotJobId = jobDatas.Select(j => j.jobId).ToArray();
						latestResult = "GETJOB:" + jobDatas.Length;
						break;
					}
					case Disquuun.DisqueCommand.ACKJOB: {
						var result = DisquuunDeserializer.AckJob(byteDatas);
						// testLogger.Log("ackjob result:" + result);
						latestResult = "ACKJOB:" + result;
						break;
					}
					case Disquuun.DisqueCommand.FASTACK: {
						var result = DisquuunDeserializer.FastAck(byteDatas);
						// testLogger.Log("fastack result:" + result);
						latestResult = "FASTACK:" + result;
						break;
					}
					case Disquuun.DisqueCommand.WORKING: {
						var postponeSec = DisquuunDeserializer.Working(byteDatas);
						// testLogger.Log("working postponeSec:" + postponeSec);
						latestResult = "WORKING:" + postponeSec;
						break;
					}
					case Disquuun.DisqueCommand.NACK: {
						var result = DisquuunDeserializer.Nack(byteDatas);
						// testLogger.Log("nack result:" + result);
						latestResult = "NACK:" + result;
						break;
					}			
					case Disquuun.DisqueCommand.INFO: {
						var infoStr = DisquuunDeserializer.Info(byteDatas);
						testLogger.Log("infoStr:" + infoStr);
						latestResult = "INFO:";
						break;
					}
					case Disquuun.DisqueCommand.HELLO: {
						var helloData = DisquuunDeserializer.Hello(byteDatas);
						// testLogger.Log("helloData	vr:" + helloData.version);
						// testLogger.Log("helloData	id:" + helloData.sourceNodeId);
						
						// testLogger.Log("helloData	node Id:" + helloData.nodeDatas[0].nodeId);
						// testLogger.Log("helloData	node ip:" + helloData.nodeDatas[0].ip);
						// testLogger.Log("helloData	node pt:" + helloData.nodeDatas[0].port);
						// testLogger.Log("helloData	node pr:" + helloData.nodeDatas[0].priority);
						latestResult = "HELLO:";
						break;
					}
					case Disquuun.DisqueCommand.QLEN: {
						var qLengthInt = DisquuunDeserializer.Qlen(byteDatas);
						// testLogger.Log("qLengthInt:" + qLengthInt);
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
				// testLogger.Log("failedCommand:" + failedCommand + " reason:" + reason);
				latestError = reason;
			}
		);
	}
	
	public virtual Action[] Ready (string testSuiteId) {
		return null;
	}
	
	private bool Run() {
		if (index < acts.Length) acts[index]();
		else return false;
		
		index++;
		// testLogger.Log("incremented:" + index);
		return true;
	}
	
	
	public void AssertResult(string expectedJobResult, string actualLatestJobResult, string message) {
		if (expectedJobResult == actualLatestJobResult) ;//testLogger.Log("PASSED:" + message);
		else {
			var error = "FAILED:" + message + " actual:" + actualLatestJobResult;
			testLogger.Log(error);
			throw new Exception(error);
		}
	}
	
	public void AssertFailureResult(string expectedJobFailedResult, string actualLatestJobFailedResult, string message) {
		if (expectedJobFailedResult == actualLatestJobFailedResult) ;//testLogger.Log("PASSED:" + message);
		else {
			var error = "FAILED:" + message + " actual:" + actualLatestJobFailedResult;
			testLogger.Log(error);
			throw new Exception(error);
		}
	}
	
	
	public class TestUpdater {
		public TestUpdater (string loopId, Func<bool> OnUpdate) {
			var mainThreadInterval = 1000f / 10;
			var testLogger = new TestLogger();
			Action loopMethod = () => {
				try {
					double nextFrame = (double)System.Environment.TickCount;
					
					var before = 0.0;
					var tickCount = (double)System.Environment.TickCount;
					
					while (true) {
						tickCount = System.Environment.TickCount * 1.0;
						if (nextFrame - tickCount > 1) {
							Thread.Sleep((int)(nextFrame - tickCount)/2);// not good...
							continue;
						}
						
						if (tickCount >= nextFrame + mainThreadInterval) {
							nextFrame += mainThreadInterval;
							continue;
						}
						
						// run action for update.
						var continuation = OnUpdate();
						if (!continuation) break;
						
						nextFrame += mainThreadInterval;
						before = tickCount; 
					}
					testLogger.Log("loopId:" + loopId + " is finished.");
				} catch (Exception e) {
					testLogger.LogError("loopId:" + loopId + " error:" + e);
				}
			};
			
			var thread = new Thread(new ThreadStart(loopMethod));
			thread.Start();
		}
	}
}