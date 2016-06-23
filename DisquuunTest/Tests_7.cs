using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DisquuunCore;
using DisquuunCore.Deserialize;

/*
	benchmark
*/

public partial class Tests {
	public void _7_0_AddJob1000 (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var count = 1000;
		
		var connectedCount = 0;
		disquuun = new Disquuun(DisquuunTests.TestDisqueHostStr, DisquuunTests.TestDisquePortNum, 1024, 10,
			disquuunId => {
				connectedCount++;
			}
		);
		
		
		WaitUntil(() => (connectedCount == 1), 5);
		
		var addedCount = 0;
		
		var queueId = Guid.NewGuid().ToString();
		
		var w = new Stopwatch();
		w.Start();
		for (var i = 0; i < count; i++) {
			disquuun.AddJob(queueId, new byte[10]).Async(
				(command, data) => {
					lock (this) addedCount++;
				}
			);
		}
		w.Stop();
		TestLogger.Log("_7_0_AddJob1000000 w:" + w.ElapsedMilliseconds + " tick:" + w.ElapsedTicks);
		
		WaitUntil(() => (addedCount == count), 100);
		
		var gotCount = 0;
		disquuun.GetJob(new string[]{queueId}, "count", count).Async(
			(command, data) => {
				var jobDatas = DisquuunDeserializer.GetJob(data);
				var jobIds = jobDatas.Select(j => j.jobId).ToArray();
				disquuun.FastAck(jobIds).Async(
					(c2, d2) => {
						var fastackCount = DisquuunDeserializer.FastAck(d2);
						lock (this) {
							gotCount += fastackCount;
						}
					}
				);
			}
		);
		
		WaitUntil(() => (gotCount == count), 10);
		disquuun.Disconnect(true);
	}
	
	public void _7_1_GetJob1000 (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var count = 1000;
		
		var connectedCount = 0;
		disquuun = new Disquuun(DisquuunTests.TestDisqueHostStr, DisquuunTests.TestDisquePortNum, 1024, 10,
			disquuunId => {
				connectedCount++;
			}
		);
		
		WaitUntil(() => (connectedCount == 1), 5);
		
		var addedCount = 0;
		
		var queueId = Guid.NewGuid().ToString();
		
		for (var i = 0; i < count; i++) {
			disquuun.AddJob(queueId, new byte[10]).Async(
				(command, data) => {
					lock (this) addedCount++;
				}
			);
		}
		
		WaitUntil(() => (addedCount == count), 10);
		
		var gotJobData = new List<DisquuunResult[]>();
		
		
		var w = new Stopwatch();
		w.Start();
		for (var i = 0; i < count; i++) {
			disquuun.GetJob(new string[]{queueId}).Async(
				(command, data) => {
					lock (this) {
						gotJobData.Add(data);
					}
				}
			);
		}
		
		WaitUntil(() => (gotJobData.Count == count), 10);
		
		w.Stop();
		TestLogger.Log("_7_1_GetJob1000000 w:" + w.ElapsedMilliseconds + " tick:" + w.ElapsedTicks);
		
		
		var allGotJobs = gotJobData.Select(j => DisquuunDeserializer.GetJob(j)).SelectMany(j => j).ToArray();
		var allGotJobIds = allGotJobs.Select(j => j.jobId).ToArray();
		
		var result = DisquuunDeserializer.FastAck(disquuun.FastAck(allGotJobIds).DEPRICATED_Sync());
		
		Assert(count, result, "not match.");
		disquuun.Disconnect(true);
	}
	
}