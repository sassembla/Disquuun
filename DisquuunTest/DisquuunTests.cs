using System;
using System.Collections.Generic;
using System.IO;

public class DisquuunTests {
	public static List<TestBase> tests;
	
    public static void RunDisquuunTests () {
		tests = new List<TestBase>();
		var testLogger = new TestLogger();
		
		
		tests.Add(new Test1_AllAPIs());
		// new Test2_Fast();
		// tests.Add(new Test3_Size());
	}
	
	
	public static void StopTests () {
		if (tests == null) return; 
		foreach (var test in tests) {
			test.Quit();
		}
	}
}

public class TestLogger {
	private const string logPath = "test.log";
	
	public static void Log (string message) {
		WriteLog(message);
	}
	
	public void LogWarning (string message) {
		WriteLog("WARNING:" + message);
	}
	
	public void LogError (string message) {
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