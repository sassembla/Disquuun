using System;
using System.Collections.Generic;
using DisquuunCore;

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
				error = e;
			}
		);
		
		WaitUntil("_5_0_ConnectionFailed", () => (error != null), 5);
		disquuun2.Disconnect();
	}

	private object _5_1_ConnectionFailedMultipleLockObject = new object();

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
				lock (_5_1_ConnectionFailedMultipleLockObject) {
					errors.Add(e);
				}
			}
		);

		WaitUntil("_5_1_ConnectionFailedMultiple", () => (errors.Count == 5), 10);
		disquuun2.Disconnect();
	}

	/**
		接続数のロジックとしては、
		・基礎接続数まで最初に接続
		・以降は何があっても接続
		・切断が発生した場合は再度接続(ここがまだない)
		という感じか。

		切断をフックしたいね。

		OPENINGを定義したんだけど、切断中になっているやつを回復させる場合は、

		・OPENING
		・OPENED
		とかの定義ではなく、
		
		・生成中
		・生成完了
		・欠損回復中(ただし生成完了してるんで使える)
		とかなので、

		・OPENING
		・OPENED
		・OPENED_RECOVERING
		とかか。一度OPENEDになったら、それ以降は接続数ベースではOPENEDとOPENED_RECOVERINGを行き来するだけで済むな。
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