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
		// tests.Add(_0_0_InitWith2Connection);
		// tests.Add(_0_0_1_WaitOnOpen2Connection);
		// tests.Add(_0_0_2_ReadmeSampleSync);
		tests.Add(_0_0_3_ReadmeSampleAsync);
		// tests.Add(_0_1_ConnectionFailedWithNoDisqueServer);
		// tests.Add(_0_2_SyncInfo);
		// tests.Add(_0_3_SyncInfoTwice);
		// tests.Add(_0_4_AsyncInfo);
		// tests.Add(_0_5_LoopInfo_Once);
		// tests.Add(_0_6_LoopInfo_Twice);
		// tests.Add(_0_7_LoopInfo_100);
		
		// // sync apis. DEPRECATED.
		// tests.Add(_1_0_AddJob_Sync);
		// tests.Add(_1_1_GetJob_Sync);
		// tests.Add(_1_1_1_GetJobWithCount_Sync);
		// tests.Add(_1_1_2_GetJobFromMultiQueue_Sync);
		// tests.Add(_1_1_3_GetJobWithNoHang_Sync);
		// tests.Add(_1_2_AckJob_Sync);
		// tests.Add(_1_3_Fastack_Sync);
		// // tests.Add(_1_4_Working_Sync);
		// // tests.Add(_1_5_Nack_Sync);
		// tests.Add(_1_6_Info_Sync);
		// tests.Add(_1_7_Hello_Sync);
		// tests.Add(_1_8_Qlen_Sync);
		// // tests.Add(_1_9_Qstat_Sync);
		// // tests.Add(_1_10_Qpeek_Sync);
		// // tests.Add(_1_11_Enqueue_Sync);
		// // tests.Add(_1_12_Dequeue_Sync);
		// // tests.Add(_1_13_DelJob_Sync);
		// // tests.Add(_1_14_Show_Sync);
		// // tests.Add(_1_15_Qscan_Sync);
		// // tests.Add(_1_16_Jscan_Sync);
		// // tests.Add(_1_17_Pause_Sync);
		
		// // async apis.
		// tests.Add(_2_0_AddJob_Async);
		// tests.Add(_2_1_GetJob_Async);
		// tests.Add(_2_1_1_GetJobWithCount_Async);
		// tests.Add(_2_1_2_GetJobFromMultiQueue_Async);
		// tests.Add(_2_1_3_GetJobWithNoHang_Async);
		// tests.Add(_2_2_AckJob_Async);
		// tests.Add(_2_3_Fastack_Async);
		// // tests.Add(_2_4_Working_Async);
		// // tests.Add(_2_5_Nack_Async);
		// tests.Add(_2_6_Info_Async);
		// tests.Add(_2_7_Hello_Async);
		// tests.Add(_2_8_Qlen_Async);
		// // tests.Add(_2_9_Qstat_Async);
		// // tests.Add(_2_10_Qpeek_Async);
		// // tests.Add(_2_11_Enqueue_Async);
		// // tests.Add(_2_12_Dequeue_Async);
		// // tests.Add(_2_13_DelJob_Async);
		// // tests.Add(_2_14_Show_Async);
		// // tests.Add(_2_15_Qscan_Async);
		// // tests.Add(_2_16_Jscan_Async);
		// // tests.Add(_2_17_Pause_Async);
		
		// // multiSocket.
		// tests.Add(_3_0_Nested2AsyncSocket);
		// tests.Add(_3_1_NestedMultipleAsyncSocket);
		
		// // buffer over.
		// tests.Add(_4_0_ByfferOverWithSingleSyncGetJob_Sync);
		// tests.Add(_4_1_ByfferOverWithMultipleSyncGetJob_Sync);
		// tests.Add(_4_2_ByfferOverWithSokcetOverSyncGetJob_Sync);
		// tests.Add(_4_3_ByfferOverWithSingleSyncGetJob_Async);
		// tests.Add(_4_4_ByfferOverWithMultipleSyncGetJob_Async);
		// tests.Add(_4_5_ByfferOverWithSokcetOverSyncGetJob_Async);
		
		// // error handling.
		// // tests.Add(_5_0_Error)// connect時に出るエラー、接続できないとかその辺のハンドリング
		
		// // adding async request over busy-socket num.
		// tests.Add(_6_0_ExceededSocketNo3In2);
		// tests.Add(_6_1_ExceededSocketNo100In2);
		
		
		TestLogger.Log("tests started.");
		
		foreach (var test in tests) {
			try {
				var disquuun = new Disquuun("127.0.0.1", 7711, 2020008, 2);// this buffer size is just for 100byte job x 10000 then receive 1 GetJob(count 1000).
				test(disquuun);
				if (disquuun != null) {
					disquuun.Disconnect(true);
					disquuun = null;
				}
			} catch (Exception e) {
				TestLogger.Log("test:" + test + " FAILED by exception:" + e);
			}
		}
		
		var restJobCount = -1;
		
		var disquuun2 = new Disquuun("127.0.0.1", 7711, 10240, 1);
		WaitUntil(() => (disquuun2.State() == Disquuun.ConnectionState.OPENED), 5);
		disquuun2.Info().Async(
			(command, data) => {
				var result = DisquuunDeserializer.Info(data);
				
				restJobCount = result.jobs.registered_jobs;
				
				TestLogger.Log("all tests over. rest unconsumed job:" + restJobCount + " connected_clients:" + result.clients.connected_clients);
			}
		);
		
		WaitUntil(() => (restJobCount != -1), 5);
		
		disquuun2.Disconnect(true);
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