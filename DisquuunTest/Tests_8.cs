using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DisquuunCore;
using DisquuunCore.Deserialize;

/*
	benchmark
*/

public partial class Tests
{
    private object _8_0_AddJob1000LockObject = new object();

    public long _8_0_AddJob1000(Disquuun disquuun)
    {
        WaitUntil("_8_0_AddJob1000", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var count = 1000 * ratio;


        var connectedCount = 0;
        disquuun = new Disquuun(DisquuunTests.TestDisqueHostStr, DisquuunTests.TestDisquePortNum, 1024, 10,
            disquuunId =>
            {
                connectedCount++;
            },
            (info, e) =>
            {
                TestLogger.Log("error, info:" + info + " e:" + e);
            },
            currentSocketCount =>
            {

                if (100 < currentSocketCount)
                {
                    return null;
                }
                return new Tuple<bool, int>(true, 1);
            }
        );


        WaitUntil("_8_0_AddJob1000", () => (connectedCount == 1), 10);


        var addedCount = 0;

        var queueId = Guid.NewGuid().ToString();

        var w = new Stopwatch();
        w.Start();
        for (var i = 0; i < count; i++)
        {
            disquuun.AddJob(queueId, data_10).Async(
                (command, data) =>
                {
                    lock (_8_0_AddJob1000LockObject) addedCount++;
                }
            );
        }
        w.Stop();

        WaitUntil("_8_0_AddJob1000", () => (addedCount == count), 100);

        var gotCount = 0;
        disquuun.GetJob(new string[] { queueId }, "count", count).Async(
            (command, data) =>
            {
                var jobDatas = DisquuunDeserializer.GetJob(data);
                var jobIds = jobDatas.Select(j => j.jobId).ToArray();
                disquuun.FastAck(jobIds).Async(
                    (c2, d2) =>
                    {
                        var fastackCount = DisquuunDeserializer.FastAck(d2);
                        lock (_8_0_AddJob1000LockObject)
                        {
                            gotCount += fastackCount;
                        }
                    }
                );
            }
        );

        WaitUntil("_8_0_AddJob1000", () => (gotCount == count), 10);
        disquuun.Disconnect();

        return w.ElapsedMilliseconds;
    }

    private object _8_0_0_AddJob1000by100ConnectionLockObject = new object();

    public long _8_0_0_AddJob1000by100Connection(Disquuun disquuun)
    {
        WaitUntil("_8_0_0_AddJob1000by100Connection", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var count = 1000 * ratio;

        var connectedCount = 0;
        disquuun = new Disquuun(DisquuunTests.TestDisqueHostStr, DisquuunTests.TestDisquePortNum, 1024, 10,
            disquuunId =>
            {
                connectedCount++;
            },
            (info, e) =>
            {
                TestLogger.Log("error, info:" + info + " e:" + e.Message);
            },
            currentSocketCount =>
            {

                if (100 < currentSocketCount)
                {
                    return null;
                }
                return new Tuple<bool, int>(true, 1);
            }
        );

        WaitUntil("_8_0_0_AddJob1000by100Connection 0", () => (connectedCount == 1), 5);

        var addedCount = 0;

        var queueId = Guid.NewGuid().ToString();

        var w = new Stopwatch();
        w.Start();
        for (var i = 0; i < count; i++)
        {
            disquuun.AddJob(queueId, data_10).Async(
                (command, data) =>
                {
                    lock (_8_0_0_AddJob1000by100ConnectionLockObject) addedCount++;
                }
            );
        }
        w.Stop();

        WaitUntil("_8_0_0_AddJob1000by100Connection 1", () => (addedCount == count), 100);

        var gotCount = 0;
        disquuun.GetJob(new string[] { queueId }, "count", count).Async(
            (command, data) =>
            {
                var jobDatas = DisquuunDeserializer.GetJob(data);
                var jobIds = jobDatas.Select(j => j.jobId).ToArray();
                disquuun.FastAck(jobIds).Async(
                    (c2, d2) =>
                    {
                        var fastackCount = DisquuunDeserializer.FastAck(d2);
                        lock (_8_0_0_AddJob1000by100ConnectionLockObject)
                        {
                            gotCount += fastackCount;
                        }
                    }
                );
            }
        );

        WaitUntil("_8_0_0_AddJob1000by100Connection 2", () => (gotCount == count), 10);
        disquuun.Disconnect();

        return w.ElapsedMilliseconds;
    }

    private object _8_0_1_AddJob1000byPipelineObject = new object();

    public long _8_0_1_AddJob1000byPipeline(Disquuun disquuun)
    {
        WaitUntil("_8_0_1_AddJob1000byPipeline", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var count = 1000 * ratio;

        var connectedCount = 0;
        disquuun = new Disquuun(DisquuunTests.TestDisqueHostStr, DisquuunTests.TestDisquePortNum, 1024, 10,
            disquuunId =>
            {
                connectedCount++;
            },
            (info, e) =>
            {
                TestLogger.Log("error, info:" + info + " e:" + e.Message);
            },
            currentSocketCount =>
            {

                if (100 < currentSocketCount)
                {
                    return null;
                }
                return new Tuple<bool, int>(true, 1);
            }
        );

        WaitUntil("_8_0_1_AddJob1000byPipeline 0", () => (connectedCount == 1), 5);

        var addedCount = 0;

        var queueId = Guid.NewGuid().ToString();

        var w = new Stopwatch();
        w.Start();
        for (var i = 0; i < count; i++)
        {
            disquuun.Pipeline(disquuun.AddJob(queueId, data_10));
        }

        disquuun.Pipeline().Execute(
            (command, data) =>
            {
                lock (_8_0_1_AddJob1000byPipelineObject)
                {
                    addedCount++;
                }
            }
        );

        w.Stop();

        WaitUntil("_8_0_1_AddJob1000byPipeline 1", () => (addedCount == count), 100);

        var gotCount = 0;
        disquuun.GetJob(new string[] { queueId }, "count", count).Async(
            (command, data) =>
            {
                var jobDatas = DisquuunDeserializer.GetJob(data);
                var jobIds = jobDatas.Select(j => j.jobId).ToArray();
                disquuun.FastAck(jobIds).Async(
                    (c2, d2) =>
                    {
                        var fastackCount = DisquuunDeserializer.FastAck(d2);
                        lock (_8_0_1_AddJob1000byPipelineObject)
                        {
                            gotCount += fastackCount;
                        }
                    }
                );
            }
        );

        WaitUntil("_8_0_1_AddJob1000byPipeline 2", () => (gotCount == count), 10);
        disquuun.Disconnect();

        return w.ElapsedMilliseconds;
    }

    private object _8_0_2_AddJob1000byPipelinesObject = new object();

    public long _8_0_2_AddJob1000byPipelines(Disquuun disquuun)
    {
        WaitUntil("_8_0_2_AddJob1000byPipelines", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var count = 1000 * ratio;

        var connectedCount = 0;
        disquuun = new Disquuun(DisquuunTests.TestDisqueHostStr, DisquuunTests.TestDisquePortNum, 1024, 10,
            disquuunId =>
            {
                connectedCount++;
            },
            (info, e) =>
            {
                TestLogger.Log("error, info:" + info + " e:" + e.Message);
            },
            currentSocketCount =>
            {

                if (100 < currentSocketCount)
                {
                    return null;
                }
                return new Tuple<bool, int>(true, 1);
            }
        );

        WaitUntil("_8_0_2_AddJob1000byPipelines 0", () => (connectedCount == 1), 5);

        var addedCount = 0;

        var queueId = Guid.NewGuid().ToString();

        var w = new Stopwatch();
        w.Start();
        for (var i = 0; i < count; i++)
        {
            if (i == count / 2) disquuun.RevolvePipeline();
            disquuun.Pipeline(disquuun.AddJob(queueId, data_10));
        }
        disquuun.Pipeline().Execute(
            (command, data) =>
            {
                lock (_8_0_2_AddJob1000byPipelinesObject) addedCount++;
            }
        );

        w.Stop();

        WaitUntil("_8_0_2_AddJob1000byPipelines 1", () => (addedCount == count), 100);

        var gotCount = 0;
        disquuun.GetJob(new string[] { queueId }, "count", count).Async(
            (command, data) =>
            {
                var jobDatas = DisquuunDeserializer.GetJob(data);
                var jobIds = jobDatas.Select(j => j.jobId).ToArray();
                disquuun.FastAck(jobIds).Async(
                    (c2, d2) =>
                    {
                        var fastackCount = DisquuunDeserializer.FastAck(d2);
                        lock (_8_0_2_AddJob1000byPipelinesObject)
                        {
                            gotCount += fastackCount;
                        }
                    }
                );
            }
        );

        WaitUntil("_8_0_2_AddJob1000byPipelines 2", () => (gotCount == count), 10);
        disquuun.Disconnect();

        return w.ElapsedMilliseconds;
    }

    private object _8_1_GetJob1000Lock = new object();

    public long _8_1_GetJob1000(Disquuun disquuun)
    {
        WaitUntil("_8_1_GetJob1000", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var addingJobCount = 1000 * ratio;

        var connected = false;
        disquuun = new Disquuun(DisquuunTests.TestDisqueHostStr, DisquuunTests.TestDisquePortNum, 1024, 10,
            disquuunId =>
            {
                connected = true;
            },
            (info, e) =>
            {
                TestLogger.Log("error, info:" + info + " e:" + e.Message, true);
                throw e;
            },
            currentSocketCount =>
            {

                if (100 < currentSocketCount)
                {
                    return null;
                }
                return new Tuple<bool, int>(true, 1);
            }
        );

        var r0 = WaitUntil("r0 _8_1_GetJob1000", () => connected, 5);
        if (!r0) return 0;

        var addedCount = 0;

        var queueId = Guid.NewGuid().ToString();

        for (var i = 0; i < addingJobCount; i++)
        {
            disquuun.AddJob(queueId, data_10).Async(
                (command, data) =>
                {
                    lock (_8_1_GetJob1000Lock) addedCount++;
                }
            );
        }

        var r1 = WaitUntil("r1 _8_1_GetJob1000", () => (addedCount == addingJobCount), 10);
        if (!r1) return 0;

        var gotJobDataIds = new List<string>();


        var w = new Stopwatch();
        w.Start();
        for (var i = 0; i < addingJobCount; i++)
        {
            disquuun.GetJob(new string[] { queueId }).Async(
                (command, data) =>
                {
                    lock (_8_1_GetJob1000Lock)
                    {
                        var jobDatas = DisquuunDeserializer.GetJob(data);
                        var jobIds = jobDatas.Select(j => j.jobId).ToList();
                        gotJobDataIds.AddRange(jobIds);
                    }
                }
            );
        }

        var r2 = WaitUntil("r2 _8_1_GetJob1000", () => (gotJobDataIds.Count == addingJobCount), 10);
        if (!r2)
        {
            TestLogger.Log("gotJobDataIds:" + gotJobDataIds.Count, true);
            return 0;
        }

        w.Stop();

        var result = DisquuunDeserializer.FastAck(disquuun.FastAck(gotJobDataIds.ToArray()).DEPRICATED_Sync());

        Assert("_8_1_GetJob1000", addingJobCount, result, "result not match.");
        disquuun.Disconnect();

        return w.ElapsedMilliseconds;
    }

    private object _8_1_0_GetJob1000by100ConnectionLockObject = new object();

    public long _8_1_0_GetJob1000by100Connection(Disquuun disquuun)
    {
        WaitUntil("_8_1_0_GetJob1000by100Connection", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var addingJobCount = 1000 * ratio;

        var connected = false;
        disquuun = new Disquuun(DisquuunTests.TestDisqueHostStr, DisquuunTests.TestDisquePortNum, 1024, 10,
            disquuunId =>
            {
                connected = true;
            },
            (info, e) =>
            {
                TestLogger.Log("error, info:" + info + " e:" + e.Message);
            },
            currentSocketCount =>
            {

                if (100 < currentSocketCount)
                {
                    return null;
                }
                return new Tuple<bool, int>(true, 1);
            }
        );

        WaitUntil("_8_1_0_GetJob1000by100Connection", () => connected, 5);

        var addedCount = 0;

        var queueId = Guid.NewGuid().ToString();

        for (var i = 0; i < addingJobCount; i++)
        {
            disquuun.AddJob(queueId, data_10).Async(
                (command, data) =>
                {
                    lock (_8_1_0_GetJob1000by100ConnectionLockObject) addedCount++;
                }
            );
        }


        WaitUntil("_8_1_0_GetJob1000by100Connection", () => (addedCount == addingJobCount), 10);

        var gotJobDataIds = new List<string>();


        var w = new Stopwatch();
        w.Start();
        for (var i = 0; i < addingJobCount; i++)
        {
            disquuun.GetJob(new string[] { queueId }).Async(
                (command, data) =>
                {
                    lock (_8_1_0_GetJob1000by100ConnectionLockObject)
                    {
                        var jobDatas = DisquuunDeserializer.GetJob(data);
                        var jobIds = jobDatas.Select(j => j.jobId).ToList();
                        gotJobDataIds.AddRange(jobIds);
                    }
                }
            );
        }

        WaitUntil("_8_1_0_GetJob1000by100Connection", () => (gotJobDataIds.Count == addingJobCount), 10);


        w.Stop();

        var result = DisquuunDeserializer.FastAck(disquuun.FastAck(gotJobDataIds.ToArray()).DEPRICATED_Sync());

        Assert("_8_1_0_GetJob1000by100Connection", addingJobCount, result, "result not match.");
        disquuun.Disconnect();

        return w.ElapsedMilliseconds;
    }

    private object _8_1_1_GetJob1000byPipelineObject = new object();

    public long _8_1_1_GetJob1000byPipeline(Disquuun disquuun)
    {
        WaitUntil("_8_1_1_GetJob1000byPipeline", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var addingJobCount = 1000 * ratio;

        var connected = false;
        disquuun = new Disquuun(DisquuunTests.TestDisqueHostStr, DisquuunTests.TestDisquePortNum, 1024, 10,
            disquuunId =>
            {
                connected = true;
            },
            (info, e) =>
            {
                TestLogger.Log("error, info:" + info + " e:" + e.Message);
            },
            currentSocketCount =>
            {

                if (100 < currentSocketCount)
                {
                    return null;
                }
                return new Tuple<bool, int>(true, 1);
            }
        );

        WaitUntil("_8_1_1_GetJob1000byPipeline", () => connected, 5);

        var addedCount = 0;

        var queueId = Guid.NewGuid().ToString();

        for (var i = 0; i < addingJobCount; i++)
        {
            disquuun.Pipeline(disquuun.AddJob(queueId, data_10));
        }

        disquuun.Pipeline().Execute(
            (command, data) =>
            {
                lock (_8_1_1_GetJob1000byPipelineObject) addedCount++;
            }
        );


        WaitUntil("_8_1_1_GetJob1000byPipeline", () => (addedCount == addingJobCount), 10);

        var gotJobDataIds = new List<string>();


        var w = new Stopwatch();
        w.Start();
        for (var i = 0; i < addingJobCount; i++)
        {
            disquuun.Pipeline(disquuun.GetJob(new string[] { queueId }));
        }
        disquuun.Pipeline().Execute(
            (command, data) =>
            {
                lock (_8_1_1_GetJob1000byPipelineObject)
                {
                    var jobDatas = DisquuunDeserializer.GetJob(data);
                    var jobIds = jobDatas.Select(j => j.jobId).ToList();
                    gotJobDataIds.AddRange(jobIds);
                }
            }
        );

        WaitUntil("_8_1_1_GetJob1000byPipeline", () => (gotJobDataIds.Count == addingJobCount), 10);


        w.Stop();

        var result = DisquuunDeserializer.FastAck(disquuun.FastAck(gotJobDataIds.ToArray()).DEPRICATED_Sync());

        Assert("_8_1_1_GetJob1000byPipeline", addingJobCount, result, "result not match.");
        disquuun.Disconnect();

        return w.ElapsedMilliseconds;
    }

    private object _8_1_2_GetJob1000byPipelinesObject = new object();

    public long _8_1_2_GetJob1000byPipelines(Disquuun disquuun)
    {
        WaitUntil("_8_1_2_GetJob1000byPipelines", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var addingJobCount = 1000 * ratio;

        var connected = false;
        disquuun = new Disquuun(DisquuunTests.TestDisqueHostStr, DisquuunTests.TestDisquePortNum, 1024, 10,
            disquuunId =>
            {
                connected = true;
            },
            (info, e) =>
            {
                TestLogger.Log("error, info:" + info + " e:" + e.Message);
            },
            currentSocketCount =>
            {

                if (100 < currentSocketCount)
                {
                    return null;
                }
                return new Tuple<bool, int>(true, 1);
            }
        );

        WaitUntil("_8_1_2_GetJob1000byPipelines", () => connected, 5);

        var addedCount = 0;

        var queueId = Guid.NewGuid().ToString();

        for (var i = 0; i < addingJobCount; i++)
        {
            disquuun.Pipeline(disquuun.AddJob(queueId, data_10));
        }

        disquuun.Pipeline().Execute(
            (command, data) =>
            {
                lock (_8_1_2_GetJob1000byPipelinesObject) addedCount++;
            }
        );


        WaitUntil("_8_1_2_GetJob1000byPipelines", () => (addedCount == addingJobCount), 10);

        var gotJobDataIds = new List<string>();


        var w = new Stopwatch();
        w.Start();
        for (var i = 0; i < addingJobCount; i++)
        {
            if (i == addingJobCount / 2) disquuun.RevolvePipeline();
            disquuun.Pipeline(disquuun.GetJob(new string[] { queueId }));
        }
        disquuun.Pipeline().Execute(
            (command, data) =>
            {
                lock (_8_1_2_GetJob1000byPipelinesObject)
                {
                    var jobDatas = DisquuunDeserializer.GetJob(data);
                    var jobIds = jobDatas.Select(j => j.jobId).ToList();
                    gotJobDataIds.AddRange(jobIds);
                }
            }
        );

        WaitUntil("_8_1_2_GetJob1000byPipelines", () => (gotJobDataIds.Count == addingJobCount), 10);

        w.Stop();

        var result = DisquuunDeserializer.FastAck(disquuun.FastAck(gotJobDataIds.ToArray()).DEPRICATED_Sync());

        Assert("_8_1_2_GetJob1000byPipelines", addingJobCount, result, "result not match.");
        disquuun.Disconnect();

        return w.ElapsedMilliseconds;
    }

    private object _8_2_GetJob1000byLoopLockObject = new object();
    public long _8_2_GetJob1000byLoop(Disquuun disquuun)
    {
        WaitUntil("_8_2_GetJob1000byLoop 0", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var addingJobCount = 1000 * ratio;

        var queueId = Guid.NewGuid().ToString();

        var addedCount = 0;

        for (var i = 0; i < addingJobCount; i++)
        {
            disquuun.AddJob(queueId, data_10).Async(
                (command, data) =>
                {
                    lock (_8_2_GetJob1000byLoopLockObject) addedCount++;
                }
            );
        }

        WaitUntil("_8_2_GetJob1000byLoop 1", () => (addedCount == addingJobCount), 5);

        var gotJobDataIds = new List<string>();

        var w = new Stopwatch();
        w.Start();

        disquuun.GetJob(new string[] { queueId }, "count", addingJobCount).Loop(
            (command, data) =>
            {
                lock (_8_2_GetJob1000byLoopLockObject)
                {
                    var jobDatas = DisquuunDeserializer.GetJob(data);
                    var jobIds = jobDatas.Select(j => j.jobId).ToList();
                    gotJobDataIds.AddRange(jobIds);

                    if (gotJobDataIds.Count == addingJobCount)
                    {
                        w.Stop();
                        return false;
                    }
                    return true;
                }
            }
        );

        WaitUntil("_8_2_GetJob1000byLoop 2", () => (gotJobDataIds.Count == addingJobCount), 30);
        WaitUntil("_8_2_GetJob1000byLoop 3", () => (0 < disquuun.AvailableSocketNum()), 1);

        var fastackedCount = 0;
        disquuun.FastAck(gotJobDataIds.ToArray()).Async(
            (c, data) =>
            {
                fastackedCount = DisquuunDeserializer.FastAck(data);
            }
        );

        WaitUntil("_8_2_GetJob1000byLoop 4", () => (addingJobCount == fastackedCount), 10);

        return w.ElapsedMilliseconds;
    }

    /*
		unexecutable test. because of 
	*/
    private object _8_2_0_GetJob1000byPipelineObject = new object();
    public long _8_2_0_GetJob1000byPipeline(Disquuun disquuun)
    {
        WaitUntil("_8_2_0_GetJob1000byPipeline 0", () => (disquuun.State() == Disquuun.ConnectionState.OPENED), 5);

        var addingJobCount = 1000 * ratio;

        var queueId = Guid.NewGuid().ToString();

        var addedCount = 0;

        for (var i = 0; i < addingJobCount; i++)
        {
            disquuun.Pipeline(disquuun.AddJob(queueId, data_10));
        }
        disquuun.Pipeline().Execute(
            (command, data) =>
            {
                lock (_8_2_GetJob1000byLoopLockObject) addedCount++;
            }
        );

        WaitUntil("_8_2_0_GetJob1000byPipeline 1", () => (addedCount == addingJobCount), 5);

        var gotJobDataIds = new List<string>();

        var w = new Stopwatch();
        w.Start();

        for (var i = 0; i < addingJobCount; i++)
        {
            disquuun.Pipeline(disquuun.GetJob(new string[] { queueId }, "count", addingJobCount));
        }
        disquuun.Pipeline().Execute(
            (command, data) =>
            {
                lock (_8_2_0_GetJob1000byPipelineObject)
                {
                    var jobDatas = DisquuunDeserializer.GetJob(data);
                    var jobIds = jobDatas.Select(j => j.jobId).ToList();
                    gotJobDataIds.AddRange(jobIds);

                    if (gotJobDataIds.Count == addingJobCount)
                    {
                        w.Stop();
                    }
                }
            }
        );

        WaitUntil("_8_2_0_GetJob1000byPipeline 2", () => (gotJobDataIds.Count == addingJobCount), 30);
        WaitUntil("_8_2_0_GetJob1000byPipeline 3", () => (0 < disquuun.AvailableSocketNum()), 1);

        var fastackedCount = 0;
        disquuun.FastAck(gotJobDataIds.ToArray()).Async(
            (c, data) =>
            {
                fastackedCount = DisquuunDeserializer.FastAck(data);
            }
        );

        WaitUntil("_8_2_0_GetJob1000byPipeline 4", () => (addingJobCount == fastackedCount), 10);

        return w.ElapsedMilliseconds;
    }
}