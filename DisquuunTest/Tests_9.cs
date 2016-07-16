using System;
using System.Collections.Generic;
using System.Linq;
using DisquuunCore;
using DisquuunCore.Deserialize;

/*
	pipeline tests.
*/

public partial class Tests {
	private object _0_9_0_PipelineCommandsObject = new object();

	public void _0_9_0_PipelineCommands (Disquuun disquuun) {
		WaitUntil("_0_9_0_PipelineCommands", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

		var infoCount = 0;

		disquuun.Pipeline(
			disquuun.Info(), disquuun.Info()
		).Execute( 
			(command, data) => {
				lock (_0_9_0_PipelineCommandsObject) infoCount++;
			}
		);

		WaitUntil("_0_9_0_PipelineCommands", () => (infoCount == 2), 5);
	}

	private object _0_9_1_MultiplePipelineObject = new object(); 
	
	public void _0_9_1_MultiplePipeline (Disquuun disquuun) {
		WaitUntil("_0_9_1_MultiplePipeline", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

		var infoCount = 0;

		disquuun.Pipeline(disquuun.Info());
		disquuun.Pipeline(disquuun.Info());
		disquuun.Pipeline(disquuun.Info()).Execute( 
			(command, data) => {
				lock (_0_9_1_MultiplePipelineObject) infoCount++;
			}
		);

		WaitUntil("_0_9_1_MultiplePipeline", () => (infoCount == 3), 5);
	}

	private object _0_9_2_MultipleCommandPipelineObject = new object();

	public void _0_9_2_MultipleCommandPipeline (Disquuun disquuun) {
		WaitUntil("_0_9_2_MultipleCommandPipeline", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

		var infoCount = 0;
		var addedJobId = string.Empty;
		var gotJobId = "_";

		var queueId = Guid.NewGuid().ToString();

		var jobData = new byte[100];
		for (var i = 0; i < jobData.Length; i++) jobData[i] = 1;

		disquuun.Pipeline(disquuun.Info());
		disquuun.Pipeline(disquuun.AddJob(queueId, jobData));
		disquuun.Pipeline(disquuun.GetJob(new string[]{queueId})).Execute( 
			(command, data) => {
				switch (command) {
					case DisqueCommand.INFO: {
						lock (_0_9_2_MultipleCommandPipelineObject) {
							infoCount++;
						}
						break;
					}
					case DisqueCommand.ADDJOB: {
						lock (_0_9_2_MultipleCommandPipelineObject) {
							addedJobId = DisquuunDeserializer.AddJob(data);
						} 
						break;
					}
					case DisqueCommand.GETJOB: {
						lock (_0_9_2_MultipleCommandPipelineObject) {
							var gotJobDatas = DisquuunDeserializer.GetJob(data);
							gotJobId = gotJobDatas[0].jobId;
							disquuun.FastAck(new string[]{gotJobId}).DEPRICATED_Sync();
						}
						break;
					}
				}
			}
		);

		WaitUntil("_0_9_2_MultipleCommandPipeline", () => (infoCount == 1 && !string.IsNullOrEmpty(addedJobId) && gotJobId == addedJobId), 5);
	}

	private object _0_9_3_SomeCommandPipelineObject = new object();

	public void _0_9_3_SomeCommandPipeline (Disquuun disquuun) {
		WaitUntil("_0_9_3_SomeCommandPipeline", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var jobCount = 1000;

		var infoCount = 0;
		var addedJobIds = new List<string>();
		var gotJobIds = new List<string>();

		var queueId = Guid.NewGuid().ToString();

		var jobData = new byte[100];
		for (var i = 0; i < jobData.Length; i++) jobData[i] = 1;

		for (var i = 0; i < jobCount; i++) disquuun.Pipeline(disquuun.AddJob(queueId, jobData));
		disquuun.Pipeline(disquuun.Info()).Execute( 
			(command, data) => {
				switch (command) {
					case DisqueCommand.INFO: {
						lock (_0_9_3_SomeCommandPipelineObject) {
							infoCount++;
						}
						break;
					}
					case DisqueCommand.ADDJOB: {
						lock (_0_9_3_SomeCommandPipelineObject) {
							addedJobIds.Add(DisquuunDeserializer.AddJob(data));
						} 
						break;
					}
				}
			}
		);
		
		disquuun.GetJob(new string[]{queueId}, "count", 1000).Loop(
			(commnand, data) => {
				lock (_0_9_3_SomeCommandPipelineObject) {
					gotJobIds.AddRange(DisquuunDeserializer.GetJob(data).Select(j => j.jobId));
					if (gotJobIds.Count != jobCount) return true;
					return false;
				}
			}
		);
		
		WaitUntil("_0_9_3_SomeCommandPipeline", () => (infoCount == 1 && addedJobIds.Count == jobCount && gotJobIds.Count == jobCount), 5);

		var fastacked = false;
		disquuun.FastAck(gotJobIds.ToArray()).Async(
			(command, data) => {
				lock (_0_9_3_SomeCommandPipelineObject) fastacked = true;
			}
		);

		WaitUntil("_0_9_3_SomeCommandPipeline", () => fastacked, 5);
	}

	private object _0_9_4_MassiveCommandPipelineObject = new object();

	public void _0_9_4_MassiveCommandPipeline (Disquuun disquuun) {
		WaitUntil("_0_9_4_MassiveCommandPipeline", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var jobCount = 100000;

		var infoCount = 0;
		var addedJobIds = new List<string>();
		var gotJobIds = new List<string>();

		var queueId = Guid.NewGuid().ToString();

		var jobData = new byte[100];
		for (var i = 0; i < jobData.Length; i++) jobData[i] = 1;

		for (var i = 0; i < jobCount; i++) disquuun.Pipeline(disquuun.AddJob(queueId, jobData));
		disquuun.Pipeline(disquuun.Info());
		disquuun.Pipeline().Execute( 
			(command, data) => {
				switch (command) {
					case DisqueCommand.INFO: {
						lock (_0_9_4_MassiveCommandPipelineObject) {
							infoCount++;
						}
						break;
					}
					case DisqueCommand.ADDJOB: {
						lock (_0_9_4_MassiveCommandPipelineObject) {
							addedJobIds.Add(DisquuunDeserializer.AddJob(data));
						} 
						break;
					}
				}
			}
		);
		
		disquuun.GetJob(new string[]{queueId}, "count", jobCount).Loop(
			(commnand, data) => {
				lock (_0_9_4_MassiveCommandPipelineObject) {
					gotJobIds.AddRange(DisquuunDeserializer.GetJob(data).Select(j => j.jobId));
					if (gotJobIds.Count != jobCount) return true;
					return false;
				}
			}
		);
		
		WaitUntil("_0_9_4_MassiveCommandPipeline 0", () => (infoCount == 1 && addedJobIds.Count == jobCount && gotJobIds.Count == jobCount), 20);
		
		var fastacked = false;
		disquuun.FastAck(gotJobIds.ToArray()).Async(
			(command, data) => {
				lock (_0_9_4_MassiveCommandPipelineObject) fastacked = true;
			}
		);

		WaitUntil("_0_9_4_MassiveCommandPipeline 1", () => fastacked, 5);
	}

	private object _0_9_5_PipelinesObject = new object();

	public void _0_9_5_Pipelines (Disquuun disquuun) {
		WaitUntil("_0_9_5_Pipelines", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var jobCount = 100000;

		var infoCount = 0;
		var addedJobIds = new List<string>();
		var gotJobIds = new List<string>();

		var queueId = Guid.NewGuid().ToString();

		var jobData = new byte[100];
		for (var i = 0; i < jobData.Length; i++) jobData[i] = 1;

		for (var i = 0; i < jobCount; i++) {
			if (i == jobCount/2) disquuun.RevolvePipeline(); 
			disquuun.Pipeline(disquuun.AddJob(queueId, jobData));
		}
		
		disquuun.Pipeline(disquuun.Info());

		disquuun.Pipeline().Execute( 
			(command, data) => {
				lock (_0_9_5_PipelinesObject) {
					switch (command) {
						case DisqueCommand.INFO: {
							infoCount++;
							break;
						}
						case DisqueCommand.ADDJOB: {
							addedJobIds.Add(DisquuunDeserializer.AddJob(data));
							break;
						}
					}
				}
			}
		);
		
		WaitUntil("_0_9_5_Pipelines 0", () => (infoCount == 1 && addedJobIds.Count == jobCount), 10);

		disquuun.GetJob(new string[]{queueId}, "count", jobCount).Loop(
			(commnand, data) => {
				lock (_0_9_5_PipelinesObject) {
					var jobDatas = DisquuunDeserializer.GetJob(data).Select(j => j.jobId).ToList();
					gotJobIds.AddRange(jobDatas);
					if (gotJobIds.Count != jobCount) {
						return true;
					}
					return false;
				}
			}
		);

		WaitUntil("_0_9_5_Pipelines 1", () => (gotJobIds.Count == jobCount), 5);
		
		var fastacked = false;
		disquuun.FastAck(gotJobIds.ToArray()).Async(
			(command, data) => {
				lock (_0_9_4_MassiveCommandPipelineObject) fastacked = true;
			}
		);

		WaitUntil("_0_9_5_Pipelines 2", () => fastacked, 5);
	}
}