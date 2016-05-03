using System;
using System.IO;

public class DisquuunTests {

    public static void RunDisquuunTests () {
		var testLogger = new TestLogger();
		try {
			// run parallel.
			// new Test1_AllAPIs();
			new Test2_Fast();
			
			// new Test2_Fast();
			// new Test2_Fast();
			
		} catch (Exception e) {
			testLogger.Log("e:" + e);	
		}
	}
}

public class TestLogger {
	private const string logPath = "test.log";
	
	public void Log (string message) {
		WriteLog(message);
	}
	
	public void LogWarning (string message) {
		WriteLog("WARNING:" + message);
	}
	
	public void LogError (string message) {
		WriteLog("ERROR:" + message);
		WriteLog("stacktrace:" + Environment.StackTrace);
	}
	
	public void WriteLog (string message) {

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