using System;

public class Test1_AllAPIs : TestBase {
	public override Action[] Setup (string testSuiteId) {
		return new Action[] {
			// add -> get -> ack.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() =>{
				waiting = true;
				disquuun.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
			},
			() => {
				waiting = true;
				disquuun.GetJob(new string[]{jobQueueId});
			},
			() => {
				waiting = true;
				disquuun.AckJob(new string[]{latestGotJobId});
			},
			() => AssertResult("ACKJOB:1", latestResult, "add -> get -> ack."),
			
			
			// add -> get -> fastack.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => {
				waiting = true;
				disquuun.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
			},
			() => {
				waiting = true;
				disquuun.GetJob(new string[]{jobQueueId});
			},
			() => {
				waiting = true;
				disquuun.FastAck(new string[]{latestGotJobId});
			},
			() => AssertResult("FASTACK:1", latestResult, "add -> get -> fastack."),
			
			
			// empty queue, will waiting data.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => {
				disquuun.GetJob(new string[]{jobQueueId});
			},
			() => {
				waiting = true;
				disquuun2.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
			},
			() => {
				waiting = true;
				disquuun.FastAck(new string[]{latestGotJobId});
			},
			() => AssertResult("FASTACK:1", latestResult, "empty queue, will waiting data."),
			
			
			// non exist queue. never back until created.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => {
				disquuun.GetJob(new string[]{jobQueueId});
			},
			() => {
				waiting = true;
				disquuun2.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
			},
			() => {
				waiting = true;
				disquuun.FastAck(new string[]{latestGotJobId});
			},
			() => AssertResult("FASTACK:1", latestResult, "non exist queue. never back until created."),
			
			
			// blocking with empty queue.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => {
				disquuun.GetJob(new string[]{jobQueueId});
			},
			() => {
				waiting = true;
				disquuun2.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
			},
			() => {
				waiting = true;
				disquuun.FastAck(new string[]{latestGotJobId});
			},
			() => AssertResult("FASTACK:1", latestResult, "blocking with empty queue."),
			
			
			// nohang getjob.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => {
				waiting = true;
				disquuun.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
			},
			() => {
				waiting = true;
				disquuun.GetJob(new string[]{jobQueueId}, "NOHANG");
			},
			() => AssertResult("GETJOB:1", latestResult, "nohang getjob."),
			() => {
				waiting = true;
				disquuun.FastAck(new string[]{latestGotJobId});
			},
			
			
			// nohang getjob with non exist queue.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => {
				waiting = true;
				disquuun.GetJob(new string[]{jobQueueId}, "NOHANG");
			},
			() => AssertResult("GETJOB:0", latestResult, "nohang getjob with non exist queue."),
			
			
			// nohang getjob with empty queue.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => {
				waiting = true;
				disquuun.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
			},
			() => {
				waiting = true;
				disquuun.GetJob(new string[]{jobQueueId});
			},
			() => {
				waiting = true;
				disquuun.FastAck(new string[]{latestGotJobId});
			},
			() => {
				waiting = true;
				disquuun.GetJob(new string[]{jobQueueId}, "NOHANG");
			},
			() => AssertResult("GETJOB:0", latestResult, "nohang getjob with empty queue."),
			
			
			// getjob with withcounters.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => {
				waiting = true;
				disquuun.AddJob(jobQueueId, new byte[10]{0,1,2,3,4,5,6,7,8,9}, 0);
			},
			() => {
				waiting = true;
				disquuun.GetJob(new string[]{jobQueueId}, "WITHCOUNTERS");
			},
			() => AssertResult("GETJOB:1", latestResult, "getjob with withcounters."),
			() => {
				waiting = true;
				disquuun.FastAck(new string[]{latestGotJobId});
			},
			
			
			// info
			() => {
				waiting = true;
				disquuun.Info();
			},
			() => AssertResult("INFO:", latestResult, "info"),
			
			
			// hello
			() => {
				waiting = true;
				disquuun.Hello();
			},
			() => AssertResult("HELLO:", latestResult, "hello"),
			
			
			// qlen returns 0.
			() => jobQueueId = Guid.NewGuid().ToString(),
			() => {
				waiting = true;
				disquuun.Qlen(jobQueueId);
			},
			() => AssertResult("QLEN:0", latestResult, "qlen returns 0."),
			
			
			// qlen returns 1.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => {
				waiting = true;
				disquuun.AddJob(jobQueueId, new byte[]{1});
			},
			() => {
				waiting = true;
				disquuun.Qlen(jobQueueId);
			},
			() => AssertResult("QLEN:1", latestResult, "qlen returns 1."),
			() => {
				waiting = true;
				disquuun.GetJob(new string[]{jobQueueId});
			},
			() => {
				waiting = true;
				disquuun.FastAck(new string[]{latestGotJobId});
			},
			
			
			// working return error with unused jobid.
			() => {
				waiting = true;
				disquuun.Working("dummyJobId");
			},
			() => AssertResult("BADID Invalid Job ID format.", latestResult, "working return error with unused job id."),
			
			
			// working return \"seconds\". よくわからん。
			// () => {
			// 	jobQueueId = Guid.NewGuid().ToString();
			// },
			// () => {
			// 	waiting = true;
			// 	disquuun.AddJob(jobQueueId, new byte[]{1,3,5,7,9});
			// },
			// () => {
			// 	waiting = true;
			// 	disquuun.Working(latestAddedJobId);
			// },
			// () => AssertResult("WORKING:300", latestResult, "working return \"seconds\"."),
			// () => {
			// 	waiting = true;
			// 	disquuun.GetJob(new string[]{jobQueueId});
			// },
			// () => {
			// 	waiting = true;
			// 	disquuun.FastAck(new string[]{latestGotJobId});
			// },
			
			
			// nack return error with dummy job id.
			() => {
				waiting = true;
				disquuun.Nack(new string[]{"dummyJobId"});
			},
			() => AssertResult("BADID Invalid Job ID format.", latestResult, "nack return error with dummy job id."),
			
			
			// nack succeeded with status.
			() => {
				jobQueueId = Guid.NewGuid().ToString();
			},
			() => {
				waiting = true;
				disquuun.AddJob(jobQueueId, new byte[]{1,3,5,7,9});
			},
			() => {
				waiting = true;
				disquuun.GetJob(new string[]{jobQueueId});
			},
			() => {
				waiting = true;
				disquuun.Nack(new string[]{latestGotJobId});
			},
			() => AssertResult("NACK:1", latestResult, "nack succeeded with status."),
			() => {
				waiting = true;
				disquuun.GetJob(new string[]{jobQueueId});
			},
			() => {
				waiting = true;
				disquuun.FastAck(new string[]{latestGotJobId});
			},
			
			() => {
				TestLogger.Log("---------------------------result info.---------------------------");
				waiting = true;
				disquuun.Info();
			},
			() => TestLogger.Log("result info:" + latestInfoStr),
			() => {
				disquuun.Disconnect();
				disquuun2.Disconnect();
			}
		};
	}
}

