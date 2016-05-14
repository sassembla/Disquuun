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

    public void _0_1_ConnectionFailedWithNoDisqueServer (Disquuun disquuun) {
		Exception e = null;
		Action<Exception> Failed = (Exception e2) => {
			// set error to param,
			e = e2;
			// TestLogger.Log("e:" + e);
		};
		
		var disquuun2 = new Disquuun("127.0.0.1", 8888, 1024, 1, Failed);
		
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