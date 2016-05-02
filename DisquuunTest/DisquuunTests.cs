using System;
using System.IO;
using System.Text;
using DisquuunCore;
using DisquuunCore.Deserialize;

public class DisquuunTests {
    private static Disquuun disquuun;
	private static Disquuun disquuun2;
	
	
	private static string latestAddedJobId;
    private static string latestGotJobId;

    public static void RunDisquuunTests () {
		var connectionId = Guid.NewGuid().ToString();	
		disquuun = new Disquuun(
			connectionId,
			"127.0.0.1", 
			7711,
			102400,
			connectedConId => {
				RunTests(connectedConId);
			},
			(command, byteDatas) => {
				TestLogger.Log("// data received:" + command + " byteDatas:" + byteDatas.Length);
				
				switch (command) {
					case Disquuun.DisqueCommand.ADDJOB: {
						var addedJobId = DisquuunDeserializer.AddJob(byteDatas);
						TestLogger.Log("addedJobId:" + addedJobId);
						
						latestAddedJobId = addedJobId;
						break;
					}
					case Disquuun.DisqueCommand.GETJOB: {
						var jobDatas = DisquuunDeserializer.GetJob(byteDatas);
						foreach (var jobData in jobDatas) {
							var gotJobIdStr = jobData.jobId;
							TestLogger.Log("gotJobIdStr:" + gotJobIdStr);
							
							latestGotJobId = gotJobIdStr;
						}
						break;
					}
					case Disquuun.DisqueCommand.ACKJOB: {
						var result = DisquuunDeserializer.AckJob(byteDatas);
						TestLogger.Log("ackjob result:" + result);
						break;
					}
					case Disquuun.DisqueCommand.FASTACK: {
						var result = DisquuunDeserializer.FastAck(byteDatas);
						TestLogger.Log("fastack result:" + result);
						break;
					}
					case Disquuun.DisqueCommand.WORKING: {
						var postponeSec = DisquuunDeserializer.Working(byteDatas);
						TestLogger.Log("working postponeSec:" + postponeSec);
						break;
					}
					case Disquuun.DisqueCommand.NACK: {
						var result = DisquuunDeserializer.Nack(byteDatas);
						TestLogger.Log("nack result:" + result);
						break;
					}			
					case Disquuun.DisqueCommand.INFO: {
						var infoStr = DisquuunDeserializer.Info(byteDatas);
						// TestLogger.Log("infoStr:" + infoStr);
						break;
					}
					case Disquuun.DisqueCommand.HELLO: {
						var helloData = DisquuunDeserializer.Hello(byteDatas);
						TestLogger.Log("helloData	vr:" + helloData.version);
						TestLogger.Log("helloData	id:" + helloData.sourceNodeId);
						
						TestLogger.Log("helloData	node Id:" + helloData.nodeDatas[0].nodeId);
						TestLogger.Log("helloData	node ip:" + helloData.nodeDatas[0].ip);
						TestLogger.Log("helloData	node pt:" + helloData.nodeDatas[0].port);
						TestLogger.Log("helloData	node pr:" + helloData.nodeDatas[0].priority);
						break;
					}
					case Disquuun.DisqueCommand.QLEN: {
						var qLengthInt = DisquuunDeserializer.Qlen(byteDatas);
						TestLogger.Log("qLengthInt:" + qLengthInt);
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
			(command, reason) => {
				TestLogger.Log("command failed:" + command + " reason:" + reason);
			},
			e => {
				TestLogger.LogError("e:" + e);
			},
			disconnectedConId => {
				TestLogger.Log("disconnectedConId:" + disconnectedConId);
			}
		);
		
		var connectionId2 = Guid.NewGuid().ToString();
		disquuun2 = new Disquuun(
			connectionId2,
			"127.0.0.1", 
			7711,
			102400
		);
	}
	
	private static void RunTests (string connectedConId) {
		TestLogger.Log("connectedConId:" + connectedConId + " test start.");
						
		int counter = 0;			
		
		// 1fごとに実行されるキューにしてみるか。問題ないはず。
		// 本来は別々のclientにしたほうがいい。
		Func<bool> UpdateSending = () => {
			// add -> get -> ack
			{
				if (counter == 0) {
					disquuun.AddJob("testQ", new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
				}
				if (counter == 1) {
					disquuun.GetJob(new string[]{"testQ"});
				}
				if (counter == 2) {
					disquuun.AckJob(new string[]{latestGotJobId});
				}
			}
			
			// add -> get -> fastack
			{
				if (counter == 10) {
					disquuun.AddJob("testQ", new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
				}
				if (counter == 11) {
					disquuun.GetJob(new string[]{"testQ"});
				}
				if (counter == 12) {
					disquuun.FastAck(new string[]{latestGotJobId});
				}
			}
			
			// empty queue, will waiting data.
			{
				if (counter == 20) {
					// start waiting,
					disquuun.GetJob(new string[]{"testV"});
				}
				if (counter == 21) {
					// add data to queue, then receive data from disquuun2.
					disquuun2.AddJob("testV", new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
				}
				if (counter == 22) {
					disquuun.FastAck(new string[]{latestGotJobId});
				}
			}
			
			// non exist queue. never back until created.
			{
				if (counter == 30) {
					disquuun.GetJob(new string[]{"testR"});
				}
				if (counter == 31) {
					disquuun2.AddJob("testR", new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
				}
				if (counter == 32) {
					disquuun.FastAck(new string[]{latestGotJobId});
				}
			}
			
			
			// blocking with empty queue.
			{
				if (counter == 40) {
					disquuun.GetJob(new string[]{"testS"});
				}
				if (counter == 41) {
					// blocked by getting job from empty queue. until add job to it.
					disquuun.Info();
				}
				if (counter == 42) {
					disquuun2.AddJob("testS", new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
				}
				if (counter == 43) {
					disquuun.FastAck(new string[]{latestGotJobId});
				}
			}
			
			// non blocking with empty queue.
			// {
			// 	if (counter == 50) {
			// 		disquuun.GetJob(new string[]{"testS"}, "NOHANG");
			// 	}
			// 	if (counter == 51) {
			// 		// non blocked by getting job from empty queue. until add job to it.
			// 		disquuun.Info();
			// 	}
			// }
		
			if (counter == 60) {
				disquuun.Info();
				disquuun.Info();
				disquuun.Info();
				disquuun.Info();
				disquuun.Info();
				disquuun.Info();
			}
			
			if (counter == 70) {
				disquuun.Info();
			}
			
			if (counter == 80) {
				disquuun.Hello();
			}
			
			// qlen returns 0.
			if (counter == 90) {
				disquuun.Qlen("testS");
			}
			
			// qlen returns 1.
			{
				if (counter == 91) {
					disquuun.AddJob("testSt", new byte[]{1});
				} 
				if (counter == 92) {
					disquuun.Qlen("testSt");
				}
				if (counter == 93) {
					disquuun.GetJob(new string[]{"testSt"});
				}
				if (counter == 94) {
					disquuun.FastAck(new string[]{latestGotJobId});
				}
			}
			
			// working return error.
			if (counter == 100) {
				disquuun.Working("dummyJobId");
			}
			
			// working return "seconds".
			{
				if (counter == 101) {// そろそろ書きやすくしないと詰むな、、stackとか？ハンドラ？ハンドラかな、、うーん、、
					disquuun.AddJob("testT", new byte[]{1,3,5,7,9});
				}
				if (counter == 102) {
					disquuun.Working(latestAddedJobId);
					disquuun.GetJob(new string[]{"testT"});
				}
				if (counter == 103) {
					disquuun.FastAck(new string[]{latestGotJobId});
				}
			}
			
			// nack return error.
			if (counter == 110) {
				disquuun.Nack(new string[]{"dummyJobId"});
			}
			
			// nack succeeded with status.
			{// まだうまくいってない気がする。
				if (counter == 111) {
					disquuun.AddJob("testNack", new byte[]{1,3,5,7,9});
				}
				if (counter == 112) {
					disquuun.Nack(new string[]{latestAddedJobId});
					disquuun.GetJob(new string[]{"testNack"});
				}
				if (counter == 113) {
					disquuun.FastAck(new string[]{latestGotJobId});
				}
			}
			
			
			
			if (counter == 500) {
				disquuun.Disconnect();
				disquuun2.Disconnect();
				return false;
			}
			
			counter++;
			return true;
		};
		
		new Updater("disquuunTestThread", UpdateSending);
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