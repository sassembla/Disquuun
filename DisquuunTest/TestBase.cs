using System;
using System.Threading;
using DisquuunCore;
using DisquuunCore.Deserialize;

public class TestBase {
	public bool waiting;
	
	private int index;
	
	public string latestAddedJobId;
	public string latestGotJobId;
	public int latestGotNackCount;
	
	public string latestInfoStr;
	
	public string[] latestWholeGotJobId;
	
	public string latestResult;
	
	public string jobQueueId;

	public Action[] acts;
	
	public Disquuun disquuun;
	public Disquuun disquuun2;
	
	public int failedCount;
	
	public TestBase () {
		disquuun2 = new Disquuun(
			Guid.NewGuid().ToString(),
			"127.0.0.1",
			7711,
			5000,
			(connectionId) => {
				InitializeDisquuunTestTarget();
			},
			(command, byteDatas) => {
				JobProcess(command, byteDatas);
			},
			(failedCommand, reason) => {
				JobFailed(failedCommand, reason);
			}
		);
	}
	
	public void Quit () {
		Killed();
	}
	
	private void InitializeDisquuunTestTarget () {
		var conId = Guid.NewGuid().ToString();
		
		
		disquuun = new Disquuun(
			conId,
			"127.0.0.1",
			7711,
			102400,
			(connectionId) => {
				acts = Setup(connectionId);
				index = 0;
				new TestUpdater("disquuunTestThread_" + connectionId, Run, Killed);
			},
			(command, byteDatas) => {
				JobProcess(command, byteDatas);
			},
			(failedCommand, reason) => {
				// testLogger.Log("failedCommand:" + failedCommand + " reason:" + reason);
				JobFailed(failedCommand, reason);
			}
		);
	}
	
	
	
	public virtual Action[] Setup (string testSuiteId) {
		return null;
	}
	
	public void JobFailed (Disquuun.DisqueCommand command, string reason) {
		latestResult = reason;
		waiting = false;
	}
	
	public void JobProcess (Disquuun.DisqueCommand command, Disquuun.ByteDatas[] byteDatas) {
		// testLogger.Log("// data received:" + command + " byteDatas:" + byteDatas.Length);
		
		switch (command) {
			case Disquuun.DisqueCommand.ADDJOB: {
				var addedJobId = DisquuunDeserializer.AddJob(byteDatas);
				// testLogger.Log("addedJobId:" + addedJobId);
				latestAddedJobId = addedJobId;
				latestResult = "ADDJOB:OK";
				break;
			}
			case Disquuun.DisqueCommand.GETJOB: {
				var jobDatas = DisquuunDeserializer.GetJob(byteDatas);
				foreach (var jobData in jobDatas) {
					var gotJobIdStr = jobData.jobId;
					var gotNackCount = jobData.nackCount;
					// testLogger.Log("gotJobIdStr:" + gotJobIdStr);
					
					latestGotJobId = gotJobIdStr;
					latestGotNackCount = gotNackCount;
				}
				
				latestWholeGotJobId = new string[jobDatas.Length];
				for (var i = 0; i < jobDatas.Length; i++) {
					latestWholeGotJobId[i] = jobDatas[i].jobId;
				}
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
				// TestLogger.Log("infoStr:" + infoStr);
				latestResult = "INFO:";
				latestInfoStr = infoStr;
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
		waiting = false;
	}
	
	private bool Run () {
		if (index < acts.Length) {
			if (!waiting) {
				acts[index]();
				index++;
			}
		} else {
			TestLogger.Log("failedCount:" + failedCount);
			return false;
		}
		// testLogger.Log("incremented:" + index);
		return true;
	}
	
	private void Killed () {
		if (disquuun != null) disquuun.Disconnect();
		if (disquuun != null) disquuun2.Disconnect();
	}
	
	
	public void AssertResult(string expectedJobResult, string actualLatestJobResult, string message) {
		if (expectedJobResult == actualLatestJobResult) TestLogger.Log("PASSED:" + message);
		else {
			var error = "FAILED:" + message + " expected:" + expectedJobResult + " actual:" + actualLatestJobResult;
			TestLogger.Log(error);
			failedCount++;
		}
	}
	
	public class TestUpdater {
		public TestUpdater (string loopId, Func<bool> OnUpdate, Action OnKilled) {
			var mainThreadInterval = 1000f / 60;
			var testLogger = new TestLogger();
			var errorBreak = false;
			Action loopMethod = () => {
				try {
					double nextFrame = (double)System.Environment.TickCount;
					
					var before = 0.0;
					var tickCount = (double)System.Environment.TickCount;
					
					while (true) {
						tickCount = System.Environment.TickCount * 1.0;
						if (nextFrame - tickCount > 1) {
							Thread.Sleep(30);
							continue;
						}
						
						if (tickCount >= nextFrame + mainThreadInterval) {
							nextFrame += mainThreadInterval;
							continue;
						}
						
						// run action for update.
						var continuation = OnUpdate();
						if (!continuation) break;
						if (errorBreak) {
							OnKilled();
							break;
						}
						nextFrame += mainThreadInterval;
						before = tickCount; 
					}
					TestLogger.Log("loopId:" + loopId + " is finished.");
				} catch (Exception e) {
					testLogger.LogError("loopId:" + loopId + " error:" + e);
					errorBreak = true;
				}
			};
			
			var thread = new Thread(new ThreadStart(loopMethod));
			thread.Start();
		}
	}
}