using System;
using System.Linq;
using DisquuunCore;
using DisquuunCore.Deserialize;

/*
	multiple execution.
*/

public partial class Tests {
	public void _3_0_2SyncSocket (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId1 = Guid.NewGuid().ToString();
		var result1 = disquuun.AddJob(queueId1, new byte[10]).Sync();
		var jobId1 = DisquuunDeserializer.AddJob(result1);
		
		var queueId2 = Guid.NewGuid().ToString();
		var result2 = disquuun.AddJob(queueId2, new byte[10]).Sync();
		var jobId2 = DisquuunDeserializer.AddJob(result2);
		
		var gets = DisquuunDeserializer.GetJob(disquuun.GetJob(new string[]{queueId1, queueId2}, "count", 2).Sync());
		disquuun.FastAck(gets.Select(job => job.jobId).ToArray()).Sync();
	}
	
	public void _3_1_MultipleSyncSocket (Disquuun disquuun) {
		// WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		// var queueId1 = Guid.NewGuid().ToString();
		// var result1 = disquuun.AddJob(queueId1, new byte[10]).Sync();
		// var jobId1 = DisquuunDeserializer.AddJob(result1);
		
		// for () {		
		// 	var queueId = Guid.NewGuid().ToString();
		// 	var result = disquuun.AddJob(queueId, new byte[10]).Sync();
		// 	var jobId = DisquuunDeserializer.AddJob(result);
		// 	Assert(!string.IsNullOrEmpty(jobId), "empty.");
		// }
	}
	
	// 2つのAsync
	// 沢山のAsync
}