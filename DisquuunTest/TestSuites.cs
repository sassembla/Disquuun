using System;

public class Test1_AllAPIs : TestBase {
	public override Action[] Ready (string testSuiteId) {
		return new Action[] {
			// add -> get -> ack.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => disquuun.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0),
			() => disquuun.GetJob(new string[]{jobQueueId}),
			() => disquuun.AckJob(new string[]{latestGotJobId}),
			() => AssertResult("ACKJOB:1", latestResult, "add -> get -> ack."),
			
			// add -> get -> fastack.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => disquuun.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0),
			() => disquuun.GetJob(new string[]{jobQueueId}),
			() => disquuun.FastAck(new string[]{latestGotJobId}),
			() => AssertResult("FASTACK:1", latestResult, "add -> get -> fastack."),
			
			// empty queue, will waiting data.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => disquuun.GetJob(new string[]{jobQueueId}),
			() => disquuun2.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0),
			() => disquuun.FastAck(new string[]{latestGotJobId}),
			() => AssertResult("FASTACK:1", latestResult, "empty queue, will waiting data."),
			
			// non exist queue. never back until created.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => disquuun.GetJob(new string[]{jobQueueId}),
			() => disquuun2.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0),
			() => disquuun.FastAck(new string[]{latestGotJobId}),
			() => AssertResult("FASTACK:1", latestResult, "non exist queue. never back until created."),
			
			// info blocking with empty queue.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => disquuun.GetJob(new string[]{jobQueueId}),
			() => disquuun.Info(),
			() => disquuun2.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0),
			() => AssertResult("INFO:", latestResult, "info blocking with empty queue.1"),
			() => disquuun.FastAck(new string[]{latestGotJobId}),
			() => AssertResult("FASTACK:1", latestResult, "info blocking with empty queue.2"),
			
			// non blocking with empty queue.
			// () => disquuun.GetJob(new string[]{"testS"}, "NOHANG"),
			// () => disquuun.Info(),
			
			// info
			() => disquuun.Info(),
			() => AssertResult("INFO:", latestResult, "info"),
			
			// hello
			() => disquuun.Hello(),
			() => AssertResult("HELLO:", latestResult, "hello"),
			
			// qlen returns 0.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => disquuun.Qlen(jobQueueId),
			() => AssertResult("QLEN:0", latestResult, "qlen returns 0."),
			
			// qlen returns 1.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => disquuun.AddJob(jobQueueId, new byte[]{1}),
			() => disquuun.Qlen(jobQueueId),
			() => AssertResult("QLEN:1", latestResult, "qlen returns 1."),
			() => disquuun.GetJob(new string[]{jobQueueId}),
			() => disquuun.FastAck(new string[]{latestGotJobId}),
			
			// working return error with unused jobid.
			() => disquuun.Working("dummyJobId"),
			() => AssertFailureResult("BADID Invalid Job ID format.", latestError, "working return error with unused job id."),
			
			// working return \"seconds\".
			// () => disquuun.AddJob("working return \"seconds\"", new byte[]{1,3,5,7,9}),
			// () => disquuun.Working(latestAddedJobId),
			// () => AssertResult("WORKING:300", latestResult, "working return \"seconds\"."),
			// () => disquuun.GetJob(new string[]{"working return \"seconds\""}),
			// () => disquuun.FastAck(new string[]{latestGotJobId}),
			
			// nack return error with dummy job id.
			() => disquuun.Nack(new string[]{"dummyJobId"}),
			() => AssertFailureResult("BADID Invalid Job ID format.", latestError, "nack return error with dummy job id."),
			
			// nack succeeded with status.
			// まだうまくいってない気がする。
			// () => disquuun.AddJob("testNack", new byte[]{1,3,5,7,9}),
			// () => disquuun.Nack(new string[]{latestAddedJobId}),
			// () => AssertResult("NACK:1", latestResult, "nack succeeded with status."),
			// () => disquuun.GetJob(new string[]{"testNack"}),
			// () => disquuun.FastAck(new string[]{latestGotJobId}),
			
			() => {
				testLogger.Log("---------------------------result info.---------------------------");
				disquuun2.Info();
			},
			() => {
				disquuun.Disconnect();
				disquuun2.Disconnect();
			}
		};
	}
}

public class Test2_Fast : TestBase {
	public override Action[] Ready (string testSuiteId) {
		return new Action[] {
			() => testLogger.Log("test started. testSuiteId:" + testSuiteId),
			
			// info
			() => disquuun.Info(),
			() => AssertResult("INFO:", latestResult, "info"),
			
			// hello
			() => disquuun.Hello(),
			() => AssertResult("HELLO:", latestResult, "hello"),
			
			// () => 
			// () => 
			// () => 
			// () => 
			
			// multiple data in same time.
			// () => {
			// 	disquuun.Info();
			// 	disquuun.Info();
			// 	disquuun.Info();
			// 	disquuun.Info();
			// 	disquuun.Info();
			// 	disquuun.Info();
			// },
			
			// some job.
			// () => {
			// 	jobQueueId = Guid.NewGuid().ToString();
			// },
			// () => {
			// 	for (var i = 0; i < 2; i++) {
			// 		disquuun2.AddJob(jobQueueId, new byte[]{0});
			// 	}
			// },
			// () => disquuun.GetJob(new string[]{jobQueueId}, "COUNT", 10),
			// () => disquuun.FastAck(latestWholeGotJobId),
			// () => AssertResult("FASTACK:2", latestResult, "some job."),
			
			// mass job.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => {
				for (var i = 0; i < 1000; i++) {
					disquuun2.AddJob(jobQueueId, new byte[]{0});
				}
			},
			() => disquuun.GetJob(new string[]{jobQueueId}, "COUNT", 1000),
			() => disquuun.FastAck(latestWholeGotJobId),
			() => AssertResult("FASTACK:1000", latestResult, "mass job."),
			
			() => {
				testLogger.Log("---------------------------result info.---------------------------");
				disquuun2.Info();
			},
			
			() => {
				disquuun.Disconnect();
				disquuun2.Disconnect();
			}
		};
	}
}