public class Test2_Fast : TestBase {
	public override Action[] Setup (string testSuiteId) {
		return new Action[] {
			() => TestLogger.Log("test started. testSuiteId:" + testSuiteId),
			
			// info
			// () => disquuun.Info(),
			// () => AssertResult("INFO:", latestResult, "info"),
			
			// // hello
			// () => disquuun.Hello(),
			// () => AssertResult("HELLO:", latestResult, "hello"),
			
			// // () => 
			// // () => 
			// // () => 
			// // () => 
			
			// // multiple data in same time.
			// // () => {
			// // 	disquuun.Info();
			// // 	disquuun.Info();
			// // 	disquuun.Info();
			// // 	disquuun.Info();
			// // 	disquuun.Info();
			// // 	disquuun.Info();
			// // },
			
			// // some job.
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
					disquuun.AddJob(jobQueueId, new byte[]{0});
				}
			},
			() => disquuun.GetJob(new string[]{jobQueueId}, "COUNT", 1000),
			() => {
				if (latestWholeGotJobId != null) {
					TestLogger.Log("latestWholeGotJobId:" + latestWholeGotJobId.Length);
					disquuun.FastAck(latestWholeGotJobId);
				} else {
					TestLogger.Log("should wait... maybe getJob is not finished yet.");
				}
			},
			() => AssertResult("FASTACK:1000", latestResult, "mass job."),
			
			() => {
				TestLogger.Log("---------------------------result info.---------------------------");
				waiting = true;
				disquuun2.Info();
			},
			() => TestLogger.Log("result info:" + latestInfoStr),
			() => {
				disquuun.Disconnect();
				disquuun2.Disconnect();
			}
		};
	}
}

public class Test3_Size : TestBase {
	public override Action[] Setup (string testSuiteId) {
		return new Action[] {
			() => TestLogger.Log("test started. testSuiteId:" + testSuiteId),
			
			// size is over maximum.
			() => jobQueueId = Guid.NewGuid().ToString(),
			() => disquuun.AddJob(jobQueueId, new byte[disquuun2.BufferSize-106]),
			() => {
				waiting = true;
				disquuun2.GetJob(new string[]{jobQueueId});
			},
			() => AssertResult("GETJOB:1", latestResult, "size is over maximum."),
			() => {
				waiting = true;
				disquuun.FastAck(latestWholeGotJobId);
			},
			
			
			// size is over maximum2.
			() => jobQueueId = Guid.NewGuid().ToString(),
			() => disquuun.AddJob(jobQueueId, new byte[disquuun2.BufferSize * 3]),
			() => {
				waiting = true;
				disquuun2.GetJob(new string[]{jobQueueId});
			},
			() => AssertResult("GETJOB:1", latestResult, "size is over maximum2."),
			() => {
				waiting = true;
				disquuun.FastAck(latestWholeGotJobId);
			},
			
			
			() => {
				TestLogger.Log("---------------------------result info.---------------------------");
				waiting = true;
				disquuun.Info();
			},
			() => TestLogger.Log("result info:" + latestInfoStr),
			
			() => {
				disquuun.Disconnect();
				disquuun2.Disconnect();
			}
		};
	}
}