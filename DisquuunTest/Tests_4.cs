using System;
using System.Linq;
using DisquuunCore;
using DisquuunCore.Deserialize;

/*
	buffer over tests.
*/

public partial class Tests {
	public void _4_0_ByfferOverWithSingleSyncGetJob_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		disquuun.AddJob(queueId, new byte[disquuun.BufferSize]).Sync();
		
		var result = disquuun.GetJob(new string[]{queueId}).Sync();
		var jobDatas = DisquuunDeserializer.GetJob(result);
		Assert(1, jobDatas.Length, "not match.");
		
		// ack in.
		var jobIds = jobDatas.Select(job => job.jobId).ToArray();
		disquuun.FastAck(jobIds).Sync();
	}
	
	public void _4_1_ByfferOverWithMultipleSyncGetJob_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		var addJobCount = 2;
		for (var i = 0; i < addJobCount; i++) disquuun.AddJob(queueId, new byte[disquuun.BufferSize/addJobCount]).Sync();
		
		var result = disquuun.GetJob(new string[]{queueId}, "COUNT", addJobCount).Sync();
		var jobDatas = DisquuunDeserializer.GetJob(result);
		Assert(addJobCount, jobDatas.Length, "not match.");
		
		// ack in.
		var jobIds = jobDatas.Select(job => job.jobId).ToArray();
		disquuun.FastAck(jobIds).Sync();
	}
	
	public void _4_2_ByfferOverWithSokcetOverSyncGetJob_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		var addJobCount = 10001;
		for (var i = 0; i < addJobCount; i++) disquuun.AddJob(queueId, new byte[100]).Sync();
		
		var result = disquuun.GetJob(new string[]{queueId}, "COUNT", addJobCount).Sync();
		var jobDatas = DisquuunDeserializer.GetJob(result);
		Assert(addJobCount, jobDatas.Length, "not match.");
		
		// ack in.
		var jobIds = jobDatas.Select(job => job.jobId).ToArray();
		disquuun.FastAck(jobIds).Sync();
	}
	
	public void _4_3_ByfferOverWithSingleSyncGetJob_Async (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		disquuun.AddJob(queueId, new byte[disquuun.BufferSize]).Sync();
		
		DisquuunDeserializer.JobData[] jobDatas = new DisquuunDeserializer.JobData[]{};
		disquuun.GetJob(new string[]{queueId}).Async(
			(command, result) => {
				jobDatas = DisquuunDeserializer.GetJob(result);
			}
		);
		
		WaitUntil(() => (jobDatas.Length == 1), 5);
		
		// ack in.
		var jobIds = jobDatas.Select(job => job.jobId).ToArray();
		disquuun.FastAck(jobIds).Sync();
	}
	
	public void _4_4_ByfferOverWithMultipleSyncGetJob_Async (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		var addJobCount = 2;
		for (var i = 0; i < addJobCount; i++) disquuun.AddJob(queueId, new byte[disquuun.BufferSize/addJobCount]).Sync();
		
		DisquuunDeserializer.JobData[] jobDatas = new DisquuunDeserializer.JobData[]{};
		disquuun.GetJob(new string[]{queueId}, "COUNT", addJobCount).Async(
			(comand, result) => {
				jobDatas = DisquuunDeserializer.GetJob(result);
			}
		);
		
		WaitUntil(() => (jobDatas.Length == addJobCount), 5);
		
		// ack in.
		var jobIds = jobDatas.Select(job => job.jobId).ToArray();
		disquuun.FastAck(jobIds).Sync();
	}
	
	public void _4_5_ByfferOverWithSokcetOverSyncGetJob_Async (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		var addJobCount = 10001;
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
}