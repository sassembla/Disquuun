using System;
using System.Collections.Generic;
using System.Linq;
using DisquuunCore;
using DisquuunCore.Deserialize;

/*
	
*/

public partial class Tests {
	public void _8_0_LargeSizeSendThenSmallSizeSendMakeEmitOnSendAfterOnReceived (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		for (var i = 0; i < 100; i++) {
			var queueId = Guid.NewGuid().ToString();
			
			var sended = false;
			disquuun.AddJob(queueId, new byte[40000]).Async(
				(command, data) => {
					disquuun.AddJob(queueId, new byte[100]).Async(
						(command2, data2) => {
							sended = true;
						}	
					);
				}
			);

			WaitUntil(() => (sended), 5);

			var fastacked = false;
			disquuun.GetJob(new string[]{queueId}, "count", 2).Async(
				(command, data) => {
					var jobDatas = DisquuunDeserializer.GetJob(data);
					var jobIds = jobDatas.Select(j => j.jobId).ToArray();
					disquuun.FastAck(jobIds).Async(
						(command2, data2) => {
							fastacked = true;
						}
					);
				}
			);

			WaitUntil(() => fastacked, 5);
		}
	}
	
	public void _8_1_LargeSizeSendThenSmallSizeSendLoopMakeEmitOnSendAfterOnReceived (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		for (var i = 0; i < 100; i++) {
			var queueId = Guid.NewGuid().ToString();
			
			var index = 0;
			var bytes = new List<byte[]>();
			
			bytes.Add(new byte[40000]);
			bytes.Add(new byte[100]);

			disquuun.AddJob(queueId, bytes[index]).Loop(
				(command, data) => {
					index++;
					if (bytes.Count <= index) return false;
					return true;
				}
			);

			WaitUntil(() => (index == 2), 1);
			
			var fastacked = false;
			disquuun.GetJob(new string[]{queueId}, "count", 20).Async(
				(command, data) => {
					var jobDatas = DisquuunDeserializer.GetJob(data);
					var jobIds = jobDatas.Select(j => j.jobId).ToArray();
					disquuun.FastAck(jobIds).Async(
						(command2, data2) => {
							fastacked = true;
						}
					);
				}
			);
			
			WaitUntil(() => fastacked, 1);
		}
	}
}