using System;
using DisquuunCore;

/*
	slot over tests.
*/

public partial class Tests {
	public void _6_0_ExceededSocketNo3In2 (Disquuun disquuun) {
		WaitUntil("_6_0_ExceededSocketNo3In2", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		var infoCount = 0;
		
		for (var i = 0; i < 3; i++) {
			disquuun.Info().Async(
				(command, data) => {
					infoCount++;
				}
			);
		}
		
		WaitUntil("_6_0_ExceededSocketNo3In2", () => (infoCount == 3), 5);
	}
	
	private object _6_1_ExceededSocketNo100In2LockObject = new object();
	public void _6_1_ExceededSocketNo100In2 (Disquuun disquuun) {
		WaitUntil("_6_1_ExceededSocketNo100In2", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		var infoCount = 0;
		
		var connectCount = 1000;
		
		for (var i = 0; i < connectCount; i++) {
			disquuun.Info().Async(
				(command, data) => {
					lock (_6_1_ExceededSocketNo100In2LockObject) infoCount++;
				}
			);
		}
		
		WaitUntil("_6_1_ExceededSocketNo100In2", () => (infoCount == connectCount), 5);
	}

	private object _6_2_ExceededSocketShouldStackedLockObject = new object();

	public void _6_2_ExceededSocketShouldStacked (Disquuun disquuun) {
		WaitUntil("_6_2_ExceededSocketShouldStacked", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
		
		var queueId = Guid.NewGuid().ToString();
		var infoCount = 0;
		
		var connectCount = 1000;

		for (var i = 0; i < connectCount; i++) {
			disquuun.Info().Async(
				(command, data) => {
					lock (_6_2_ExceededSocketShouldStackedLockObject) infoCount++;
				}
			);
		}

		var stackedCommandCount = disquuun.StackedCommandCount();

		Assert("_6_2_ExceededSocketShouldStacked", 0 < stackedCommandCount, "not match, " + stackedCommandCount);
		WaitUntil("_6_2_ExceededSocketShouldStacked", () => (infoCount == connectCount), 5);
	}
}