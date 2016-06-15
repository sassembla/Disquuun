using System;
using System.Linq;
using DisquuunCore;
using DisquuunCore.Deserialize;

/*
	api sync tests.
*/

public partial class Tests {
	// all sync apis are deprecated.
	
	public void _1_0_AddJob_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		var result = disquuun.AddJob(queueId, new byte[10]).DEPRICATED_Sync();
		var jobId = DisquuunDeserializer.AddJob(result);
		Assert(!string.IsNullOrEmpty(jobId), "empty.");
		
		// ack in.
		disquuun.FastAck(new string[]{jobId}).DEPRICATED_Sync();
	}
	
	public void _1_1_GetJob_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		disquuun.AddJob(queueId, new byte[10]).DEPRICATED_Sync();
		
		var result = disquuun.GetJob(new string[]{queueId}).DEPRICATED_Sync();
		var jobDatas = DisquuunDeserializer.GetJob(result);
		Assert(1, jobDatas.Length, "not match.");
		
		// ack in.
		var jobId = jobDatas[0].jobId;
		disquuun.FastAck(new string[]{jobId}).DEPRICATED_Sync();
	}
	
	public void _1_1_1_GetJobWithCount_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		var addJobCount = 10000;
		for (var i = 0; i < addJobCount; i++) disquuun.AddJob(queueId, new byte[100]).DEPRICATED_Sync();
		
		var result = disquuun.GetJob(new string[]{queueId}, "COUNT", addJobCount).DEPRICATED_Sync();
		var jobDatas = DisquuunDeserializer.GetJob(result);
		Assert(addJobCount, jobDatas.Length, "not match.");
		
		// ack in.
		var jobIds = jobDatas.Select(job => job.jobId).ToArray();
		disquuun.FastAck(jobIds).DEPRICATED_Sync();
	}
	
	public void _1_1_2_GetJobFromMultiQueue_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId1 = Guid.NewGuid().ToString();
		disquuun.AddJob(queueId1, new byte[10]).DEPRICATED_Sync();
		
		var queueId2 = Guid.NewGuid().ToString();
		disquuun.AddJob(queueId2, new byte[10]).DEPRICATED_Sync();
		
		var result = disquuun.GetJob(new string[]{queueId1, queueId2}, "count", 2).DEPRICATED_Sync();
		var jobDatas = DisquuunDeserializer.GetJob(result);
		Assert(2, jobDatas.Length, "not match.");
		
		// ack in.
		var jobIds = jobDatas.Select(job => job.jobId).ToArray();
		disquuun.FastAck(jobIds).DEPRICATED_Sync();
	}
	
	public void _1_1_3_GetJobWithNoHang_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		var result = disquuun.GetJob(new string[]{queueId}, "NOHANG").DEPRICATED_Sync();
		var jobDatas = DisquuunDeserializer.GetJob(result);
		Assert(0, jobDatas.Length, "not match.");
	}
	
	public void _1_1_4_GetJobWithCounters_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		disquuun.AddJob(queueId, new byte[100]).DEPRICATED_Sync();
		
		var result = disquuun.GetJob(new string[]{queueId}, "withcounters").DEPRICATED_Sync();
		var jobDatas = DisquuunDeserializer.GetJob(result);
		var ackCount = jobDatas[0].additionalDeliveriesCount;
		Assert(0, ackCount, "not match.");
		
		disquuun.FastAck(new string[]{jobDatas[0].jobId}).DEPRICATED_Sync();
	}
	
	public void _1_2_AckJob_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		var jobId = DisquuunDeserializer.AddJob(
			disquuun.AddJob(queueId, new byte[10]).DEPRICATED_Sync()
		);
		
		var result = disquuun.AckJob(new string[]{jobId}).DEPRICATED_Sync();
		var ackCount = DisquuunDeserializer.AckJob(result);
		Assert(1, ackCount, "not match.");
	}
	
	public void _1_3_Fastack_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		var jobId = DisquuunDeserializer.AddJob(
			disquuun.AddJob(queueId, new byte[10]).DEPRICATED_Sync()
		);
		
		var result = disquuun.FastAck(new string[]{jobId}).DEPRICATED_Sync();
		var ackCount = DisquuunDeserializer.FastAck(result);
		Assert(1, ackCount, "not match.");
	}
	
	public void _1_4_Working_Sync (Disquuun disquuun) {
		Disquuun.Log("_1_4_Working_Sync not yet applied");
		
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var queueId = Guid.NewGuid().ToString();
		// var jobId = DisquuunDeserializer.AddJob(
		// 	disquuun.AddJob(queueId, new byte[10]).DEPRICATED_Sync()
		// );
		
		// var workResult = disquuun.Working(jobId).DEPRICATED_Sync();
		// var workingResult = DisquuunDeserializer.Working(workResult);
		
		// var result = disquuun.FastAck(new string[]{jobId}).DEPRICATED_Sync();
		// var ackCount = DisquuunDeserializer.FastAck(result);
		// Assert(1, ackCount, "not match.");
	}
	
	public void _1_5_Nack_Sync (Disquuun disquuun) {
		Disquuun.Log("_1_5_Nack_Sync not yet applied");
		
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var queueId = Guid.NewGuid().ToString();
		// var jobId = DisquuunDeserializer.AddJob(
		// 	disquuun.AddJob(queueId, new byte[10]).DEPRICATED_Sync()
		// );
		
		// var result = disquuun.FastAck(new string[]{jobId}).DEPRICATED_Sync();
		// var ackCount = DisquuunDeserializer.FastAck(result);
		// Assert(1, ackCount, "not match.");
	}
	
	public void _1_6_Info_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var infoData = disquuun.Info().DEPRICATED_Sync();
		var infoResult = DisquuunDeserializer.Info(infoData);
		
		Assert(0, infoResult.jobs.registered_jobs, "not match.");
	}
	
	public void _1_7_Hello_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var helloData = disquuun.Hello().DEPRICATED_Sync();
		var helloResult = DisquuunDeserializer.Hello(helloData);
		
		Assert("1", helloResult.version, "not match.");
	}
	
	public void _1_8_Qlen_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		var jobId = DisquuunDeserializer.AddJob(
			disquuun.AddJob(queueId, new byte[10]).DEPRICATED_Sync()
		);
		
		var qlen = DisquuunDeserializer.Qlen(disquuun.Qlen(queueId).DEPRICATED_Sync());
		Assert(1, qlen, "not match.");
		
		disquuun.FastAck(new string[]{jobId}).DEPRICATED_Sync();
	}
	
	public void _1_9_Qstat_Sync (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		var queueId = Guid.NewGuid().ToString();
		var jobId = DisquuunDeserializer.AddJob(
			disquuun.AddJob(queueId, new byte[10]).DEPRICATED_Sync()
		);
		
		var data = disquuun.Qstat(queueId).DEPRICATED_Sync();
		var qstatData = DisquuunDeserializer.Qstat(data);
		Assert(1, qstatData.len, "not match.");
		
		disquuun.FastAck(new string[]{jobId}).DEPRICATED_Sync();
	}
	
	
	public void _1_10_Qpeek_Sync (Disquuun disquuun) {
		Disquuun.Log("_1_10_Qpeek_Sync not yet applied");
		// <queue-name> <count>
		
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var queueId = Guid.NewGuid().ToString();
		// var jobId = DisquuunDeserializer.AddJob(
		// 	disquuun.AddJob(queueId, new byte[10]).DEPRICATED_Sync()
		// );
		
		// var data = disquuun.Qpeek(queueId, 1).DEPRICATED_Sync();
		// var infoResult = DisquuunDeserializer.Info(infoData);
		
		// Assert(0, infoResult.jobs.registered_jobs, "not match.");
	}
	
	public void _1_11_Enqueue_Sync (Disquuun disquuun) {
		Disquuun.Log("_1_11_Enqueue_Sync not yet applied");
		// <job-id> ... <job-id>
		
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var infoData = disquuun.Info().DEPRICATED_Sync();
		// var infoResult = DisquuunDeserializer.Info(infoData);
		
		// Assert(0, infoResult.jobs.registered_jobs, "not match.");
	}
	
	public void _1_12_Dequeue_Sync (Disquuun disquuun) {
		Disquuun.Log("_1_12_Dequeue_Sync not yet applied");
		// <job-id> ... <job-id>
		
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var infoData = disquuun.Info().DEPRICATED_Sync();
		// var infoResult = DisquuunDeserializer.Info(infoData);
		
		// Assert(0, infoResult.jobs.registered_jobs, "not match.");
	}
	
	public void _1_13_DelJob_Sync (Disquuun disquuun) {
		Disquuun.Log("_1_13_DelJob_Sync not yet applied");
		// <job-id> ... <job-id>
		
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var infoData = disquuun.Info().DEPRICATED_Sync();
		// var infoResult = DisquuunDeserializer.Info(infoData);
		
		// Assert(0, infoResult.jobs.registered_jobs, "not match.");
	}
	
	public void _1_14_Show_Sync (Disquuun disquuun) {
		Disquuun.Log("_1_14_Show_Sync not yet applied");
		// <job-id>
		
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var infoData = disquuun.Info().DEPRICATED_Sync();
		// var infoResult = DisquuunDeserializer.Info(infoData);
		
		// Assert(0, infoResult.jobs.registered_jobs, "not match.");
	}
	
	public void _1_15_Qscan_Sync (Disquuun disquuun) {
		Disquuun.Log("_1_15_Qscan_Sync not yet applied");
		// [COUNT <count>] [BUSYLOOP] [MINLEN <len>] [MAXLEN <len>] [IMPORTRATE <rate>]
		
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var infoData = disquuun.Info().DEPRICATED_Sync();
		// var infoResult = DisquuunDeserializer.Info(infoData);
		
		// Assert(0, infoResult.jobs.registered_jobs, "not match.");
	}
	
	public void _1_16_Jscan_Sync (Disquuun disquuun) {
		Disquuun.Log("_1_16_Jscan_Sync not yet applied");
		// [<cursor>] [COUNT <count>] [BUSYLOOP] [QUEUE <queue>] [STATE <state1> STATE <state2> ... STATE <stateN>] [REPLY all|id]
		
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var infoData = disquuun.Info().DEPRICATED_Sync();
		// var infoResult = DisquuunDeserializer.Info(infoData);
		
		// Assert(0, infoResult.jobs.registered_jobs, "not match.");
	}
	
	public void _1_17_Pause_Sync (Disquuun disquuun) {
		Disquuun.Log("_1_17_Pause_Sync not yet applied");
		// <queue-name> option1 [option2 ... optionN]
		
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var pauseData = disquuun.Pause().DEPRICATED_Sync();
		// var pauseResult = DisquuunDeserializer.Info(pauseData);
		
		// Assert(0, pauseResult.jobs.registered_jobs, "not match.");
	}
}