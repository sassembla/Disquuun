using System;
using System.Text;

namespace DisquuunCore.Deserialize {
	
	public static class DisquuunDeserializer {
		
		public static string AddJob (Disquuun.ByteDatas[] data) {
			var idStrBytes = new byte[data[0].bytesArray[0].Length];
			TestLogger.Log("idStrBytes:" + idStrBytes.Length);
			return Encoding.UTF8.GetString(idStrBytes);
		}
		
		public struct JobData {
			public readonly string jobId;
			public readonly byte[] jobData;
			public JobData (Disquuun.ByteDatas dataSourceBytes) {
				this.jobId = Encoding.UTF8.GetString(dataSourceBytes.bytesArray[0]);
				this.jobData = dataSourceBytes.bytesArray[1];
			}
		}
		
		public static JobData[] GetJob (Disquuun.ByteDatas[] data) {
			var jobDatas = new JobData[data.Length];
			for (var i = 0; i < data.Length; i++) {
				var jobDataSource = data[i];
				jobDatas[i] = new JobData(jobDataSource);
			}
			return jobDatas;
		}
	
// ACKJOB,// jobid1 jobid2 ... jobidN
// FASTACK,// jobid1 jobid2 ... jobidN
// WORKING,// jobid
// NACK,// <job-id> ... <job-id>
	
	
		public static string Info (Disquuun.ByteDatas[] data) {
			return Encoding.UTF8.GetString(data[0].bytesArray[0], 0, data[0].bytesArray[0].Length);
		}
		
		public static string Hello (Disquuun.ByteDatas[] data) {
			return Encoding.UTF8.GetString(data[0].bytesArray[0], 0, data[0].bytesArray[0].Length);
		}
	}
	
// QLEN,// <queue-name>
// QSTAT,// <queue-name>
// QPEEK,// <queue-name> <count>
// ENQUEUE,// <job-id> ... <job-id>
// DEQUEUE,// <job-id> ... <job-id>
// DELJOB,// <job-id> ... <job-id>
// SHOW,// <job-id>
// QSCAN,// [COUNT <count>] [BUSYLOOP] [MINLEN <len>] [MAXLEN <len>] [IMPORTRATE <rate>]
// JSCAN,// [<cursor>] [COUNT <count>] [BUSYLOOP] [QUEUE <queue>] [STATE <state1> STATE <state2> ... STATE <stateN>] [REPLY all|id]
// PAUSE,// <queue-name> option1 [option2 ... optionN]
		
		
}