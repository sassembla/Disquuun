using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

using DisquuunCore;
using DisquuunCore.Deserialize;

public class DisquuunTests
{
    public const string TestDisqueHostStr = "127.0.0.1";
    public const int TestDisquePortNum = 7711;
    public const int TestDisqueDummyPortNum = 7712;

    public static Tests tests;

    public static void Start()
    {
        tests = new Tests();
        tests.RunTests();
    }

    public static void Stop()
    {
        tests = null;
    }
}

public partial class Tests
{
    public void RunTests()
    {
        var tests = new List<Action<Disquuun>>();
        for (var i = 0; i < 1; i++)
        {
            // basement.
            tests.Add(_0_0_InitWith2Connection);

            tests.Add(_0_0_1_WaitOnOpen2Connection);
            tests.Add(_0_0_2_ReadmeSampleSync);
            tests.Add(_0_0_3_ReadmeSampleAsync);
            tests.Add(_0_0_4_ReadmeSamplePipeline);
            tests.Add(_0_1_ConnectedShouldCallOnce);
            tests.Add(_0_1_ConnectionFailedWithNoDisqueServer);
            tests.Add(_0_2_SyncInfo);
            tests.Add(_0_3_SyncInfoTwice);
            tests.Add(_0_4_AsyncInfo);
            tests.Add(_0_5_LoopInfo_Once);
            tests.Add(_0_6_LoopInfo_Twice);
            tests.Add(_0_7_LoopInfo_100);
            tests.Add(_0_8_Pipeline_Single);
            tests.Add(_0_9_Pipeline);

            // sync apis. DEPRECATED.
            tests.Add(_1_0_AddJob_Sync);
            tests.Add(_1_0_1_AddJob_Sync_TimeToLive);
            tests.Add(_1_0_2_AddJob_Sync_TimeToLive_Wait_Dead);
            tests.Add(_1_0_3_AddJob_Sync_Retry);
            tests.Add(_1_0_4_AddJob_Sync_Retry_0_And_TTL_1Sec);
            tests.Add(_1_1_GetJob_Sync);
            tests.Add(_1_1_1_GetJobWithCount_Sync);
            tests.Add(_1_1_2_GetJobFromMultiQueue_Sync);
            tests.Add(_1_1_3_GetJobWithNoHang_Sync);
            tests.Add(_1_2_AckJob_Sync);
            tests.Add(_1_3_Fastack_Sync);
            tests.Add(_1_1_4_GetJobWithCounters_Sync);
            // tests.Add(_1_4_Working_Sync);
            // tests.Add(_1_5_Nack_Sync);
            tests.Add(_1_6_Info_Sync);
            tests.Add(_1_7_Hello_Sync);
            tests.Add(_1_8_Qlen_Sync);
            tests.Add(_1_9_Qstat_Sync);
            // tests.Add(_1_10_Qpeek_Sync);
            // tests.Add(_1_11_Enqueue_Sync);
            // tests.Add(_1_12_Dequeue_Sync);
            // tests.Add(_1_13_DelJob_Sync);
            // tests.Add(_1_14_Show_Sync);
            // tests.Add(_1_15_Qscan_Sync);
            // tests.Add(_1_16_Jscan_Sync);
            // tests.Add(_1_17_Pause_Sync);

            // async apis.
            tests.Add(_2_0_AddJob_Async);
            tests.Add(_2_0_1_AddJob_Async_TimeToLive);
            tests.Add(_2_0_2_AddJob_Async_TimeToLive_Wait_Dead);
            tests.Add(_2_0_3_AddJob_Async_Retry);
            tests.Add(_2_0_4_AddJob_Async_Retry_0_And_TTL_1Sec);
            tests.Add(_2_1_GetJob_Async);
            tests.Add(_2_1_1_GetJobWithCount_Async);
            tests.Add(_2_1_2_GetJobFromMultiQueue_Async);
            tests.Add(_2_1_3_GetJobWithNoHang_Async);
            tests.Add(_2_1_4_GetJobWithCounters_Async);
            tests.Add(_2_2_AckJob_Async);
            tests.Add(_2_3_Fastack_Async);
            // tests.Add(_2_4_Working_Async);
            // tests.Add(_2_5_Nack_Async);
            tests.Add(_2_6_Info_Async);
            tests.Add(_2_7_Hello_Async);
            tests.Add(_2_8_Qlen_Async);
            tests.Add(_2_9_Qstat_Async);
            // tests.Add(_2_10_Qpeek_Async);
            // tests.Add(_2_11_Enqueue_Async);
            // tests.Add(_2_12_Dequeue_Async);
            // tests.Add(_2_13_DelJob_Async);
            // tests.Add(_2_14_Show_Async);
            // tests.Add(_2_15_Qscan_Async);
            // tests.Add(_2_16_Jscan_Async);
            // tests.Add(_2_17_Pause_Async);

            // multiSocket.
            tests.Add(_3_0_Nested2AsyncSocket);
            tests.Add(_3_1_NestedMultipleAsyncSocket);

            // buffer over.
            tests.Add(_4_0_ByfferOverWithSingleSyncGetJob_Sync);
            tests.Add(_4_1_ByfferOverWithMultipleSyncGetJob_Sync);
            tests.Add(_4_2_ByfferOverWithSokcetOverSyncGetJob_Sync);
            tests.Add(_4_3_ByfferOverWithSingleSyncGetJob_Async);
            tests.Add(_4_4_ByfferOverWithMultipleSyncGetJob_Async);
            tests.Add(_4_5_ByfferOverWithSokcetOverSyncGetJob_Async);

            // error handling.
            tests.Add(_5_0_ConnectionFailed);
            tests.Add(_5_1_ConnectionFailedMultiple);

            // adding async request over busy-socket num.
            tests.Add(_6_0_ExceededSocketNo3In2);
            tests.Add(_6_1_ExceededSocketNo100In2);
            tests.Add(_6_2_ExceededSocketShouldStacked);

            // benchmarks.
            tests.Add(_7_0_AddJob1000);
            tests.Add(_7_0_0_AddJob1000by100Connection);
            tests.Add(_7_0_1_AddJob1000byPipeline);
            tests.Add(_7_0_2_AddJob1000byPipelines);
            tests.Add(_7_1_GetJob1000);
            tests.Add(_7_1_0_GetJob1000by100Connection);
            tests.Add(_7_1_1_GetJob1000byPipeline);
            tests.Add(_7_1_2_GetJob1000byPipelines);
            tests.Add(_7_2_GetJob1000byLoop);
            // tests.Add(_7_2_0_GetJob1000byPipeline);// unexecutable.

            // data size bounding case.
            tests.Add(_8_0_LargeSizeSendThenSmallSizeSendMakeEmitOnSendAfterOnReceived);
            tests.Add(_8_1_LargeSizeSendThenSmallSizeSendLoopMakeEmitOnSendAfterOnReceived);

            // pipeline
            tests.Add(_0_9_0_PipelineCommands);
            tests.Add(_0_9_1_MultiplePipeline);
            tests.Add(_0_9_2_MultipleCommandPipeline);
            tests.Add(_0_9_3_SomeCommandPipeline);
            tests.Add(_0_9_4_MassiveCommandPipeline);
            tests.Add(_0_9_5_Pipelines);

        }

        try
        {
            var disquuunForResultInfo = new Disquuun(DisquuunTests.TestDisqueHostStr, DisquuunTests.TestDisquePortNum, 10240, 1);
            WaitUntil(
                "_internal",
                () => disquuunForResultInfo.State() == Disquuun.ConnectionState.OPENED,
                5
            );

            var i = 0;
            foreach (var test in tests)
            {
                var methodName = i.ToString() + "_" + test.Method.Name;

                i++;

                try
                {
                    var disquuun = new Disquuun(
                        DisquuunTests.TestDisqueHostStr,
                        DisquuunTests.TestDisquePortNum,
                        2020008,
                        20
                    );// this buffer size is just for 100byte job x 10000 then receive 1 GetJob(count 1000).

                    // すべての接続ができるまで待つ
                    WaitUntil(
                        "_internal",
                        () => disquuun.State() == Disquuun.ConnectionState.OPENED,
                        5
                    );

                    test(disquuun);

                    if (disquuun != null)
                    {
                        disquuun.Disconnect();
                        disquuun = null;
                    }

                    var info = disquuunForResultInfo.Info().DEPRICATED_Sync();
                    var result = DisquuunDeserializer.Info(info);
                    var restJobCount = result.jobs.registered_jobs;

                    if (restJobCount != 0)
                    {
                        TestLogger.Log("test:" + methodName + " rest job:" + restJobCount, true);
                    }
                    else
                    {
                        TestLogger.Log("test:" + methodName + " passed.", true);
                    }
                }
                catch (Exception e)
                {
                    TestLogger.Log("test:" + methodName + " FAILED by exception:" + e, true);
                }
            }

            disquuunForResultInfo.Disconnect();
            TestLogger.Log("tests end.", true);
        }
        catch (Exception e)
        {
            TestLogger.Log("tests failed:" + e, true);
        }
    }

