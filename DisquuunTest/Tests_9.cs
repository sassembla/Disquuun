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

	private object _0_9_1_MultiplePipelinesObject = new object(); 
	
	public void _0_9_1_MultiplePipelines (Disquuun disquuun) {
		WaitUntil("_0_9_1_MultiplePipelines", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

		var infoCount = 0;

		disquuun.Pipeline(disquuun.Info());
		disquuun.Pipeline(disquuun.Info());
		disquuun.Pipeline(disquuun.Info()).Execute( 
			(command, data) => {
				lock (_0_9_1_MultiplePipelinesObject) infoCount++;
			}
		);

		WaitUntil("_0_9_1_MultiplePipelines", () => (infoCount == 3), 5);
	}

	private object _0_9_2_MultipleCommandPipelinesObject = new object();

	public void _0_9_2_MultipleCommandPipelines (Disquuun disquuun) {
		WaitUntil("_0_9_2_MultipleCommandPipelines", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

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
						lock (_0_9_2_MultipleCommandPipelinesObject) {
							infoCount++;
						}
						break;
					}
					case DisqueCommand.ADDJOB: {
						lock (_0_9_2_MultipleCommandPipelinesObject) {
							addedJobId = DisquuunDeserializer.AddJob(data);
						} 
						break;
					}
					case DisqueCommand.GETJOB: {
						lock (_0_9_2_MultipleCommandPipelinesObject) {
							var gotJobDatas = DisquuunDeserializer.GetJob(data);
							gotJobId = gotJobDatas[0].jobId;
							disquuun.FastAck(new string[]{gotJobId}).DEPRICATED_Sync();
						}
						break;
					}
				}
			}
		);

		WaitUntil("_0_9_2_MultipleCommandPipelines", () => (infoCount == 1 && !string.IsNullOrEmpty(addedJobId) && gotJobId == addedJobId), 5);
	}

	private object _0_9_3_SomeCommandPipelinesObject = new object();

	public void _0_9_3_SomeCommandPipelines (Disquuun disquuun) {
		WaitUntil("_0_9_3_SomeCommandPipelines", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
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
						lock (_0_9_3_SomeCommandPipelinesObject) {
							infoCount++;
						}
						break;
					}
					case DisqueCommand.ADDJOB: {
						lock (_0_9_3_SomeCommandPipelinesObject) {
							addedJobIds.Add(DisquuunDeserializer.AddJob(data));
						} 
						break;
					}
				}
			}
		);
		
		disquuun.GetJob(new string[]{queueId}, "count", 1000).Loop(
			(commnand, data) => {
				lock (_0_9_3_SomeCommandPipelinesObject) {
					gotJobIds.AddRange(DisquuunDeserializer.GetJob(data).Select(j => j.jobId));
					if (gotJobIds.Count != jobCount) return true;
					return false;
				}
			}
		);
		
		WaitUntil("_0_9_3_SomeCommandPipelines", () => (infoCount == 1 && addedJobIds.Count == jobCount && gotJobIds.Count == jobCount), 5);

		var fastacked = false;
		disquuun.FastAck(gotJobIds.ToArray()).Async(
			(command, data) => {
				lock (_0_9_3_SomeCommandPipelinesObject) fastacked = true;
			}
		);

		WaitUntil("_0_9_3_SomeCommandPipelines", () => fastacked, 5);
	}

	private object _0_9_4_MassiveCommandPipelinesObject = new object();

	public void _0_9_4_MassiveCommandPipelines (Disquuun disquuun) {
		WaitUntil("_0_9_4_MassiveCommandPipelines", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
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
						lock (_0_9_4_MassiveCommandPipelinesObject) {
							infoCount++;
						}
						break;
					}
					case DisqueCommand.ADDJOB: {
						lock (_0_9_4_MassiveCommandPipelinesObject) {
							addedJobIds.Add(DisquuunDeserializer.AddJob(data));
						} 
						break;
					}
				}
			}
		);
		
		disquuun.GetJob(new string[]{queueId}, "count", jobCount).Loop(
			(commnand, data) => {
				lock (_0_9_4_MassiveCommandPipelinesObject) {
					gotJobIds.AddRange(DisquuunDeserializer.GetJob(data).Select(j => j.jobId));
					if (gotJobIds.Count != jobCount) return true;
					return false;
				}
			}
		);
		
		WaitUntil("_0_9_4_MassiveCommandPipelines 0", () => (infoCount == 1 && addedJobIds.Count == jobCount && gotJobIds.Count == jobCount), 20);
		
		var fastacked = false;
		disquuun.FastAck(gotJobIds.ToArray()).Async(
			(command, data) => {
				lock (_0_9_4_MassiveCommandPipelinesObject) fastacked = true;
			}
		);

		WaitUntil("_0_9_4_MassiveCommandPipelines 1", () => fastacked, 5);
	}
}