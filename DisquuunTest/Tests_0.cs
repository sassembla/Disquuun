using System;
using System.Collections.Generic;

using DisquuunCore;
using DisquuunCore.Deserialize;

/*
	basement api tests.
*/

public partial class Tests {
	public void _0_0_InitWith2Connection (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
	}
	
	public void _0_0_1_WaitOnOpen2Connection (Disquuun disquuun) {
		var conId = string.Empty;
		var disquuun2 = new Disquuun("127.0.0.1", 7711, 1, 1, 
			connectionId => {
				conId = connectionId;
			},
			e => {
				
			}
		);
		WaitUntil(() => !string.IsNullOrEmpty(conId), 5);
		
		disquuun2.Disconnect(true);
	}
	
	public void _0_0_2_ReadmeSample (Disquuun disquuun) {
		Disquuun disquuun2 = null;
		
		bool overed = false;
		disquuun2 = new Disquuun("127.0.0.1", 7711, 1024, 1,
			disquuunId => {
				var queueId = Guid.NewGuid().ToString();

				// addjob. add 10bytes job to Disque.
				disquuun2.AddJob(queueId, new byte[10]).Sync();

				// getjob. get job from Disque.
				var result = disquuun2.GetJob(new string[]{queueId}).Sync();
				var jobDatas = DisquuunDeserializer.GetJob(result);

				Assert(1, jobDatas.Length, "not match.");

				// fastack.
				var jobId = jobDatas[0].jobId;
				disquuun2.FastAck(new string[]{jobId}).Sync();
				
				overed = true;
			}
		);
		
		WaitUntil(() => overed, 5);
		
		disquuun2.Disconnect(true);
	}

    public void _0_1_ConnectionFailedWithNoDisqueServer (Disquuun disquuun) {
		
		Exception e = null;
		Action<Exception> Failed = (Exception e2) => {
			// set error to param,
			e = e2;
			// TestLogger.Log("e:" + e);
		};
		
		var disquuun2 = new Disquuun("127.0.0.1", 8888, 1024, 1, 
			conId => {},
			e2 => {
				// set error to param,
				e = e2;
				// TestLogger.Log("e:" + e);
			}
		);
		
		WaitUntil(() => (e != null), 1);
		
		disquuun2.Disconnect(true);
	}
	
	public void _0_2_SyncInfo (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		var data = disquuun.Info().Sync();
		var infoStr = DisquuunDeserializer.Info(data).rawString;
		Assert(!string.IsNullOrEmpty(infoStr), "empty.");
	}
	
	public void _0_3_SyncInfoTwice (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		{
			var datas = disquuun.Info().Sync();
			var infoStr = DisquuunDeserializer.Info(datas).rawString;
			Assert(!string.IsNullOrEmpty(infoStr), "empty.");
		}
		
		{
			var datas = disquuun.Info().Sync();
			var infoStr = DisquuunDeserializer.Info(datas).rawString;
			Assert(!string.IsNullOrEmpty(infoStr), "empty.");
		}	
	}
	
	public void _0_4_AsyncInfo (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var infoStr = string.Empty;
		disquuun.Info().Async(
			(DisqueCommand command, DisquuunResult[] datas) => {
				infoStr = DisquuunDeserializer.Info(datas).rawString;
			}
		);
		
		WaitUntil(() => !string.IsNullOrEmpty(infoStr), 5);
	}
	
	public void _0_5_LoopInfo_Once (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var infoStr = string.Empty;
		disquuun.Info().Loop(
			(DisqueCommand command, DisquuunResult[] datas) => {
				infoStr = DisquuunDeserializer.Info(datas).rawString;
				return false;
			} 
		);
		
		WaitUntil(() => !string.IsNullOrEmpty(infoStr), 5);
	}
	
	public void _0_6_LoopInfo_Twice (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var infos = new List<string>();
		disquuun.Info().Loop(
			(DisqueCommand command, DisquuunResult[] datas) => {
				var infoStr = DisquuunDeserializer.Info(datas).rawString;
				infos.Add(infoStr);
				if (infos.Count < 2) return true;
				return false;
			} 
		);
		
		WaitUntil(() => (infos.Count == 2), 5);
	}
	
	public void _0_7_LoopInfo_100 (Disquuun disquuun) {
		WaitUntil(() => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var infos = new List<string>();
		disquuun.Info().Loop(
			(DisqueCommand command, DisquuunResult[] datas) => {
				var infoStr = DisquuunDeserializer.Info(datas).rawString;
				infos.Add(infoStr);
				if (infos.Count < 100) return true;
				return false;
			} 
		);
		
		WaitUntil(() => (infos.Count == 100), 5);		
	}
	
	
}