    public bool Wait(string methodName, int timeoutSec)
    {
        var resetEvent = new ManualResetEvent(false);
        var waitingThread = new Thread(
            () =>
            {
                resetEvent.Reset();
                var startTime = DateTime.Now;

                try
                {
                    while (true)
                    {
                        var current = DateTime.Now;
                        var distanceSeconds = (current - startTime).Seconds;

                        if (timeoutSec < distanceSeconds)
                        {
                            break;
                        }

                        System.Threading.Thread.Sleep(10);
                    }
                }
                catch (Exception e)
                {
                    TestLogger.Log("methodName:" + methodName + " error:" + e.Message, true);
                }

                resetEvent.Set();
            }
        );

        waitingThread.Start();

        resetEvent.WaitOne();
        return true;
    }

    public bool WaitUntil(string message, Func<bool> WaitFor, int timeoutSec, [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
    {
        var resetEvent = new ManualResetEvent(false);
        var succeeded = true;
        var waitingThread = new Thread(
            () =>
            {
                resetEvent.Reset();
                var startTime = DateTime.Now;

                try
                {
                    while (!WaitFor())
                    {
                        var current = DateTime.Now;
                        var distanceSeconds = (current - startTime).Seconds;

                        if (timeoutSec < distanceSeconds)
                        {
                            TestLogger.Log("timeout:" + methodName + " message:" + message + " time limit sec:" + timeoutSec + " is overed.", true);
                            succeeded = false;
                            break;
                        }

                        System.Threading.Thread.Sleep(10);
                    }
                }
                catch (Exception e)
                {
                    TestLogger.Log("methodName:" + methodName + " error:" + e.Message, true);
                }

                resetEvent.Set();
            }
        );

        waitingThread.Start();

        resetEvent.WaitOne();
        return succeeded;
    }

    public void Assert(string methodName, bool condition, string message)
    {
        if (!condition) TestLogger.Log("test:" + methodName + " FAILED:" + message);
    }


    public void Assert(string methodName, object expected, object actual, string message)
    {
        if (expected.ToString() != actual.ToString()) TestLogger.Log("test:" + methodName + " FAILED:" + message + " expected:" + expected + " actual:" + actual);
    }
}



public static class TestLogger
{
    private static object lockObject = new object();

    public static string logPath;
    public static StringBuilder logs = new StringBuilder();
    public static void Log(string message, bool export = false)
    {
        lock (lockObject)
        {
            if (!export)
            {
                logs.AppendLine(message);
                return;
            }

            logPath = "test.log";

            // file write
            using (var fs = new FileStream(
                logPath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite)
            )
            {
                using (var sr = new StreamWriter(fs))
                {
                    if (0 < logs.Length)
                    {
                        sr.WriteLine(logs.ToString());
                        logs = new StringBuilder();
                    }
                    sr.WriteLine("log:" + message);
                }
            }
        }
    }
}
