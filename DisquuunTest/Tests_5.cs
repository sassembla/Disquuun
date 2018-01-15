using System;
using System.Collections.Generic;
using DisquuunCore;

/*
	errors.
*/

public partial class Tests
{
    public long _5_0_ConnectionFailed(Disquuun disquuun)
    {
        WaitUntil("_5_0_ConnectionFailed", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        Exception error = null;
        var disquuun2 = new Disquuun(
            DisquuunTests.TestDisqueHostStr,
            DisquuunTests.TestDisqueDummyPortNum,// fake port number. 
            1,
            1,
            (conId) => { },
            (info, e) =>
            {
                error = e;
            },
            currentSocketCount =>
            {

                return false;
            }
        );

        WaitUntil("_5_0_ConnectionFailed", () => (error != null), 5);
        disquuun2.Disconnect();
        return 0;
    }

    private object _5_1_ConnectionFailedMultipleLockObject = new object();

    public long _5_1_ConnectionFailedMultiple(Disquuun disquuun)
    {
        WaitUntil("_5_1_ConnectionFailedMultiple", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        List<Exception> errors = new List<Exception>();

        var disquuun2 = new Disquuun(
            DisquuunTests.TestDisqueHostStr,
            DisquuunTests.TestDisqueDummyPortNum,// fake port number. 
            1,
            5,
            (conId) => { },
            (info, e) =>
            {
                lock (_5_1_ConnectionFailedMultipleLockObject)
                {
                    errors.Add(e);
                }
            },
            currentSocketCount =>
            {

                return false;
            }
        );

        WaitUntil("_5_1_ConnectionFailedMultiple", () => (errors.Count == 5), 10);
        disquuun2.Disconnect();
        return 0;
    }
}