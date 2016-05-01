using System;
using System.IO;
using System.Text;
using DisquuunCore;
using DisquuunCore.Deserialize;

public class DisquuunTests {
    private static Disquuun disquuun;
	private static Disquuun disquuun2;
	
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
				TestLogger.Log("data received:" + command + " byteDatas:" + byteDatas.Length);
				
				switch (command) {
					case Disquuun.DisqueCommand.ADDJOB: {
						var addedJobId = DisquuunDeserializer.AddJob(byteDatas);
						TestLogger.Log("addedJobId:" + addedJobId);
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
					case Disquuun.DisqueCommand.INFO: {
						var infoStr = DisquuunDeserializer.Info(byteDatas);
						TestLogger.Log("infoStr:" + infoStr);
						break;
					}
					case Disquuun.DisqueCommand.HELLO: {
						try {
						var helloData = DisquuunDeserializer.Hello(byteDatas);
						TestLogger.Log("helloData	vr:" + helloData.version);
						TestLogger.Log("helloData	id:" + helloData.sourceNodeId);
						
						TestLogger.Log("helloData	node Id:" + helloData.nodeDatas[0].nodeId);
						TestLogger.Log("helloData	node ip:" + helloData.nodeDatas[0].ip);
						TestLogger.Log("helloData	node pt:" + helloData.nodeDatas[0].port);
						TestLogger.Log("helloData	node pr:" + helloData.nodeDatas[0].priority);
						} catch (Exception e) {
							TestLogger.Log("e:" + e);
						}
						break;
					}
					default: {
						// ignored
						break;
					}
				} 
			},
			(command, bytes) => {
				TestLogger.Log("data failed:" + command + " bytes:" + bytes.Length);
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
		
		Func<bool> UpdateSending = () => {
			// add -> get -> ack
			{
				if (counter == 0) {
					disquuun.AddJob("testQ", new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
				}
				if (counter == 10) {
					disquuun.GetJob(new string[]{"testQ"});
				}
				if (counter == 20) {
					disquuun.AckJob(new string[]{latestGotJobId});
				}
			}
			
			// add -> get -> fastack
			{
				if (counter == 30) {
					disquuun.AddJob("testQ", new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
				}
				if (counter == 40) {
					disquuun.GetJob(new string[]{"testQ"});
				}
				if (counter == 50) {
					disquuun.FastAck(new string[]{latestGotJobId});
				}
			}
			
			// empty queue, will waiting data.
			{
				if (counter == 60) {
					// start waiting,
					disquuun.GetJob(new string[]{"testV"});
				}
				if (counter == 70) {
					// add data to queue, then receive data from disquuun2.
					disquuun2.AddJob("testV", new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
				}
				if (counter == 80) {
					disquuun.FastAck(new string[]{latestGotJobId});
				}
			}
			
			// non exist queue. never back until created.
			{
				if (counter == 80) {
					disquuun.GetJob(new string[]{"testR"});
				}
				if (counter == 90) {
					disquuun2.AddJob("testR", new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
				}
				if (counter == 100) {
					disquuun.FastAck(new string[]{latestGotJobId});
				}
			}
			
			
			// blocking with empty queue.
			{
				if (counter == 120) {
					disquuun.GetJob(new string[]{"testS"});
				}
				if (counter == 130) {
					// blocked by getting job from empty queue. until add job to it.
					disquuun.Hello();
				}
				if (counter == 140) {
					disquuun2.AddJob("testS", new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
				}
				if (counter == 150) {
					disquuun.FastAck(new string[]{latestGotJobId});
				}
			}
			
			
			// if (counter == 200) {
			// 	disquuun.Info();
			// 	disquuun.Info();
			// 	disquuun.Info();
			// 	disquuun.Info();
			// 	disquuun.Info();
			// 	disquuun.Info();
			// }
			
			if (counter == 210) {
				disquuun.Info();
			}
			
			if (counter == 220) {
				disquuun.Hello();
			}
			
			if (counter == 230) {
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