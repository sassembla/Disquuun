#Disquuun

C# disque client.

lightweight, small, independent. not depends on any Redis code.  
"n" means "this is written in C#".


##usage
```C#
using DisquuunCore;
using DisquuunCore.Deserialize;


Disquuun disquuun = null;

disquuun = new Disquuun("127.0.0.1", 7711, 1024, 1,
	disquuunId => {
		var queueId = Guid.NewGuid().ToString();

		// addjob. add 10bytes job to Disque.
		disquuun.AddJob(queueId, new byte[10]).Sync();

		// getjob. get job from Disque.
		var result = disquuun.GetJob(new string[]{queueId}).Sync();
		var jobDatas = DisquuunDeserializer.GetJob(result);

		Assert(1, jobDatas.Length, "not match.");

		// fastack.
		var jobId = jobDatas[0].jobId;
		disquuun.FastAck(new string[]{jobId}).Sync();
	}
);
```
sync & async api is supported.


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

The Loop() frequency is depends on behaviour of the Dique's API. GetJob without "nohang" option will be locked until queueId-queue gets new jobs in Disque. In this case you can wait the incoming of new job data and then keep waiting next job data.