using System;
using System.Linq;
using DisquuunCore;
using DisquuunCore.Deserialize;

/*
	api async tests.
*/

public partial class Tests {
	public void _2_0_AddJob_Async (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		var jobId = string.Empty;
		disquuun.AddJob(queueId, new byte[10]).Async(
			(command, result) => {
				jobId = DisquuunDeserializer.AddJob(result);
			}
		);
		
		WaitUntil(() => !string.IsNullOrEmpty(jobId), 5);
		
		// ack in.
		disquuun.FastAck(new string[]{jobId}).Sync();
	}
	
	public void _2_1_GetJob_Async (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		disquuun.AddJob(queueId, new byte[10]).Sync();
		
		DisquuunDeserializer.JobData[] jobDatas = new DisquuunDeserializer.JobData[]{};
		
		disquuun.GetJob(new string[]{queueId}).Async(
			(command, result) => {
				jobDatas = DisquuunDeserializer.GetJob(result);
			}
		);
		
		WaitUntil(() => (jobDatas.Length == 1), 5);
		
		// ack in.
		var jobId = jobDatas[0].jobId;
		disquuun.FastAck(new string[]{jobId}).Sync();
	}
	
	public void _2_1_1_GetJobWithCount_Async (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		var addJobCount = 10000;
		for (var i = 0; i < addJobCount; i++) disquuun.AddJob(queueId, new byte[100]).Sync();
		
		DisquuunDeserializer.JobData[] jobDatas = new DisquuunDeserializer.JobData[]{};
		disquuun.GetJob(new string[]{queueId}, "COUNT", addJobCount).Async(
			(command, result) => {
				jobDatas = DisquuunDeserializer.GetJob(result);
			}
		);
		
		WaitUntil(() => (jobDatas.Length == addJobCount), 5);
		
		// ack in.
		var jobIds = jobDatas.Select(job => job.jobId).ToArray();
		disquuun.FastAck(jobIds).Sync();
	}
	
	public void _2_1_2_GetJobFromMultiQueue_Async (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId1 = Guid.NewGuid().ToString();
		disquuun.AddJob(queueId1, new byte[10]).Sync();
		
		var queueId2 = Guid.NewGuid().ToString();
		disquuun.AddJob(queueId2, new byte[10]).Sync();
		
		DisquuunDeserializer.JobData[] jobDatas = new DisquuunDeserializer.JobData[]{};
		disquuun.GetJob(new string[]{queueId1, queueId2}, "count", 2).Async(
			(command, result) => {
				jobDatas = DisquuunDeserializer.GetJob(result);
			}
		);
		
		WaitUntil(() => (jobDatas.Length == 2), 5);
		
		// ack in.
		var jobIds = jobDatas.Select(job => job.jobId).ToArray();
		disquuun.FastAck(jobIds).Sync();
	}
	
	public void _2_1_3_GetJobWithNoHang_Async (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		bool received = false;
		disquuun.GetJob(new string[]{queueId}, "NOHANG").Async(
			(command, result) => {
				var jobDatas = DisquuunDeserializer.GetJob(result);
				Assert(jobDatas.Length == 0, "not match.");
				received = true;
			}
		);
		
		WaitUntil(() => received, 5);
	}
	
	public void _2_2_AckJob_Async (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		var jobId = DisquuunDeserializer.AddJob(
			disquuun.AddJob(queueId, new byte[10]).Sync()
		);
		
		var ackCount = 0;
		disquuun.AckJob(new string[]{jobId}).Async(
			(command, result) => {
				ackCount = DisquuunDeserializer.AckJob(result);
			}
		);
		
		WaitUntil(() => (ackCount == 1), 5);
	}
	
	public void _2_3_Fastack_Async (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		var jobId = DisquuunDeserializer.AddJob(
			disquuun.AddJob(queueId, new byte[10]).Sync()
		);
		
		var ackCount = 0;
		disquuun.FastAck(new string[]{jobId}).Async(
			(command, result) => {
				ackCount = DisquuunDeserializer.FastAck(result);
			}
		);
		
		WaitUntil(() => (ackCount == 1), 5);
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