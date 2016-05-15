using System;
using System.Linq;
using DisquuunCore;
using DisquuunCore.Deserialize;

/*
	api sync tests.
*/

public partial class Tests {
	public void _1_0_AddJob_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		var result = disquuun.AddJob(queueId, new byte[10]).Sync();
		var jobId = DisquuunDeserializer.AddJob(result);
		Assert(!string.IsNullOrEmpty(jobId), "empty.");
		
		// ack in.
		disquuun.FastAck(new string[]{jobId}).Sync();
	}
	
	public void _1_1_GetJob_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		disquuun.AddJob(queueId, new byte[10]).Sync();
		
		var result = disquuun.GetJob(new string[]{queueId}).Sync();
		var jobDatas = DisquuunDeserializer.GetJob(result);
		Assert(1, jobDatas.Length, "not match.");
		
		// ack in.
		var jobId = jobDatas[0].jobId;
		disquuun.FastAck(new string[]{jobId}).Sync();
	}
	
	public void _1_1_1_GetJobWithCount_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		var addJobCount = 10000;
		for (var i = 0; i < addJobCount; i++) disquuun.AddJob(queueId, new byte[100]).Sync();
		
		var result = disquuun.GetJob(new string[]{queueId}, "COUNT", addJobCount).Sync();
		var jobDatas = DisquuunDeserializer.GetJob(result);
		Assert(addJobCount, jobDatas.Length, "not match.");
		
		// ack in.
		var jobIds = jobDatas.Select(job => job.jobId).ToArray();
		disquuun.FastAck(jobIds).Sync();
	}
	
	public void _1_1_2_GetJobFromMultiQueue_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId1 = Guid.NewGuid().ToString();
		disquuun.AddJob(queueId1, new byte[10]).Sync();
		
		var queueId2 = Guid.NewGuid().ToString();
		disquuun.AddJob(queueId2, new byte[10]).Sync();
		
		var result = disquuun.GetJob(new string[]{queueId1, queueId2}, "count", 2).Sync();
		var jobDatas = DisquuunDeserializer.GetJob(result);
		Assert(2, jobDatas.Length, "not match.");
		
		// ack in.
		var jobIds = jobDatas.Select(job => job.jobId).ToArray();
		disquuun.FastAck(jobIds).Sync();
	}
	
	public void _1_1_3_GetJobWithNoHang_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		var result = disquuun.GetJob(new string[]{queueId}, "NOHANG").Sync();
		var jobDatas = DisquuunDeserializer.GetJob(result);
		Assert(0, jobDatas.Length, "not match.");
	}
	
	public void _1_2_AckJob_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		var jobId = DisquuunDeserializer.AddJob(
			disquuun.AddJob(queueId, new byte[10]).Sync()
		);
		
		var result = disquuun.AckJob(new string[]{jobId}).Sync();
		var ackCount = DisquuunDeserializer.AckJob(result);
		Assert(1, ackCount, "not match.");
	}
	
	public void _1_3_Fastack_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		var jobId = DisquuunDeserializer.AddJob(
			disquuun.AddJob(queueId, new byte[10]).Sync()
		);
		
		var result = disquuun.FastAck(new string[]{jobId}).Sync();
		var ackCount = DisquuunDeserializer.FastAck(result);
		Assert(1, ackCount, "not match.");
	}
	
	
	// WORKING,// jobid
	// NACK,// <job-id> ... <job-id>
	// INFO,
	// HELLO,
	// QLEN,// <queue-name>
	// QSTAT,// <queue-name>
	// QPEEK,// <queue-name> <count>
	// ENQUEUE,// <job-id> ... <job-id>
	// DEQUEUE,// <job-id> ... <job-id>
	// DELJOB,// <job-id> ... <job-id>
	// SHOW,// <job-id>
	// QSCAN,// [COUNT <count>] [BUSYLOOP] [MINLEN <len>] [MAXLEN <len>] [IMPORTRATE <rate>]
	// JSCAN,// [<cursor>] [COUNT <count>] [BUSYLOOP] [QUEUE <queue>] [STATE <state1> STATE <state2> ... STATE <stateN>] [REPLY all|id]
	// PAUSE,
}