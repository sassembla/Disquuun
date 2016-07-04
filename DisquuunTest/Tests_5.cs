using System;
using System.Collections.Generic;
using System.Linq;
using DisquuunCore;
using DisquuunCore.Deserialize;

/*
	errors.
*/

public partial class Tests {
	public void _5_0_ConnectionFailed (Disquuun disquuun) {
		WaitUntil("_5_0_ConnectionFailed", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

		Exception error = null;
		var disquuun2 = new Disquuun(
			DisquuunTests.TestDisqueHostStr, 
			DisquuunTests.TestDisqueDummyPortNum,// fake port number. 
			1, 
			1, 
			(conId) => {}, 
			(info, e) => {
				// TestLogger.Log("failed! e:" + e);
				error = e;
			}
		);
		
		WaitUntil("_5_0_ConnectionFailed", () => (error != null), 5);
		disquuun2.Disconnect(true);
	}
	public void _5_1_ConnectionFailedMultiple (Disquuun disquuun) {
		WaitUntil("_5_1_ConnectionFailedMultiple", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

		List<Exception> errors = new List<Exception>();
		var disquuun2 = new Disquuun(
			DisquuunTests.TestDisqueHostStr, 
			DisquuunTests.TestDisqueDummyPortNum,// fake port number. 
			1, 
			5,
			(conId) => {},
			(info, e) => {
				lock (this) {
					TestLogger.Log("failed! info:"+ info + " e:" + e, true);
					errors.Add(e);
				}
			}
		);

		WaitUntil("_5_1_ConnectionFailedMultiple", () => (errors.Count == 5), 10);
		disquuun2.Disconnect(true);
	}

	/**
		接続数のロジックとしては、
		・基礎接続数まで最初に接続
		・追加接続数上限(これを超えたら実行できなくなる、、、んで、やっぱりできない)はセットしたくない
		・DISPOSABLEの切断をやめる(接続したあと切断せずスロットを増やす)っていう感じで、スロットを増やすか。DISPOSABLEいらんよな、、
		
		っていう戦略をとると、
		・基礎接続数をオーバーした場合は警告が出る
		っていうだけでいいかな。
	*/
	// public void _5_2_追加接続でのエラーを出そう。 (Disquuun disquuun) {
	// 	WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
	// }

	// public void _5_3_接続数上限を超えそうな時に、、っていうのが良いのかな〜〜デザインの問題だな、、 (Disquuun disquuun) {
	// 	WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
	// }

	// public void _5_4_ConnectionClosedWhileReceiving (Disquuun disquuun) {
	// 	WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
	// }

	// public void _5_5_ConnectionClosedWhileReceiving (Disquuun disquuun) {
	// 	WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
	// }

	
}