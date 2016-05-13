using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using DisquuunCore;

public class DisquuunTests {
	public static Tests tests;
	
	public static void Start () {
		tests = new Tests();
		tests.RunTests();	
	}
	
	public static void Stop () {
		tests = null;
	}
}


public partial class Tests {
	public void RunTests () {
		
		var tests = new List<Action<Disquuun>>();
		tests.Add(_0_0_InitWith2Connection);
		tests.Add(_0_1_ConnectionFailedWithNoDisqueServer);
		tests.Add(_0_2_SyncInfo);
		tests.Add(_0_3_SyncInfoTwice);
		tests.Add(_0_4_AsyncInfo);
		tests.Add(_0_5_LoopInfo_Once);
		tests.Add(_0_6_LoopInfo_Twice);
		tests.Add(_0_7_LoopInfo_100);
		
		
		TestLogger.Log("tests started.");
		
		
		foreach (var test in tests) {
			var disquuun = new Disquuun("127.0.0.1", 7711, 10240, 2);
			test(disquuun);
			if (disquuun != null) {
				disquuun.Disconnect(true);
				disquuun = null;
			}
		}
		
		TestLogger.Log("all tests over.");
	}
	
	
	public void WaitUntil (Func<bool> WaitFor, int timeoutSec) {
		System.Diagnostics.StackTrace stack  = new System.Diagnostics.StackTrace(false);
		var methodName = stack.GetFrame(1).GetMethod().Name;
		var resetEvent = new ManualResetEvent(false);
		
		var waitingThread = new Thread(
			() => {
				resetEvent.Reset();
				var startTime = DateTime.Now;
				
				try {
					while (!WaitFor()) {
						var current = DateTime.Now;
						var distanceSeconds = (current - startTime).Seconds;
						
						if (timeoutSec < distanceSeconds) {
							TestLogger.Log("timeout:" + methodName);
							break;
						}
						
						System.Threading.Thread.Sleep(10);
					}
				} catch (Exception e) {
					TestLogger.Log("methodName:" + methodName + " error:" + e);
				}
				
				resetEvent.Set();
			}
		);
		
		waitingThread.Start();
		
		resetEvent.WaitOne();
	}
}




public static class TestLogger {
	public static string logPath;
	
	public static void Log (string message) {
		logPath = "test.log";
		
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