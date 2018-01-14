using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Running;
using DisquuunCore;

namespace DisquuunTest
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<DisquuunBench>();
            // DisquuunTests.Start();
        }
    }

    [ShortRunJob]
    public class DisquuunBench2
    {
        [Benchmark]
        public void Bench()
        {
            Disquuun disquuun = null;
            var sampleQueueName = Guid.NewGuid().ToString();
            var n = 100;

            var waitHandles = new ManualResetEvent[n];
            for (var i = 0; i < n; i++)
            {
                waitHandles[i] = new ManualResetEvent(false);
            }
            disquuun = new Disquuun(
                "127.0.0.1", 7711, 1024, 10, id =>
                {
                    for (var i = 0; i < n; i++)
                    {
                        var index = i;
                        disquuun.AddJob(sampleQueueName, new byte[100]).Async(
                            (command, data) =>
                            {
                                waitHandles[index].Set();
                            }
                        );
                    }
                }
            );

            for (var i = 0; i < n; i++)
            {
                waitHandles[i].WaitOne(Timeout.Infinite);
            }
        }
    }

    [ShortRunJob]
    public class DisquuunBench
    {
        public Disquuun disquuun2;
        public Disquuun disquuun10;
        public Disquuun disquuun30;

        const string qName = "testQueue";

        byte[] dataBytes1 = new byte[10];
        byte[] dataBytes2 = new byte[100];
        byte[] dataBytes3 = new byte[1000];

        public DisquuunBench()
        {
            {
                var waitHandle = new ManualResetEvent(false);
                disquuun2 = new Disquuun(
                    "127.0.0.1", 7711, 1024, 2, id =>
                    {
                        waitHandle.Set();
                        // Console.WriteLine("ready2");
                    }
                );
                waitHandle.WaitOne(Timeout.Infinite);
            }
            {
                var waitHandle = new ManualResetEvent(false);
                disquuun10 = new Disquuun(
                    "127.0.0.1", 7711, 1024, 10, id =>
                    {
                        waitHandle.Set();
                        // Console.WriteLine("ready10");
                    }
                );
                waitHandle.WaitOne(Timeout.Infinite);
            }
            {
                var waitHandle = new ManualResetEvent(false);
                disquuun30 = new Disquuun(
                    "127.0.0.1", 7711, 1024, 30, id =>
                    {
                        waitHandle.Set();
                        // Console.WriteLine("ready30");
                    }
                );
                waitHandle.WaitOne(Timeout.Infinite);
            }
        }


        [Benchmark]
        public void Take_10byte_2sock_async()
        {
            var waitHandle = new ManualResetEvent(false);
            disquuun2.AddJob(qName, dataBytes1).Async((a, b) =>
            {
                waitHandle.Set();
            });
            waitHandle.WaitOne(Timeout.Infinite);
        }

        [Benchmark]
        public void Take_10byte_2sock_sync()
        {
            disquuun2.AddJob(qName, dataBytes1).DEPRICATED_Sync();
        }

        [Benchmark]
        public void Take_10byte_10sock_async()
        {
            var waitHandle = new ManualResetEvent(false);
            disquuun10.AddJob(qName, dataBytes1).Async((a, b) =>
            {
                waitHandle.Set();
            });
            waitHandle.WaitOne(Timeout.Infinite);
        }

        [Benchmark]
        public void Take_10byte_30sock_async()
        {
            var waitHandle = new ManualResetEvent(false);
            disquuun30.AddJob(qName, dataBytes1).Async((a, b) =>
            {
                waitHandle.Set();
            });
            waitHandle.WaitOne(Timeout.Infinite);
        }


        // 10 item.
        [Benchmark]
        public void Take_10byte_2sock_sync_10item()
        {
            for (var i = 0; i < 10; i++)
            {
                disquuun2.AddJob(qName, dataBytes1).DEPRICATED_Sync();
            }
        }

        [Benchmark]
        public void Take_10byte_10sock_async_10item()
        {
            var localLock = new object();
            var waitHandle = new ManualResetEvent(false);
            var j = 0;
            for (var i = 0; i < 10; i++)
            {
                disquuun10.AddJob(qName, dataBytes1).Async((a, b) =>
                {
                    lock (localLock)
                    {
                        j++;
                        if (j == 10)
                        {
                            waitHandle.Set();
                        }
                    }
                });
            }
            waitHandle.WaitOne(Timeout.Infinite);
        }

        [Benchmark]
        public void Take_10byte_30sock_async_10item()
        {
            var localLock = new object();
            var waitHandle = new ManualResetEvent(false);
            var j = 0;
            for (var i = 0; i < 10; i++)
            {
                disquuun30.AddJob(qName, dataBytes1).Async((a, b) =>
                {
                    lock (localLock)
                    {
                        j++;
                        if (j == 10)
                        {
                            waitHandle.Set();
                        }
                    }
                });
            }
            waitHandle.WaitOne(Timeout.Infinite);
        }

        // pipelines, 1 item.

        [Benchmark]
        public void Take_10byte_2sock_pipeline()
        {
            var waitHandle = new ManualResetEvent(false);
            for (var i = 0; i < 1; i++)
            {
                disquuun2.Pipeline(disquuun2.AddJob(qName, dataBytes1));
            }
            disquuun2.Pipeline().Execute((a, b) =>
            {
                waitHandle.Set();
            });
            waitHandle.WaitOne(Timeout.Infinite);
        }

        [Benchmark]
        public void Take_10byte_10sock_pipeline()
        {
            var waitHandle = new ManualResetEvent(false);
            for (var i = 0; i < 1; i++)
            {
                disquuun10.Pipeline(disquuun10.AddJob(qName, dataBytes1));
            }
            disquuun10.Pipeline().Execute((a, b) =>
            {
                waitHandle.Set();
            });
            waitHandle.WaitOne(Timeout.Infinite);
        }

        [Benchmark]
        public void Take_10byte_30sock_pipeline()
        {
            var waitHandle = new ManualResetEvent(false);
            for (var i = 0; i < 1; i++)
            {
                disquuun30.Pipeline(disquuun30.AddJob(qName, dataBytes1));
            }
            disquuun30.Pipeline().Execute((a, b) =>
            {
                waitHandle.Set();
            });
            waitHandle.WaitOne(Timeout.Infinite);
        }


        // pipelines, 10 item.

        [Benchmark]
        public void Take_10byte_2sock_pipeline_10()
        {
            var waitHandle = new ManualResetEvent(false);
            for (var i = 0; i < 10; i++)
            {
                disquuun2.Pipeline(disquuun2.AddJob(qName, dataBytes1));
            }
            disquuun2.Pipeline().Execute((a, b) =>
            {
                waitHandle.Set();
            });
            waitHandle.WaitOne(Timeout.Infinite);
        }

        [Benchmark]
        public void Take_10byte_10sock_pipeline_10()
        {
            var waitHandle = new ManualResetEvent(false);
            for (var i = 0; i < 10; i++)
            {
                disquuun10.Pipeline(disquuun10.AddJob(qName, dataBytes1));
            }
            disquuun10.Pipeline().Execute((a, b) =>
            {
                waitHandle.Set();
            });
            waitHandle.WaitOne(Timeout.Infinite);
        }

        [Benchmark]
        public void Take_10byte_30sock_pipeline_10()
        {
            var waitHandle = new ManualResetEvent(false);
            for (var i = 0; i < 10; i++)
            {
                disquuun30.Pipeline(disquuun30.AddJob(qName, dataBytes1));
            }
            disquuun30.Pipeline().Execute((a, b) =>
            {
                waitHandle.Set();
            });
            waitHandle.WaitOne(Timeout.Infinite);
        }
    }
}
