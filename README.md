#Disquuun

C# Disque client.  
ver 0.5.0 (essential Disque commands are supported.)

##motivation
Lightweight, async, independent. not depends on any Redis code. 

No threads contained,  
but the Loop mechanism is included for repeating specific Disque command.

"n" means "this is written in C#".


##usage

here is connect -> AddJob -> GetJob -> FastAck async sample.  

```C#
using DisquuunCore;
using DisquuunCore.Deserialize;


Disquuun disquuun = null;

int fastAckedJobCount = 0;
		
disquuun = new Disquuun("127.0.0.1", 7711, 1024, 1,
	disquuunId => {
		var queueId = Guid.NewGuid().ToString();

		// addjob. add 10bytes job to Disque.
		disquuun.AddJob(queueId, new byte[10]).Async(
			(addJobCommand, addJobData) => {
				// job added to queueId @ Disque.
				
				// getjob. get job from Disque.
				disquuun.GetJob(new string[]{queueId}).Async(
					(getJobCommand, getJobData) => {
						// got job from queueId @ Disque.
						
						var jobDatas = DisquuunDeserializer.GetJob(getJobData);
						Assert(1, jobDatas.Length, "not match.");
						
						// get jobId from got job data.
						var gotJobId = jobDatas[0].jobId;
						
						// fastack it.
						disquuun.FastAck(new string[]{gotJobId}).Async(
							(fastAckCommand, fastAckData) => {
								// fastack succeded or not.
								
								fastAckedJobCount = DisquuunDeserializer.FastAck(fastAckData);
								Assert(1, fastAckedJobCount, "not match.");
							} 
						);
					}
				);
			}
		);
	}
);
	
WaitUntil(() => (fastAckedJobCount == 1), 5);
```
Sync & Async api is supported. but Sync api is already deplicated.  
(only used for tests.)

##advanced usage
Disquuun can getting job with Loop(callback).

```C#
disquuun.GetJob(new string[]{queueId}, "count", 1000).Loop(
	(command, data) => {
		var jobs = DisquuunDeserializer.GetJob(data);
		
		var jobIds = jobs.Select(jobData => jobData.jobId).ToArray();
		var jobDatas = jobs.Select(jobData => jobData.jobData).ToList();
		
		/*
			fast ack all.
		*/
		disquuun.FastAck(jobIds).Async((command2, data2) => {});
		
		InputDatasToContext(jobDatas);
		return true;
	}
);
```

The Loop() frequency is depends on behaviour of the Dique's API.  
 GetJob without "nohang" option will be locked until queueId-queue gets new jobs in Disque. 

In this case you can wait the incoming of new job data and then keep waiting next job data.

##license
MIT.