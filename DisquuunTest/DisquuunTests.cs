using System;
using System.IO;
using System.Text;
using DisquuunCore;

public class DisquuunTests {
    private static Disquuun disquuun;
    private static string gotJobId;

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
			(command, bytes0, bytes1) => {
				if (bytes1 != null) TestLogger.Log("data received:" + command + " bytes0:" + bytes0.Length + " bytes1:" + bytes1.Length);
				else TestLogger.Log("data received:" + command + " bytes0:" + bytes0.Length);
				
				switch (command) {
					case Disquuun.DisqueCommand.GETJOB: {
						var jobIdStr = Encoding.UTF8.GetString(bytes0, 0, bytes0.Length);
						// TestLogger.Log("jobIdStr:" + jobIdStr);
						
						gotJobId = jobIdStr;
						break;
					}
					case Disquuun.DisqueCommand.HELLO: {
						var info = Encoding.UTF8.GetString(bytes0, 0, bytes0.Length);
						TestLogger.Log("info:" + info);
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
					disquuun.AckJob(new string[]{gotJobId});
				}
			}
			
			// add -> get -> fastack
			{
				if (counter == 30) {
					disquuun.AddJob("testV", new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
				}
				if (counter == 40) {
					disquuun.GetJob(new string[]{"testV"});
				}
				if (counter == 50) {
					disquuun.FastAck(new string[]{gotJobId});
				}
			}
			
			// if (counter == 40) {
			// 	disqueSharp.GetJob(new string[]{"testQ"});
			// }
			
			// if (counter == 50) {
			// 	disqueSharp.GetJob(new string[]{"testQ"});
			// }
			
			
			if (counter == 160) {// 複数件がいっぺんにくるケース
				disquuun.Info();
				disquuun.Info();
				disquuun.Info();
				disquuun.Info();
				disquuun.Info();
				disquuun.Info();
			}
			
			if (counter == 170) {
				disquuun.Hello();
			}
			
			if (counter == 200) {
				disquuun.Disconnect();
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