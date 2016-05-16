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