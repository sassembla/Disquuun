using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using DisquuunCore;
using DisquuunCore.Deserialize;

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
		
		// basement.
		tests.Add(_0_0_InitWith2Connection);
		tests.Add(_0_1_ConnectionFailedWithNoDisqueServer);
		tests.Add(_0_2_SyncInfo);
		tests.Add(_0_3_SyncInfoTwice);
		tests.Add(_0_4_AsyncInfo);
		tests.Add(_0_5_LoopInfo_Once);
		tests.Add(_0_6_LoopInfo_Twice);
		tests.Add(_0_7_LoopInfo_100);
		
		// apis.
		tests.Add(_1_0_AddJob);
		tests.Add(_1_1_GetJob);
		tests.Add(_1_1_1_GetJobWithCount);
		tests.Add(_1_1_2_GetJobFromMultiQueue);
		tests.Add(_1_1_3_GetJobWithNoHang);
		tests.Add(_1_2_AckJob);
		tests.Add(_1_3_Fastack);
		
		// multiSocket.
		tests.Add(_2_0_2SyncSocket);
		tests.Add(_2_1_MultipleSyncSocket);
		
		
		TestLogger.Log("tests started.");
		
		
		foreach (var test in tests) {
			try {
				var disquuun = new Disquuun("127.0.0.1", 7711, 10240, 2);
				test(disquuun);
				if (disquuun != null) {
					disquuun.Disconnect(true);
					disquuun = null;
				}
			} catch (Exception e) {
				TestLogger.Log("test:" + test + " FAILED by exception:" + e);
			}
		}
		
		var disquuun2 = new Disquuun("127.0.0.1", 7711, 10240, 1);
		WaitUntil(() => (disquuun2.State() == Disquuun.ConnectionState.OPENED), 5);
		var result = DisquuunDeserializer.Info(disquuun2.Info().Sync());
		disquuun2.Disconnect(true);
		
		TestLogger.Log("all tests over. rest unconsumed job:" + result.jobs.registered_jobs);
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
	
	public void Assert (bool condition, string message) {
		System.Diagnostics.StackTrace stack  = new System.Diagnostics.StackTrace(false);
		var methodName = stack.GetFrame(1).GetMethod().Name;
		if (!condition) TestLogger.Log("test:" + methodName + " FAILED:" + message); 
	}
	
	public void Assert (object expected, object actual, string message) {
		System.Diagnostics.StackTrace stack  = new System.Diagnostics.StackTrace(false);
		var methodName = stack.GetFrame(1).GetMethod().Name;
		if (expected.ToString() != actual.ToString()) TestLogger.Log("test:" + methodName + " FAILED:" + message + " expected:" + expected + " actual:" + actual); 
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