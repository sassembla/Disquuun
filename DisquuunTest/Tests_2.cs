using System;
using System.Diagnostics;
using System.Linq;
using DisquuunCore;
using DisquuunCore.Deserialize;

/*
	api async tests.
*/

public partial class Tests
{
    public long _2_0_AddJob_Async(Disquuun disquuun)
    {
        WaitUntil("_2_0_AddJob_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var queueId = Guid.NewGuid().ToString();
        var w = new Stopwatch();
        w.Start();


        var jobId = string.Empty;
        disquuun.AddJob(queueId, data_10).Async(
            (command, result) =>
            {
                jobId = DisquuunDeserializer.AddJob(result);
                w.Stop();
            }
        );

        WaitUntil("_2_0_AddJob_Async", () => !string.IsNullOrEmpty(jobId), 5);

        // ack in.
        disquuun.FastAck(new string[] { jobId }).DEPRICATED_Sync();

        return w.ElapsedMilliseconds;
    }

    public long _2_0_1_AddJob_Async_TimeToLive(Disquuun disquuun)
    {
        WaitUntil("_2_0_1_AddJob_Async_TimeToLive", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var queueId = Guid.NewGuid().ToString();

        var jobId = string.Empty;


        var w = new Stopwatch();
        w.Start();

        disquuun.AddJob(queueId, data_10, 0, "TTL", 100).Async(
            (command, result) =>
            {
                jobId = DisquuunDeserializer.AddJob(result);
                w.Stop();
            }
        );

        WaitUntil("_2_0_1_AddJob_Async_TimeToLive", () => !string.IsNullOrEmpty(jobId), 5);

        // ack in.
        disquuun.FastAck(new string[] { jobId }).DEPRICATED_Sync();

        return w.ElapsedMilliseconds;
    }

    public long _2_0_2_AddJob_Async_TimeToLive_Wait_Dead(Disquuun disquuun)
    {
        WaitUntil("_2_0_2_AddJob_Async_TimeToLive_Wait_Dead", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var queueId = Guid.NewGuid().ToString();

        var jobId = string.Empty;
        disquuun.AddJob(queueId, data_10, 0, "TTL", 1).Async(
            (command, result) =>
            {
                jobId = DisquuunDeserializer.AddJob(result);
            }
        );

        WaitUntil("_2_0_2_AddJob_Async_TimeToLive_Wait_Dead", () => !string.IsNullOrEmpty(jobId), 5);

        // wait 2 sec.
        Wait("_2_0_2_AddJob_Async_TimeToLive_Wait_Dead", 2);

        // get queue len.
        var len = DisquuunDeserializer.Qlen(disquuun.Qlen(queueId).DEPRICATED_Sync());
        Assert("_2_0_2_AddJob_Async_TimeToLive_Wait_Dead", len == 0, "not match, len:" + len);

        return 0;
    }

    public long _2_0_3_AddJob_Async_Retry(Disquuun disquuun)
    {
        WaitUntil("_2_0_3_AddJob_Async_Retry", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var queueId = Guid.NewGuid().ToString();

        var jobId = string.Empty;
        disquuun.AddJob(queueId, data_10, 0, "RETRY", 1).Async(
            (command, result) =>
            {
                jobId = DisquuunDeserializer.AddJob(result);
            }
        );

        WaitUntil("_2_0_3_AddJob_Async_Retry", () => !string.IsNullOrEmpty(jobId), 5);

        var len0 = DisquuunDeserializer.Qlen(disquuun.Qlen(queueId).DEPRICATED_Sync());
        // len0 is 1. job is added.

        // getjob (but not ack it.)
        DisquuunDeserializer.GetJob(disquuun.GetJob(new string[] { queueId }).DEPRICATED_Sync());

        var len1 = DisquuunDeserializer.Qlen(disquuun.Qlen(queueId).DEPRICATED_Sync());
        // len1 is 0. job is deleted from queue.

        // wait 2 sec.
        Wait("_2_0_3_AddJob_Async_Retry", 2);

        var len2 = DisquuunDeserializer.Qlen(disquuun.Qlen(queueId).DEPRICATED_Sync());
        Assert("_2_0_3_AddJob_Async_Retry", len2 == 1, "not match, len2:" + len2);

        disquuun.FastAck(new string[] { jobId }).DEPRICATED_Sync();
        return 0;
    }

    public long _2_0_4_AddJob_Async_Retry_0_And_TTL_1Sec(Disquuun disquuun)
    {
        WaitUntil("_2_0_4_AddJob_Async_Retry_0_And_TTL_1Sec", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var queueId = Guid.NewGuid().ToString();

        var jobId = string.Empty;
        disquuun.AddJob(queueId, data_10, 0, "RETRY", 0, "TTL", 1).Async(
            (command, result) =>
            {
                jobId = DisquuunDeserializer.AddJob(result);
            }
        );

        WaitUntil("_2_0_4_AddJob_Async_Retry_0_And_TTL_1Sec", () => !string.IsNullOrEmpty(jobId), 5);

        DisquuunDeserializer.Qlen(disquuun.Qlen(queueId).DEPRICATED_Sync());

        // getjob (but not ack it.)
        DisquuunDeserializer.GetJob(disquuun.GetJob(new string[] { queueId }).DEPRICATED_Sync());

        // once, queue len should be zero by getJob.
        var len1 = DisquuunDeserializer.Qlen(disquuun.Qlen(queueId).DEPRICATED_Sync());
        Assert("_2_0_4_AddJob_Async_Retry_0_And_TTL_1Sec", len1 == 0, "not match, len1:" + len1);

        // wait 2 sec.
        Wait("_2_0_4_AddJob_Async_Retry_0_And_TTL_1Sec", 2);

        // both qlen and info returns rest job == 0.

        var len2 = DisquuunDeserializer.Qlen(disquuun.Qlen(queueId).DEPRICATED_Sync());
        Assert("_2_0_4_AddJob_Async_Retry_0_And_TTL_1Sec", len2 == 0, "not match, len2:" + len2);

        // and rest job is 0.
        var info = DisquuunDeserializer.Info(disquuun.Info().DEPRICATED_Sync());
        Assert("_2_0_4_AddJob_Async_Retry_0_And_TTL_1Sec", info.jobs.registered_jobs == 0, "not match.");
        return 0;
    }

    public long _2_1_GetJob_Async(Disquuun disquuun)
    {
        WaitUntil("_2_1_GetJob_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var queueId = Guid.NewGuid().ToString();

        disquuun.AddJob(queueId, data_10).DEPRICATED_Sync();

        DisquuunDeserializer.JobData[] jobDatas = new DisquuunDeserializer.JobData[] { };
        var w = new Stopwatch();
        w.Start();


        disquuun.GetJob(new string[] { queueId }).Async(
            (command, result) =>
            {
                jobDatas = DisquuunDeserializer.GetJob(result);
                w.Stop();
            }
        );

        WaitUntil("_2_1_GetJob_Async", () => (jobDatas.Length == 1), 5);

        // ack in.
        var jobId = jobDatas[0].jobId;
        disquuun.FastAck(new string[] { jobId }).DEPRICATED_Sync();

        return w.ElapsedMilliseconds;
    }

    public long _2_1_1_GetJobWithCount_Async(Disquuun disquuun)
    {
        WaitUntil("_2_1_1_GetJobWithCount_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var queueId = Guid.NewGuid().ToString();

        var addJobCount = 10000;
        for (var i = 0; i < addJobCount; i++)
        {
            disquuun.AddJob(queueId, data_100).DEPRICATED_Sync();
        }
        var w = new Stopwatch();
        w.Start();


        DisquuunDeserializer.JobData[] jobDatas = new DisquuunDeserializer.JobData[] { };
        disquuun.GetJob(new string[] { queueId }, "COUNT", addJobCount).Async(
            (command, result) =>
            {
                jobDatas = DisquuunDeserializer.GetJob(result);
                w.Stop();
            }
        );

        WaitUntil("_2_1_1_GetJobWithCount_Async", () => (jobDatas.Length == addJobCount), 5);

        // ack in.
        var jobIds = jobDatas.Select(job => job.jobId).ToArray();
        disquuun.FastAck(jobIds).DEPRICATED_Sync();

        return w.ElapsedMilliseconds;
    }

    public long _2_1_2_GetJobFromMultiQueue_Async(Disquuun disquuun)
    {
        WaitUntil("_2_1_2_GetJobFromMultiQueue_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var queueId1 = Guid.NewGuid().ToString();
        disquuun.AddJob(queueId1, data_10).DEPRICATED_Sync();

        var queueId2 = Guid.NewGuid().ToString();
        disquuun.AddJob(queueId2, data_10).DEPRICATED_Sync();

        var w = new Stopwatch();
        w.Start();


        DisquuunDeserializer.JobData[] jobDatas = new DisquuunDeserializer.JobData[] { };
        disquuun.GetJob(new string[] { queueId1, queueId2 }, "count", 2).Async(
            (command, result) =>
            {
                jobDatas = DisquuunDeserializer.GetJob(result);
                w.Stop();
            }
        );

        WaitUntil("_2_1_2_GetJobFromMultiQueue_Async", () => (jobDatas.Length == 2), 5);

        // ack in.
        var jobIds = jobDatas.Select(job => job.jobId).ToArray();
        disquuun.FastAck(jobIds).DEPRICATED_Sync();

        return w.ElapsedMilliseconds;
    }

    public long _2_1_3_GetJobWithNoHang_Async(Disquuun disquuun)
    {
        WaitUntil("_2_1_3_GetJobWithNoHang_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var queueId = Guid.NewGuid().ToString();

        bool received = false;

        var w = new Stopwatch();
        w.Start();


        disquuun.GetJob(new string[] { queueId }, "NOHANG").Async(
            (command, result) =>
            {
                var jobDatas = DisquuunDeserializer.GetJob(result);
                Assert("_2_1_3_GetJobWithNoHang_Async", jobDatas.Length == 0, "not match.");
                received = true;
                w.Stop();
            }
        );

        WaitUntil("_2_1_3_GetJobWithNoHang_Async", () => received, 5);

        return w.ElapsedMilliseconds;
    }

    public long _2_1_4_GetJobWithCounters_Async(Disquuun disquuun)
    {
        WaitUntil("_2_1_4_GetJobWithCounters_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var queueId = Guid.NewGuid().ToString();

        disquuun.AddJob(queueId, data_100).DEPRICATED_Sync();

        var ackCount = -1;

        var w = new Stopwatch();
        w.Start();


        disquuun.GetJob(new string[] { queueId }, "withcounters").Async(
            (command, result) =>
            {
                var jobDatas = DisquuunDeserializer.GetJob(result);
                ackCount = jobDatas[0].additionalDeliveriesCount;
                w.Stop();
                disquuun.FastAck(new string[] { jobDatas[0].jobId }).Async(
                    (c, d) => { }
                );
            }
        );

        WaitUntil("_2_1_4_GetJobWithCounters_Async", () => (ackCount == 0), 5);
        return w.ElapsedMilliseconds;
    }

    public long _2_2_AckJob_Async(Disquuun disquuun)
    {
        WaitUntil("_2_2_AckJob_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var queueId = Guid.NewGuid().ToString();
        var jobId = DisquuunDeserializer.AddJob(
            disquuun.AddJob(queueId, data_10).DEPRICATED_Sync()
        );

        var w = new Stopwatch();
        w.Start();

        var ackCount = 0;
        disquuun.AckJob(new string[] { jobId }).Async(
            (command, result) =>
            {
                ackCount = DisquuunDeserializer.AckJob(result);
                w.Stop();
            }
        );

        WaitUntil("_2_2_AckJob_Async", () => (ackCount == 1), 5);

        return w.ElapsedMilliseconds;
    }

    public long _2_3_Fastack_Async(Disquuun disquuun)
    {
        WaitUntil("_2_3_Fastack_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var queueId = Guid.NewGuid().ToString();
        var jobId = DisquuunDeserializer.AddJob(
            disquuun.AddJob(queueId, data_10).DEPRICATED_Sync()
        );
        var w = new Stopwatch();
        w.Start();


        var ackCount = 0;
        disquuun.FastAck(new string[] { jobId }).Async(
            (command, result) =>
            {
                ackCount = DisquuunDeserializer.FastAck(result);
                w.Stop();
            }
        );

        WaitUntil("_2_3_Fastack_Async", () => (ackCount == 1), 5);

        return w.ElapsedMilliseconds;
    }

    public long _2_4_Working_Async(Disquuun disquuun)
    {
        DisquuunLogger.Log("_1_4_Working_Async not yet applied");
        // WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var queueId = Guid.NewGuid().ToString();
        // var jobId = DisquuunDeserializer.AddJob(
        // 	disquuun.AddJob(queueId, data_10).DEPRICATED_Sync()
        // );

        // var workResult = disquuun.Working(jobId).DEPRICATED_Sync();
        // var workingResult = DisquuunDeserializer.Working(workResult);

        // var result = disquuun.FastAck(new string[]{jobId}).DEPRICATED_Sync();
        // var ackCount = DisquuunDeserializer.FastAck(result);
        // Assert("", 1, ackCount, "not match.");
        return 0;
    }

    public long _2_5_Nack_Async(Disquuun disquuun)
    {
        DisquuunLogger.Log("_1_5_Nack_Async not yet applied");
        // WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var queueId = Guid.NewGuid().ToString();
        // var jobId = DisquuunDeserializer.AddJob(
        // 	disquuun.AddJob(queueId, data_10).DEPRICATED_Sync()
        // );

        // var result = disquuun.FastAck(new string[]{jobId}).DEPRICATED_Sync();
        // var ackCount = DisquuunDeserializer.FastAck(result);
        // Assert("", 1, ackCount, "not match.");
        return 0;
    }

    public long _2_6_Info_Async(Disquuun disquuun)
    {
        WaitUntil("_2_6_Info_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var registered_jobs = 0;
        var w = new Stopwatch();
        w.Start();


        disquuun.Info().Async(
            (c, data) =>
            {
                var infoResult = DisquuunDeserializer.Info(data);
                registered_jobs = infoResult.jobs.registered_jobs;
                w.Stop();
            }
        );

        WaitUntil("_2_6_Info_Async", () => (registered_jobs == 0), 5);

        return w.ElapsedMilliseconds;
    }

    public long _2_7_Hello_Async(Disquuun disquuun)
    {
        WaitUntil("_2_7_Hello_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var version = string.Empty;
        var w = new Stopwatch();
        w.Start();


        disquuun.Hello().Async(
            (c, data) =>
            {
                var helloResult = DisquuunDeserializer.Hello(data);
                version = helloResult.version;
                w.Stop();
            }
        );

        WaitUntil("_2_7_Hello_Async", () => (version == "1"), 5);

        return w.ElapsedMilliseconds;
    }

    public long _2_8_Qlen_Async(Disquuun disquuun)
    {
        WaitUntil("_2_8_Qlen_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var queueId = Guid.NewGuid().ToString();
        var jobId = DisquuunDeserializer.AddJob(
            disquuun.AddJob(queueId, data_10).DEPRICATED_Sync()
        );

        var w = new Stopwatch();
        w.Start();


        var qlen = 0;
        disquuun.Qlen(queueId).Async(
            (c, data) =>
            {
                qlen = DisquuunDeserializer.Qlen(data);
                w.Stop();
            }
        );
        WaitUntil("_2_8_Qlen_Async", () => (qlen == 1), 5);

        var result = disquuun.FastAck(new string[] { jobId }).DEPRICATED_Sync();
        var ackCount = DisquuunDeserializer.FastAck(result);

        return w.ElapsedMilliseconds;
    }

    public long _2_9_Qstat_Async(Disquuun disquuun)
    {
        WaitUntil("_2_9_Qstat_Async", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var queueId = Guid.NewGuid().ToString();
        var jobId = DisquuunDeserializer.AddJob(disquuun.AddJob(queueId, data_10).DEPRICATED_Sync());
        var w = new Stopwatch();
        w.Start();


        var qstatLen = 0;
        disquuun.Qstat(queueId).Async(
            (command, data) =>
            {
                qstatLen = DisquuunDeserializer.Qstat(data).len;
                w.Stop();
            }
        );

        WaitUntil("_2_9_Qstat_Async", () => (qstatLen == 1), 5);

        disquuun.FastAck(new string[] { jobId }).DEPRICATED_Sync();

        return w.ElapsedMilliseconds;
    }


    public long _2_10_Qpeek_Async(Disquuun disquuun)
    {
        DisquuunLogger.Log("_2_10_Qpeek_Async not yet applied");
        // <queue-name> <count>

        // WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var infoData = disquuun.Info().DEPRICATED_Sync();
        // var infoResult = DisquuunDeserializer.Info(infoData);

        // Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
        return 0;
    }

    public long _2_11_Enqueue_Async(Disquuun disquuun)
    {
        DisquuunLogger.Log("_2_11_Enqueue_Async not yet applied");
        // <job-id> ... <job-id>

        // WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var infoData = disquuun.Info().DEPRICATED_Sync();
        // var infoResult = DisquuunDeserializer.Info(infoData);

        // Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
        return 0;
    }

    public long _2_12_Dequeue_Async(Disquuun disquuun)
    {
        DisquuunLogger.Log("_2_12_Dequeue_Async not yet applied");
        // <job-id> ... <job-id>

        // WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var infoData = disquuun.Info().DEPRICATED_Sync();
        // var infoResult = DisquuunDeserializer.Info(infoData);

        // Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
        return 0;
    }

    public long _2_13_DelJob_Async(Disquuun disquuun)
    {
        DisquuunLogger.Log("_2_13_DelJob_Async not yet applied");
        // <job-id> ... <job-id>

        // WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var infoData = disquuun.Info().DEPRICATED_Sync();
        // var infoResult = DisquuunDeserializer.Info(infoData);

        // Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
        return 0;
    }

    public long _2_14_Show_Async(Disquuun disquuun)
    {
        DisquuunLogger.Log("_2_14_Show_Async not yet applied");
        // <job-id>

        // WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var infoData = disquuun.Info().DEPRICATED_Sync();
        // var infoResult = DisquuunDeserializer.Info(infoData);

        // Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
        return 0;
    }

    public long _2_15_Qscan_Async(Disquuun disquuun)
    {
        DisquuunLogger.Log("_2_15_Qscan_Async not yet applied");
        // [COUNT <count>] [BUSYLOOP] [MINLEN <len>] [MAXLEN <len>] [IMPORTRATE <rate>]

        // WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var infoData = disquuun.Info().DEPRICATED_Sync();
        // var infoResult = DisquuunDeserializer.Info(infoData);

        // Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
        return 0;
    }

    public long _2_16_Jscan_Async(Disquuun disquuun)
    {
        DisquuunLogger.Log("_2_16_Jscan_Async not yet applied");
        // [<cursor>] [COUNT <count>] [BUSYLOOP] [QUEUE <queue>] [STATE <state1> STATE <state2> ... STATE <stateN>] [REPLY all|id]

        // WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var infoData = disquuun.Info().DEPRICATED_Sync();
        // var infoResult = DisquuunDeserializer.Info(infoData);

        // Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
        return 0;
    }

    public long _2_17_Pause_Async(Disquuun disquuun)
    {
        DisquuunLogger.Log("_2_17_Pause_Async not yet applied");
        // <queue-name> option1 [option2 ... optionN]

        // WaitUntil("", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var pauseData = disquuun.Pause().DEPRICATED_Sync();
        // var pauseResult = DisquuunDeserializer.Info(pauseData);

        // Assert("", 0, pauseResult.jobs.registered_jobs, "not match.");
        return 0;
    }
}
