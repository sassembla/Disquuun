using System;
using System.Diagnostics;
using System.Linq;
using DisquuunCore;
using DisquuunCore.Deserialize;

/*
	api sync tests.
*/

public partial class Tests
{
    // all sync apis are deprecated.

    public long _1_0_AddJob_Sync(Disquuun disquuun)
    {
        WaitUntil("_1_0_AddJob_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var w = new Stopwatch();
        w.Start();


        var queueId = Guid.NewGuid().ToString();
        var result = disquuun.AddJob(queueId, data_10).DEPRICATED_Sync();
        var jobId = DisquuunDeserializer.AddJob(result);
        Assert("_1_0_AddJob_Sync", !string.IsNullOrEmpty(jobId), "empty.");

        // ack in.
        disquuun.FastAck(new string[] { jobId }).DEPRICATED_Sync();
        w.Stop();
        return w.ElapsedMilliseconds;
    }

    public long _1_0_1_AddJob_Sync_TimeToLive(Disquuun disquuun)
    {
        WaitUntil("_1_0_1_AddJob_Sync_TimeToLive", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var w = new Stopwatch();
        w.Start();


        var queueId = Guid.NewGuid().ToString();
        var result = disquuun.AddJob(queueId, data_10, 0, "TTL", 100).DEPRICATED_Sync();
        var jobId = DisquuunDeserializer.AddJob(result);
        Assert("_1_0_1_AddJob_Sync_TimeToLive", !string.IsNullOrEmpty(jobId), "empty.");

        // ack in.
        disquuun.FastAck(new string[] { jobId }).DEPRICATED_Sync();
        w.Stop();
        return w.ElapsedMilliseconds;
    }

    public long _1_0_2_AddJob_Sync_TimeToLive_Wait_Dead(Disquuun disquuun)
    {
        WaitUntil("_1_0_2_AddJob_Sync_TimeToLive_Wait_Dead", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var w = new Stopwatch();
        w.Start();


        var queueId = Guid.NewGuid().ToString();

        var result = disquuun.AddJob(queueId, data_10, 0, "TTL", 1).DEPRICATED_Sync();
        var jobId = DisquuunDeserializer.AddJob(result);

        WaitUntil("_1_0_2_AddJob_Sync_TimeToLive_Wait_Dead", () => !string.IsNullOrEmpty(jobId), 5);

        // wait 2 sec.
        Wait("_1_0_2_AddJob_Sync_TimeToLive_Wait_Dead", 2);

        // get queue len.
        var len = DisquuunDeserializer.Qlen(disquuun.Qlen(queueId).DEPRICATED_Sync());
        Assert("_1_0_2_AddJob_Sync_TimeToLive_Wait_Dead", len == 0, "not match, len:" + len);
        w.Stop();
        return w.ElapsedMilliseconds;
    }

    public long _1_0_3_AddJob_Sync_Retry(Disquuun disquuun)
    {
        WaitUntil("_1_0_3_AddJob_Sync_Retry", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var w = new Stopwatch();
        w.Start();


        var queueId = Guid.NewGuid().ToString();

        var jobId = DisquuunDeserializer.AddJob(disquuun.AddJob(queueId, data_10, 0, "RETRY", 1).DEPRICATED_Sync());

        WaitUntil("_1_0_3_AddJob_Sync_Retry", () => !string.IsNullOrEmpty(jobId), 5);

        var len0 = DisquuunDeserializer.Qlen(disquuun.Qlen(queueId).DEPRICATED_Sync());
        // len0 is 1. job is added.

        // getjob (but not ack it.)
        DisquuunDeserializer.GetJob(disquuun.GetJob(new string[] { queueId }).DEPRICATED_Sync());

        var len1 = DisquuunDeserializer.Qlen(disquuun.Qlen(queueId).DEPRICATED_Sync());
        // len1 is 0. job is deleted from queue.

        // wait 2 sec.
        Wait("_1_0_3_AddJob_Sync_Retry", 2);

        var len2 = DisquuunDeserializer.Qlen(disquuun.Qlen(queueId).DEPRICATED_Sync());
        Assert("_1_0_3_AddJob_Sync_Retry", len2 == 1, "not match, len2:" + len2);
        // job is returned to queue by retry param.

        disquuun.FastAck(new string[] { jobId }).DEPRICATED_Sync();
        w.Stop();
        return w.ElapsedMilliseconds;
    }

    public long _1_0_4_AddJob_Sync_Retry_0_And_TTL_1Sec(Disquuun disquuun)
    {
        WaitUntil("_1_0_4_AddJob_Sync_Retry_0_And_TTL_1Sec", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var w = new Stopwatch();
        w.Start();


        var queueId = Guid.NewGuid().ToString();

        var jobId = DisquuunDeserializer.AddJob(disquuun.AddJob(queueId, data_10, 0, "RETRY", 0, "TTL", 1).DEPRICATED_Sync());

        WaitUntil("_1_0_4_AddJob_Sync_Retry_0_And_TTL_1Sec", () => !string.IsNullOrEmpty(jobId), 5);

        DisquuunDeserializer.Qlen(disquuun.Qlen(queueId).DEPRICATED_Sync());

        // getjob (but not ack it.)
        DisquuunDeserializer.GetJob(disquuun.GetJob(new string[] { queueId }).DEPRICATED_Sync());

        // once, queue len should be zero by getJob.
        var len1 = DisquuunDeserializer.Qlen(disquuun.Qlen(queueId).DEPRICATED_Sync());
        Assert("_1_0_4_AddJob_Sync_Retry_0_And_TTL_1Sec", len1 == 0, "not match, len1:" + len1);

        // wait 2 sec.
        Wait("_1_0_4_AddJob_Sync_Retry_0_And_TTL_1Sec", 2);

        // both qlen and info returns rest job == 0.

        var len2 = DisquuunDeserializer.Qlen(disquuun.Qlen(queueId).DEPRICATED_Sync());
        Assert("_1_0_4_AddJob_Sync_Retry_0_And_TTL_1Sec", len2 == 0, "not match, len2:" + len2);

        // and rest job is 0.
        var info = DisquuunDeserializer.Info(disquuun.Info().DEPRICATED_Sync());
        Assert("_1_0_4_AddJob_Sync_Retry_0_And_TTL_1Sec", info.jobs.registered_jobs == 0, "not match.");
        w.Stop();
        return w.ElapsedMilliseconds;
    }

    public long _1_1_GetJob_Sync(Disquuun disquuun)
    {
        WaitUntil("_1_1_GetJob_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var w = new Stopwatch();
        w.Start();


        var queueId = Guid.NewGuid().ToString();

        disquuun.AddJob(queueId, data_10).DEPRICATED_Sync();

        var result = disquuun.GetJob(new string[] { queueId }).DEPRICATED_Sync();
        var jobDatas = DisquuunDeserializer.GetJob(result);
        Assert("_1_1_GetJob_Sync", 1, jobDatas.Length, "not match.");

        // ack in.
        var jobId = jobDatas[0].jobId;
        disquuun.FastAck(new string[] { jobId }).DEPRICATED_Sync();
        w.Stop();
        return w.ElapsedMilliseconds;
    }

    public long _1_1_1_GetJobWithCount_Sync(Disquuun disquuun)
    {
        WaitUntil("_1_1_1_GetJobWithCount_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var w = new Stopwatch();
        w.Start();


        var queueId = Guid.NewGuid().ToString();

        var addJobCount = 10000;
        for (var i = 0; i < addJobCount; i++) disquuun.AddJob(queueId, data_100).DEPRICATED_Sync();

        var result = disquuun.GetJob(new string[] { queueId }, "COUNT", addJobCount).DEPRICATED_Sync();
        var jobDatas = DisquuunDeserializer.GetJob(result);
        Assert("_1_1_1_GetJobWithCount_Sync", addJobCount, jobDatas.Length, "not match.");

        // ack in.
        var jobIds = jobDatas.Select(job => job.jobId).ToArray();
        disquuun.FastAck(jobIds).DEPRICATED_Sync();
        w.Stop();
        return w.ElapsedMilliseconds;
    }

    public long _1_1_2_GetJobFromMultiQueue_Sync(Disquuun disquuun)
    {
        WaitUntil("_1_1_2_GetJobFromMultiQueue_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var w = new Stopwatch();
        w.Start();


        var queueId1 = Guid.NewGuid().ToString();
        disquuun.AddJob(queueId1, data_10).DEPRICATED_Sync();

        var queueId2 = Guid.NewGuid().ToString();
        disquuun.AddJob(queueId2, data_10).DEPRICATED_Sync();

        var result = disquuun.GetJob(new string[] { queueId1, queueId2 }, "count", 2).DEPRICATED_Sync();
        var jobDatas = DisquuunDeserializer.GetJob(result);
        Assert("_1_1_2_GetJobFromMultiQueue_Sync", 2, jobDatas.Length, "not match.");

        // ack in.
        var jobIds = jobDatas.Select(job => job.jobId).ToArray();
        disquuun.FastAck(jobIds).DEPRICATED_Sync();
        w.Stop();
        return w.ElapsedMilliseconds;
    }

    public long _1_1_3_GetJobWithNoHang_Sync(Disquuun disquuun)
    {
        WaitUntil("_1_1_3_GetJobWithNoHang_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var w = new Stopwatch();
        w.Start();


        var queueId = Guid.NewGuid().ToString();

        var result = disquuun.GetJob(new string[] { queueId }, "NOHANG").DEPRICATED_Sync();
        var jobDatas = DisquuunDeserializer.GetJob(result);
        Assert("_1_1_3_GetJobWithNoHang_Sync", 0, jobDatas.Length, "not match.");
        w.Stop();
        return w.ElapsedMilliseconds;
    }

    public long _1_1_4_GetJobWithCounters_Sync(Disquuun disquuun)
    {
        WaitUntil("_1_1_4_GetJobWithCounters_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var w = new Stopwatch();
        w.Start();


        var queueId = Guid.NewGuid().ToString();

        disquuun.AddJob(queueId, data_100).DEPRICATED_Sync();

        var result = disquuun.GetJob(new string[] { queueId }, "withcounters").DEPRICATED_Sync();
        var jobDatas = DisquuunDeserializer.GetJob(result);
        var ackCount = jobDatas[0].additionalDeliveriesCount;
        Assert("_1_1_4_GetJobWithCounters_Sync", 0, ackCount, "not match.");

        disquuun.FastAck(new string[] { jobDatas[0].jobId }).DEPRICATED_Sync();
        w.Stop();
        return w.ElapsedMilliseconds;
    }

    public long _1_2_AckJob_Sync(Disquuun disquuun)
    {
        WaitUntil("_1_2_AckJob_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var w = new Stopwatch();
        w.Start();


        var queueId = Guid.NewGuid().ToString();
        var jobId = DisquuunDeserializer.AddJob(
            disquuun.AddJob(queueId, data_10).DEPRICATED_Sync()
        );

        var result = disquuun.AckJob(new string[] { jobId }).DEPRICATED_Sync();
        var ackCount = DisquuunDeserializer.AckJob(result);
        Assert("_1_2_AckJob_Sync", 1, ackCount, "not match.");
        w.Stop();
        return w.ElapsedMilliseconds;
    }

    public long _1_3_Fastack_Sync(Disquuun disquuun)
    {
        WaitUntil("_1_3_Fastack_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var w = new Stopwatch();
        w.Start();


        var queueId = Guid.NewGuid().ToString();
        var jobId = DisquuunDeserializer.AddJob(
            disquuun.AddJob(queueId, data_10).DEPRICATED_Sync()
        );

        var result = disquuun.FastAck(new string[] { jobId }).DEPRICATED_Sync();
        var ackCount = DisquuunDeserializer.FastAck(result);
        Assert("_1_3_Fastack_Sync", 1, ackCount, "not match.");
        w.Stop();
        return w.ElapsedMilliseconds;
    }

    public long _1_4_Working_Sync(Disquuun disquuun)
    {
        DisquuunLogger.Log("_1_4_Working_Sync not yet applied");

        WaitUntil("_1_4_Working_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

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

    public long _1_5_Nack_Sync(Disquuun disquuun)
    {
        DisquuunLogger.Log("_1_5_Nack_Sync not yet applied");

        WaitUntil("_1_5_Nack_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var queueId = Guid.NewGuid().ToString();
        // var jobId = DisquuunDeserializer.AddJob(
        // 	disquuun.AddJob(queueId, data_10).DEPRICATED_Sync()
        // );

        // var result = disquuun.FastAck(new string[]{jobId}).DEPRICATED_Sync();
        // var ackCount = DisquuunDeserializer.FastAck(result);
        // Assert("", 1, ackCount, "not match.");
        return 0;
    }

    public long _1_6_Info_Sync(Disquuun disquuun)
    {
        WaitUntil("_1_6_Info_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var w = new Stopwatch();
        w.Start();


        var infoData = disquuun.Info().DEPRICATED_Sync();
        var infoResult = DisquuunDeserializer.Info(infoData);

        Assert("_1_6_Info_Sync", 0, infoResult.jobs.registered_jobs, "not match.");
        w.Stop();
        return w.ElapsedMilliseconds;
    }

    public long _1_7_Hello_Sync(Disquuun disquuun)
    {
        WaitUntil("_1_7_Hello_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var w = new Stopwatch();
        w.Start();


        var helloData = disquuun.Hello().DEPRICATED_Sync();
        var helloResult = DisquuunDeserializer.Hello(helloData);

        Assert("_1_7_Hello_Sync", "1", helloResult.version, "not match.");
        w.Stop();
        return w.ElapsedMilliseconds;
    }

    public long _1_8_Qlen_Sync(Disquuun disquuun)
    {
        WaitUntil("_1_8_Qlen_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var w = new Stopwatch();
        w.Start();


        var queueId = Guid.NewGuid().ToString();
        var jobId = DisquuunDeserializer.AddJob(
            disquuun.AddJob(queueId, data_10).DEPRICATED_Sync()
        );

        var qlen = DisquuunDeserializer.Qlen(disquuun.Qlen(queueId).DEPRICATED_Sync());
        Assert("_1_8_Qlen_Sync", 1, qlen, "not match.");

        disquuun.FastAck(new string[] { jobId }).DEPRICATED_Sync();
        w.Stop();
        return w.ElapsedMilliseconds;
    }

    public long _1_9_Qstat_Sync(Disquuun disquuun)
    {
        WaitUntil("_1_9_Qstat_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);
        var w = new Stopwatch();
        w.Start();


        var queueId = Guid.NewGuid().ToString();
        var jobId = DisquuunDeserializer.AddJob(
            disquuun.AddJob(queueId, data_10).DEPRICATED_Sync()
        );

        var data = disquuun.Qstat(queueId).DEPRICATED_Sync();
        var qstatData = DisquuunDeserializer.Qstat(data);
        Assert("_1_9_Qstat_Sync", 1, qstatData.len, "not match.");

        disquuun.FastAck(new string[] { jobId }).DEPRICATED_Sync();
        w.Stop();
        return w.ElapsedMilliseconds;
    }


    public long _1_10_Qpeek_Sync(Disquuun disquuun)
    {
        DisquuunLogger.Log("_1_10_Qpeek_Sync not yet applied");
        // <queue-name> <count>

        WaitUntil("_1_10_Qpeek_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var queueId = Guid.NewGuid().ToString();
        // var jobId = DisquuunDeserializer.AddJob(
        // 	disquuun.AddJob(queueId, data_10).DEPRICATED_Sync()
        // );

        // var data = disquuun.Qpeek(queueId, 1).DEPRICATED_Sync();
        // var infoResult = DisquuunDeserializer.Info(infoData);

        // Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
        return 0;
    }

    public long _1_11_Enqueue_Sync(Disquuun disquuun)
    {
        DisquuunLogger.Log("_1_11_Enqueue_Sync not yet applied");
        // <job-id> ... <job-id>

        WaitUntil("_1_11_Enqueue_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var infoData = disquuun.Info().DEPRICATED_Sync();
        // var infoResult = DisquuunDeserializer.Info(infoData);

        // Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
        return 0;
    }


    public long _1_12_Dequeue_Sync(Disquuun disquuun)
    {
        DisquuunLogger.Log("_1_12_Dequeue_Sync not yet applied");
        // <job-id> ... <job-id>

        WaitUntil("_1_12_Dequeue_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var infoData = disquuun.Info().DEPRICATED_Sync();
        // var infoResult = DisquuunDeserializer.Info(infoData);

        // Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
        return 0;
    }

    public long _1_13_DelJob_Sync(Disquuun disquuun)
    {
        DisquuunLogger.Log("_1_13_DelJob_Sync not yet applied");
        // <job-id> ... <job-id>

        WaitUntil("_1_13_DelJob_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var infoData = disquuun.Info().DEPRICATED_Sync();
        // var infoResult = DisquuunDeserializer.Info(infoData);

        // Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
        return 0;
    }

    public long _1_14_Show_Sync(Disquuun disquuun)
    {
        DisquuunLogger.Log("_1_14_Show_Sync not yet applied");
        // <job-id>

        WaitUntil("_1_14_Show_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var infoData = disquuun.Info().DEPRICATED_Sync();
        // var infoResult = DisquuunDeserializer.Info(infoData);

        // Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
        return 0;
    }

    public long _1_15_Qscan_Sync(Disquuun disquuun)
    {
        DisquuunLogger.Log("_1_15_Qscan_Sync not yet applied");
        // [COUNT <count>] [BUSYLOOP] [MINLEN <len>] [MAXLEN <len>] [IMPORTRATE <rate>]

        WaitUntil("_1_15_Qscan_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var infoData = disquuun.Info().DEPRICATED_Sync();
        // var infoResult = DisquuunDeserializer.Info(infoData);

        // Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
        return 0;
    }

    public long _1_16_Jscan_Sync(Disquuun disquuun)
    {
        DisquuunLogger.Log("_1_16_Jscan_Sync not yet applied");
        // [<cursor>] [COUNT <count>] [BUSYLOOP] [QUEUE <queue>] [STATE <state1> STATE <state2> ... STATE <stateN>] [REPLY all|id]

        WaitUntil("_1_16_Jscan_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var infoData = disquuun.Info().DEPRICATED_Sync();
        // var infoResult = DisquuunDeserializer.Info(infoData);

        // Assert("", 0, infoResult.jobs.registered_jobs, "not match.");
        return 0;
    }

    public long _1_17_Pause_Sync(Disquuun disquuun)
    {
        DisquuunLogger.Log("_1_17_Pause_Sync not yet applied");
        // <queue-name> option1 [option2 ... optionN]

        WaitUntil("_1_17_Pause_Sync", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        // var pauseData = disquuun.Pause().DEPRICATED_Sync();
        // var pauseResult = DisquuunDeserializer.Info(pauseData);

        // Assert("", 0, pauseResult.jobs.registered_jobs, "not match.");
        return 0;
    }
}
