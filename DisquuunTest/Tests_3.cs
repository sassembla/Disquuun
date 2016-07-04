using System;
using System.Linq;
using DisquuunCore;
using DisquuunCore.Deserialize;

/*
	multiple execution.
*/

public partial class Tests {
	public void _3_0_Nested2AsyncSocket (Disquuun disquuun) {
		WaitUntil("_3_0_Nested2AsyncSocket", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var jobId1 = string.Empty;
		
		var queueId1 = Guid.NewGuid().ToString();
		disquuun.AddJob(queueId1, new byte[10]).Async(
			(command, data) => {
				jobId1 = DisquuunDeserializer.AddJob(data);
			}
		);
		
		var jobId2 = string.Empty;
		
		var queueId2 = Guid.NewGuid().ToString();
		disquuun.AddJob(queueId2, new byte[10]).Async(
			(command, data) => {
				jobId2 = DisquuunDeserializer.AddJob(data);
			}
		);
		
		WaitUntil("_3_0_Nested2AsyncSocket", () => (!string.IsNullOrEmpty(queueId1) && !string.IsNullOrEmpty(queueId2)), 5);
		
		var done = false;
		disquuun.GetJob(new string[]{queueId1, queueId2}, "count", 2).Async(
			(command, data) => {
				var gets = DisquuunDeserializer.GetJob(data);
				
				Assert("_3_0_Nested2AsyncSocket", 2, gets.Length, "not match.");
				
				disquuun.FastAck(gets.Select(job => job.jobId).ToArray()).Async(
					(c, d) => {
						done = true;
					}
				);
			}	
		);
		
		WaitUntil("_3_0_Nested2AsyncSocket", () => done, 5);
	}
	
	public void _3_1_NestedMultipleAsyncSocket (Disquuun disquuun) {
		WaitUntil("_3_1_NestedMultipleAsyncSocket", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		var addGetFastAckCount = 100;
		var resultCount = 0;
		
		for (var i = 0; i < addGetFastAckCount; i++) {
			disquuun.AddJob(queueId, new byte[10]).Async(
				(c1, d1) => {
					Assert("_3_1_NestedMultipleAsyncSocket", DisqueCommand.ADDJOB, c1, "command mismatch.");
					disquuun.GetJob(new string[]{queueId}).Async(
						(c2, d2) => {
							Assert("_3_1_NestedMultipleAsyncSocket", DisqueCommand.GETJOB, c2, "command mismatch.");
							var gotJobs = DisquuunDeserializer.GetJob(d2);
							var gotJobId = gotJobs[0].jobId;
							var gotJobData = gotJobs[0].jobData;
							Assert("_3_1_NestedMultipleAsyncSocket", 10, gotJobData.Length, "not match.");
							
							disquuun.FastAck(new string[]{gotJobId}).Async(
								(c3, d3) => {
									Assert("_3_1_NestedMultipleAsyncSocket", DisqueCommand.FASTACK, c3, "command mismatch.");
									var fastackResult = DisquuunDeserializer.FastAck(d3);
									Assert("_3_1_NestedMultipleAsyncSocket", 1, fastackResult, "not match.");
									resultCount++;
								}
							);
						}
					);
				}
			);
		}
		
		
		WaitUntil("_3_1_NestedMultipleAsyncSocket", () => (resultCount == addGetFastAckCount), 10);
	}
}