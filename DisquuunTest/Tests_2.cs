using System;
using System.Linq;
using DisquuunCore;
using DisquuunCore.Deserialize;

/*
	api async tests.
*/

public partial class Tests {
	public void _2_0_AddJob_Async (Disquuun disquuun) {
		WaitUntil("_2_0_AddJob_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		var jobId = string.Empty;
		disquuun.AddJob(queueId, new byte[10]).Async(
			(command, result) => {
				jobId = DisquuunDeserializer.AddJob(result);
			}
		);
		
		WaitUntil("_2_0_AddJob_Async", () => !string.IsNullOrEmpty(jobId), 5);
		
		// ack in.
		disquuun.FastAck(new string[]{jobId}).DEPRICATED_Sync();
	}
	
	public void _2_1_GetJob_Async (Disquuun disquuun) {
		WaitUntil("_2_1_GetJob_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		disquuun.AddJob(queueId, new byte[10]).DEPRICATED_Sync();
		
		DisquuunDeserializer.JobData[] jobDatas = new DisquuunDeserializer.JobData[]{};
		
		disquuun.GetJob(new string[]{queueId}).Async(
			(command, result) => {
				jobDatas = DisquuunDeserializer.GetJob(result);
			}
		);
		
		WaitUntil("_2_1_GetJob_Async", () => (jobDatas.Length == 1), 5);
		
		// ack in.
		var jobId = jobDatas[0].jobId;
		disquuun.FastAck(new string[]{jobId}).DEPRICATED_Sync();
	}
	
	public void _2_1_1_GetJobWithCount_Async (Disquuun disquuun) {
		WaitUntil("_2_1_1_GetJobWithCount_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		var addJobCount = 10000;
		for (var i = 0; i < addJobCount; i++) disquuun.AddJob(queueId, new byte[100]).DEPRICATED_Sync();
		
		DisquuunDeserializer.JobData[] jobDatas = new DisquuunDeserializer.JobData[]{};
		disquuun.GetJob(new string[]{queueId}, "COUNT", addJobCount).Async(
			(command, result) => {
				jobDatas = DisquuunDeserializer.GetJob(result);
			}
		);
		
		WaitUntil("_2_1_1_GetJobWithCount_Async", () => (jobDatas.Length == addJobCount), 5);
		
		// ack in.
		var jobIds = jobDatas.Select(job => job.jobId).ToArray();
		disquuun.FastAck(jobIds).DEPRICATED_Sync();
	}
	
	public void _2_1_2_GetJobFromMultiQueue_Async (Disquuun disquuun) {
		WaitUntil("_2_1_2_GetJobFromMultiQueue_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId1 = Guid.NewGuid().ToString();
		disquuun.AddJob(queueId1, new byte[10]).DEPRICATED_Sync();
		
		var queueId2 = Guid.NewGuid().ToString();
		disquuun.AddJob(queueId2, new byte[10]).DEPRICATED_Sync();
		
		DisquuunDeserializer.JobData[] jobDatas = new DisquuunDeserializer.JobData[]{};
		disquuun.GetJob(new string[]{queueId1, queueId2}, "count", 2).Async(
			(command, result) => {
				jobDatas = DisquuunDeserializer.GetJob(result);
			}
		);
		
		WaitUntil("_2_1_2_GetJobFromMultiQueue_Async", () => (jobDatas.Length == 2), 5);
		
		// ack in.
		var jobIds = jobDatas.Select(job => job.jobId).ToArray();
		disquuun.FastAck(jobIds).DEPRICATED_Sync();
	}
	
	public void _2_1_3_GetJobWithNoHang_Async (Disquuun disquuun) {
		WaitUntil("_2_1_3_GetJobWithNoHang_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		bool received = false;
		disquuun.GetJob(new string[]{queueId}, "NOHANG").Async(
			(command, result) => {
				var jobDatas = DisquuunDeserializer.GetJob(result);
				Assert("_2_1_3_GetJobWithNoHang_Async", jobDatas.Length == 0, "not match.");
				received = true;
			}
		);
		
		WaitUntil("_2_1_3_GetJobWithNoHang_Async", () => received, 5);
	}
	
	public void _2_1_4_GetJobWithCounters_Async (Disquuun disquuun) {
		WaitUntil("_2_1_4_GetJobWithCounters_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		
		disquuun.AddJob(queueId, new byte[100]).DEPRICATED_Sync();
		
		var ackCount = -1;
		
		disquuun.GetJob(new string[]{queueId}, "withcounters").Async(
			(command, result) => {
				var jobDatas = DisquuunDeserializer.GetJob(result);
				ackCount = jobDatas[0].additionalDeliveriesCount;
				disquuun.FastAck(new string[]{jobDatas[0].jobId}).Async(
					(c,d) => {}
				);
			}
		);
		
		WaitUntil("_2_1_4_GetJobWithCounters_Async", () => (ackCount == 0), 5);
	}
	
	public void _2_2_AckJob_Async (Disquuun disquuun) {
		WaitUntil("_2_2_AckJob_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		var jobId = DisquuunDeserializer.AddJob(
			disquuun.AddJob(queueId, new byte[10]).DEPRICATED_Sync()
		);
		
		var ackCount = 0;
		disquuun.AckJob(new string[]{jobId}).Async(
			(command, result) => {
				ackCount = DisquuunDeserializer.AckJob(result);
			}
		);
		
		WaitUntil("_2_2_AckJob_Async", () => (ackCount == 1), 5);
	}
	
	public void _2_3_Fastack_Async (Disquuun disquuun) {
		WaitUntil("_2_3_Fastack_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		var jobId = DisquuunDeserializer.AddJob(
			disquuun.AddJob(queueId, new byte[10]).DEPRICATED_Sync()
		);
		
		var ackCount = 0;
		disquuun.FastAck(new string[]{jobId}).Async(
			(command, result) => {
				ackCount = DisquuunDeserializer.FastAck(result);
			}
		);
		
		WaitUntil("_2_3_Fastack_Async", () => (ackCount == 1), 5);
	}
	
	public void _2_4_Working_Async (Disquuun disquuun) {
		DisquuunLogger.Log("_1_4_Working_Async not yet applied");
		// WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var queueId = Guid.NewGuid().ToString();
		// var jobId = DisquuunDeserializer.AddJob(
		// 	disquuun.AddJob(queueId, new byte[10]).DEPRICATED_Sync()
		// );
		
		// var workResult = disquuun.Working(jobId).DEPRICATED_Sync();
		// var workingResult = DisquuunDeserializer.Working(workResult);
		
		// var result = disquuun.FastAck(new string[]{jobId}).DEPRICATED_Sync();
		// var ackCount = DisquuunDeserializer.FastAck(result);
		// Assert("", 1, ackCount, "not match.");
	}
	
	public void _2_5_Nack_Async (Disquuun disquuun) {
		DisquuunLogger.Log("_1_5_Nack_Async not yet applied");
		// WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var queueId = Guid.NewGuid().ToString();
		// var jobId = DisquuunDeserializer.AddJob(
		// 	disquuun.AddJob(queueId, new byte[10]).DEPRICATED_Sync()
		// );
		
		// var result = disquuun.FastAck(new string[]{jobId}).DEPRICATED_Sync();
		// var ackCount = DisquuunDeserializer.FastAck(result);
		// Assert("", 1, ackCount, "not match.");
	}
	
	public void _2_6_Info_Async (Disquuun disquuun) {
		WaitUntil("_2_6_Info_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var registered_jobs = 0;
		
		disquuun.Info().Async(
			(c, data) => {
				var infoResult = DisquuunDeserializer.Info(data);
				registered_jobs = infoResult.jobs.registered_jobs;
			}
		);
		
		WaitUntil("_2_6_Info_Async", () => (registered_jobs == 0), 5);
	}
	
	public void _2_7_Hello_Async (Disquuun disquuun) {
		WaitUntil("_2_7_Hello_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var version = string.Empty;
		
		disquuun.Hello().Async(
			(c, data) => {
				var helloResult = DisquuunDeserializer.Hello(data);
				version = helloResult.version;
			}
		);
		
		WaitUntil("_2_7_Hello_Async", () => (version == "1"), 5);
	}
	
	public void _2_8_Qlen_Async (Disquuun disquuun) {
		WaitUntil("_2_8_Qlen_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		var jobId = DisquuunDeserializer.AddJob(
			disquuun.AddJob(queueId, new byte[10]).DEPRICATED_Sync()
		);
		
		var qlen = 0;
		disquuun.Qlen(queueId).Async(
			(c, data) => {
				qlen = DisquuunDeserializer.Qlen(data);
			}
		);
		WaitUntil("_2_8_Qlen_Async", () => (qlen == 1), 5);
		
		var result = disquuun.FastAck(new string[]{jobId}).DEPRICATED_Sync();
		var ackCount = DisquuunDeserializer.FastAck(result);
	}
	
	public void _2_9_Qstat_Async (Disquuun disquuun) {
		WaitUntil("_2_9_Qstat_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		var queueId = Guid.NewGuid().ToString();
		var jobId = DisquuunDeserializer.AddJob(disquuun.AddJob(queueId, new byte[10]).DEPRICATED_Sync());
		
		var qstatLen = 0;
		disquuun.Qstat(queueId).Async(
			(command, data) => {
				qstatLen = DisquuunDeserializer.Qstat(data).len;
			}
		);
		
		WaitUntil("_2_9_Qstat_Async", () => (qstatLen == 1), 5);
		
		disquuun.FastAck(new string[]{jobId}).DEPRICATED_Sync();
	}
	
	
	public void _2_10_Qpeek_Async (Disquuun disquuun) {
		DisquuunLogger.Log("_2_10_Qpeek_Async not yet applied");
		// <queue-name> <count>
		
		// WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var infoData = disquuun.Info().DEPRICATED_Sync();
		// var infoResult = DisquuunDeserializer.Info(infoData);
		
		// Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
	}
	
	public void _2_11_Enqueue_Async (Disquuun disquuun) {
		DisquuunLogger.Log("_2_11_Enqueue_Async not yet applied");
		// <job-id> ... <job-id>
		
		// WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var infoData = disquuun.Info().DEPRICATED_Sync();
		// var infoResult = DisquuunDeserializer.Info(infoData);
		
		// Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
	}
	
	public void _2_12_Dequeue_Async (Disquuun disquuun) {
		DisquuunLogger.Log("_2_12_Dequeue_Async not yet applied");
		// <job-id> ... <job-id>
		
		// WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var infoData = disquuun.Info().DEPRICATED_Sync();
		// var infoResult = DisquuunDeserializer.Info(infoData);
		
		// Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
	}
	
	public void _2_13_DelJob_Async (Disquuun disquuun) {
		DisquuunLogger.Log("_2_13_DelJob_Async not yet applied");
		// <job-id> ... <job-id>
		
		// WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var infoData = disquuun.Info().DEPRICATED_Sync();
		// var infoResult = DisquuunDeserializer.Info(infoData);
		
		// Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
	}
	
	public void _2_14_Show_Async (Disquuun disquuun) {
		DisquuunLogger.Log("_2_14_Show_Async not yet applied");
		// <job-id>
		
		// WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var infoData = disquuun.Info().DEPRICATED_Sync();
		// var infoResult = DisquuunDeserializer.Info(infoData);
		
		// Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
	}
	
	public void _2_15_Qscan_Async (Disquuun disquuun) {
		DisquuunLogger.Log("_2_15_Qscan_Async not yet applied");
		// [COUNT <count>] [BUSYLOOP] [MINLEN <len>] [MAXLEN <len>] [IMPORTRATE <rate>]
		
		// WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var infoData = disquuun.Info().DEPRICATED_Sync();
		// var infoResult = DisquuunDeserializer.Info(infoData);
		
		// Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
	}
	
	public void _2_16_Jscan_Async (Disquuun disquuun) {
		DisquuunLogger.Log("_2_16_Jscan_Async not yet applied");
		// [<cursor>] [COUNT <count>] [BUSYLOOP] [QUEUE <queue>] [STATE <state1> STATE <state2> ... STATE <stateN>] [REPLY all|id]
		
		// WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var infoData = disquuun.Info().DEPRICATED_Sync();
		// var infoResult = DisquuunDeserializer.Info(infoData);
		
		// Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
	}
	
	public void _2_17_Pause_Async (Disquuun disquuun) {
		DisquuunLogger.Log("_2_17_Pause_Async not yet applied");
		// <queue-name> option1 [option2 ... optionN]
		
		// WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		// var pauseData = disquuun.Pause().DEPRICATED_Sync();
		// var pauseResult = DisquuunDeserializer.Info(pauseData);
		
		// Assert("", 0, pauseResult.jobs.registered_jobs, "not match.");
	}
}