#Disquuun

C# Disque client.  
ver 0.7.4 (only essential Disque commands and options are supported.)

DotNet Core 1.0 adopetd. Running on DotNet 3.5 ~ Core 1.0

##Table of Contents
  * [Motivation](#motivation)
  * [Basic Usage](#basic-usage)
  * [Loop usage](#loop-usage)
  * [Pipeline usage](#pipeline-usage)
  * [Test](#test)
  * [License](#license)
  * [Contribution](#contribution)
  

##Motivation
Lightweight, async, zero copy, independent. not depends on any Redis library code. 

**Lightweight**  
No threads contained,  
but the Loop mechanism is included for repeating the Disque commands.

**async**  
Every operation runs async basically.

**zero copy**  
Zero memory allocation after receiving data from Disque. you can get data as ArraySegment then you can copy it by using **DisquuunDeserializer**.

**independent**  
yes. it is for making allocation smaller than before.


"n" means "this is written in C#".


##Basic Usage

here is connect to Disque server -> AddJob -> GetJob -> FastAck async sample.  

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
						// got job by queueId from Disque server.
						
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
(it is used for tests only.)

##Loop usage
Disquuun can repeat command easily.  
Getting job with Loop() sample is below.

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

The Loop() method's frequency is depends on behaviour of the Dique's API.  
 GetJob without "nohang" option will be locked until queueId-queue gets new jobs in Disque. 

In this case you can wait the incoming of new job data and then keep waiting next job data.


##Pipeline usage
Disquuun enables Pipelining of Disque commands.
Add -> Getting job with Pipeline sample is below.

```C#
var fastacked = false;
		
disquuun.Pipeline(
	disquuun.AddJob("my queue name", new byte[100]),
	disquuun.GetJob(new string[]{"my queue name"})
).Execute(
	(command, data) => {
		if (command != DisqueCommand.GETJOB) return;
		var jobs = DisquuunDeserializer.GetJob(data);
		
		var jobIds = jobs.Select(jobData => jobData.jobId).ToArray();
		var jobDatas = jobs.Select(jobData => jobData.jobData).ToList();
		
		/*
			fast ack all.
		*/
		disquuun.FastAck(jobIds).Async(
			(command2, data2) => {
				fastacked = true;
			}
		);
	}
);

WaitUntil("_0_0_4_ReadmeSamplePipeline", () => fastacked, 5);
```

The disquuun.Pipeline() method is for pipelining Disque commands.
use Execute(Callback) to execute pipelined commands.

You can also use Execute(Callback) with empty Pipeline() method like below.

```C#
for (var i = 0; i < 1000; i++) disquuun.Pipeline(disquuun.AddJob("my queue name", new byte[100]));

disquuun.Pipeline().Execute(
	...
);
```

##Test
This repository is DotNet Core project and runnable.
And the project contains test codes of Disquuun.

required and not contained: Disque Server.

##License
MIT.


##Contribution
very welcome!! especially implement the read-block of received Disque commands and options.