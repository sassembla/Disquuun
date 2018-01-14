using System;
using System.Collections.Generic;
using System.Linq;
using DisquuunCore;
using DisquuunCore.Deserialize;

/*
	basement api tests.
*/

public partial class Tests
{
    private byte[] data_10 = new byte[10];
    private byte[] data_100 = new byte[100];
    public void _0_0_InitWith2Connection(Disquuun disquuun)
    {
        WaitUntil("_0_0_InitWith2Connection", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
    }

    public void _0_0_1_WaitOnOpen2Connection(Disquuun disquuun)
    {
        var conId = string.Empty;
        var disquuun2 = new Disquuun(DisquuunTests.TestDisqueHostStr, DisquuunTests.TestDisquePortNum, 1, 1,
            connectionId =>
            {
                conId = connectionId;
            },
            (info, e) =>
            {

            }
        );
        WaitUntil("_0_0_1_WaitOnOpen2Connection", () => !string.IsNullOrEmpty(conId), 5);

        disquuun2.Disconnect();
    }

    public void _0_0_2_ReadmeSampleSync(Disquuun disquuun)
    {
        bool overed = false;
        disquuun = new Disquuun(DisquuunTests.TestDisqueHostStr, DisquuunTests.TestDisquePortNum, 1024, 1,
            disquuunId =>
            {
                var queueId = Guid.NewGuid().ToString();

                // addjob. add 10bytes job to Disque.
                disquuun.AddJob(queueId, data_10).DEPRICATED_Sync();

                // getjob. get job from Disque.
                var result = disquuun.GetJob(new string[] { queueId }).DEPRICATED_Sync();
                var jobDatas = DisquuunDeserializer.GetJob(result);

                Assert("_0_0_2_ReadmeSampleSync", 1, jobDatas.Length, "not match.");

                // fastack.
                var jobId = jobDatas[0].jobId;
                disquuun.FastAck(new string[] { jobId }).DEPRICATED_Sync();

                overed = true;
            }
        );

        WaitUntil("", () => overed, 5);

        disquuun.Disconnect();
    }

    public void _0_0_3_ReadmeSampleAsync(Disquuun disquuun)
    {
        int fastAckedJobCount = 0;

        disquuun = new Disquuun(DisquuunTests.TestDisqueHostStr, DisquuunTests.TestDisquePortNum, 1024, 3,
            disquuunId =>
            {
                var queueId = Guid.NewGuid().ToString();

                // addjob. add 10bytes job to Disque.
                disquuun.AddJob(queueId, data_10).Async(
                    (addJobCommand, addJobData) =>
                    {
                        // job added to queueId @ Disque.

                        // getjob. get job from Disque.
                        disquuun.GetJob(new string[] { queueId }).Async(
                            (getJobCommand, getJobData) =>
                            {
                                // got job from queueId @ Disque.

                                var jobDatas = DisquuunDeserializer.GetJob(getJobData);
                                Assert("_0_0_3_ReadmeSampleAsync", 1, jobDatas.Length, "not match.");

                                // get jobId from got job data.
                                var gotJobId = jobDatas[0].jobId;

                                // fastack it.
                                disquuun.FastAck(new string[] { gotJobId }).Async(
                                    (fastAckCommand, fastAckData) =>
                                    {
                                        // fastack succeded or not.

                                        fastAckedJobCount = DisquuunDeserializer.FastAck(fastAckData);
                                        Assert("_0_0_3_ReadmeSampleAsync", 1, fastAckedJobCount, "not match.");
                                    }
                                );
                            }
                        );
                    }
                );
            }
        );

        WaitUntil("_0_0_3_ReadmeSampleAsync", () => (fastAckedJobCount == 1), 5);

        disquuun.Disconnect();
    }

    public void _0_0_4_ReadmeSamplePipeline(Disquuun disquuun)
    {
        WaitUntil("_0_0_4_ReadmeSamplePipeline", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var fastacked = false;

        disquuun.Pipeline(
            disquuun.AddJob("my queue name", data_100),
            disquuun.GetJob(new string[] { "my queue name" })
        ).Execute(
            (command, data) =>
            {
                if (command != DisqueCommand.GETJOB) return;
                var jobs = DisquuunDeserializer.GetJob(data);

                var jobIds = jobs.Select(jobData => jobData.jobId).ToArray();
                var jobDatas = jobs.Select(jobData => jobData.jobData).ToList();

                /*
					fast ack all.
				*/
                disquuun.FastAck(jobIds).Async(
                    (command2, data2) =>
                    {
                        fastacked = true;
                    }
                );
            }
        );

        WaitUntil("_0_0_4_ReadmeSamplePipeline", () => fastacked, 5);
    }

    private object _0_1_ConnectedShouldCallOnceObject = new object();

    public void _0_1_ConnectedShouldCallOnce(Disquuun disquuun)
    {
        int connectedCount = 0;

        disquuun = new Disquuun(DisquuunTests.TestDisqueHostStr, DisquuunTests.TestDisquePortNum, 1024, 100,
            disquuunId =>
            {
                lock (_0_1_ConnectedShouldCallOnceObject)
                {
                    Assert("_0_1_ConnectedShouldCallOnce", 0, connectedCount, "not match.");
                    connectedCount++;
                }
            }
        );

        WaitUntil("_0_1_ConnectedShouldCallOnce", () => (connectedCount == 1), 5);

        disquuun.Disconnect();
    }

    private object _0_1_ConnectionFailedWithNoDisqueServerObject = new object();
    public void _0_1_ConnectionFailedWithNoDisqueServer(Disquuun disquuun)
    {
        WaitUntil("_0_1_ConnectionFailedWithNoDisqueServer", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        Exception e = null;

        var disquuun2 = new Disquuun(DisquuunTests.TestDisqueHostStr, DisquuunTests.TestDisqueDummyPortNum, 1024, 1,
            conId => { },
            (info, e2) =>
            {
                // set error to param,
                lock (_0_1_ConnectionFailedWithNoDisqueServerObject) e = e2;
                // TestLogger.Log("e:" + e);
            }
        );

        WaitUntil("_0_1_ConnectionFailedWithNoDisqueServer", () => (e != null), 1);
    }

    public void _0_2_SyncInfo(Disquuun disquuun)
    {
        WaitUntil("_0_2_SyncInfo", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var data = disquuun.Info().DEPRICATED_Sync();
        var infoStr = DisquuunDeserializer.Info(data).rawString;
        Assert("_0_2_SyncInfo", !string.IsNullOrEmpty(infoStr), "empty.");
    }

    public void _0_3_SyncInfoTwice(Disquuun disquuun)
    {
        WaitUntil("_0_3_SyncInfoTwice", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        {
            var datas = disquuun.Info().DEPRICATED_Sync();
            var infoStr = DisquuunDeserializer.Info(datas).rawString;
            Assert("_0_3_SyncInfoTwice", !string.IsNullOrEmpty(infoStr), "empty.");
        }

        {
            var datas = disquuun.Info().DEPRICATED_Sync();
            var infoStr = DisquuunDeserializer.Info(datas).rawString;
            Assert("_0_3_SyncInfoTwice", !string.IsNullOrEmpty(infoStr), "empty.");
        }
    }

    public void _0_4_AsyncInfo(Disquuun disquuun)
    {
        WaitUntil("_0_4_AsyncInfo", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var infoStr = string.Empty;
        disquuun.Info().Async(
            (DisqueCommand command, DisquuunResult[] datas) =>
            {
                infoStr = DisquuunDeserializer.Info(datas).rawString;
            }
        );

        WaitUntil("_0_4_AsyncInfo", () => !string.IsNullOrEmpty(infoStr), 5);
    }

    public void _0_5_LoopInfo_Once(Disquuun disquuun)
    {
        WaitUntil("_0_5_LoopInfo_Once", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var infoStr = string.Empty;
        disquuun.Info().Loop(
            (DisqueCommand command, DisquuunResult[] datas) =>
            {
                infoStr = DisquuunDeserializer.Info(datas).rawString;
                return false;
            }
        );

        WaitUntil("_0_5_LoopInfo_Once", () => !string.IsNullOrEmpty(infoStr), 5);
    }

    private object _0_6_LoopInfo_TwiceObject = new object();

    public void _0_6_LoopInfo_Twice(Disquuun disquuun)
    {
        WaitUntil("_0_6_LoopInfo_Twice", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var infos = new List<string>();
        disquuun.Info().Loop(
            (DisqueCommand command, DisquuunResult[] datas) =>
            {
                var infoStr = DisquuunDeserializer.Info(datas).rawString;
                lock (_0_6_LoopInfo_TwiceObject) infos.Add(infoStr);
                if (infos.Count < 2) return true;
                return false;
            }
        );

        WaitUntil("_0_6_LoopInfo_Twice", () => (infos.Count == 2), 5);
    }

    private object _0_7_LoopInfo_100Object = new object();

    public void _0_7_LoopInfo_100(Disquuun disquuun)
    {
        WaitUntil("_0_7_LoopInfo_100", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var infos = new List<string>();
        disquuun.Info().Loop(
            (DisqueCommand command, DisquuunResult[] datas) =>
            {
                var infoStr = DisquuunDeserializer.Info(datas).rawString;
                lock (_0_7_LoopInfo_100Object) infos.Add(infoStr);
                if (infos.Count < 100) return true;
                return false;
            }
        );

        WaitUntil("_0_7_LoopInfo_100", () => (infos.Count == 100), 5);
    }

    public void _0_8_Pipeline_Single(Disquuun disquuun)
    {
        WaitUntil("_0_8_Pipeline_Single", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var gotInfo = false;

        disquuun.Pipeline(disquuun.Info()).Execute(
            (command, data) =>
            {
                gotInfo = true;
            }
        );

        WaitUntil("_0_8_Pipeline_Single", () => gotInfo, 1);
    }

    private object _0_9_PipelineObject = new object();

    public void _0_9_Pipeline(Disquuun disquuun)
    {
        WaitUntil("_0_9_Pipeline", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var gotInfoCount = 0;

        disquuun.Pipeline(disquuun.Info(), disquuun.Info()).Execute(
            (command, data) =>
            {
                lock (_0_9_PipelineObject) gotInfoCount++;
            }
        );

        WaitUntil("_0_9_Pipeline", () => (gotInfoCount == 2), 1);
    }


}