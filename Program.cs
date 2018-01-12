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
        }
    }

    // [ShortRunJob]
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
            disquuun2 = new Disquuun(
                "127.0.0.1", 7711, 1024, 2
            );
            disquuun10 = new Disquuun(
                "127.0.0.1", 7711, 1024, 10
            );
            disquuun30 = new Disquuun(
                "127.0.0.1", 7711, 1024, 30
            );
        }

        // [Benchmark]
        // public void Take_10byte_2sock()
        // {
        //     disquuun2.AddJob(qName, dataBytes1).DEPRICATED_Sync();
        // }

        // [Benchmark]
        // public void Take_10byte_10sock()
        // {
        //     disquuun10.AddJob(qName, dataBytes1).DEPRICATED_Sync();
        // }

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

        // [Benchmark]
        // public void Take10Connection2()
        // {
        //     var waitHandle = new ManualResetEvent(false);
        //     disquuun.AddJob(qName, dataBytes2).Async((a, b) =>
        //     {
        //         waitHandle.Set();
        //     });
        //     waitHandle.WaitOne(Timeout.Infinite);
        // }

        // // [Benchmark]
        // // public void Take10Connection3()
        // // {
        // //     var waitHandle = new ManualResetEvent(false);
        // //     disquuun.AddJob(qName, dataBytes3).Async((a, b) =>
        // //     {
        // //         waitHandle.Set();
        // //     });
        // //     waitHandle.WaitOne(Timeout.Infinite);
        // // }

        // [Benchmark]
        // public void Take_10byte_pipeline()
        // {
        //     var waitHandle = new ManualResetEvent(false);
        //     for (var i = 0; i < 1; i++)
        //     {
        //         disquuun.Pipeline(disquuun.AddJob(qName, dataBytes1));
        //     }
        //     disquuun.Pipeline().Execute((a, b) =>
        //     {
        //         waitHandle.Set();
        //     });
        //     waitHandle.WaitOne(Timeout.Infinite);
        // }

        // [Benchmark]
        // public void Take_10byte_pipeline10()
        // {
        //     var waitHandle = new ManualResetEvent(false);
        //     for (var i = 0; i < 10; i++)
        //     {
        //         disquuun.Pipeline(disquuun.AddJob(qName, dataBytes1));
        //     }
        //     disquuun.Pipeline().Execute((a, b) =>
        //     {
        //         waitHandle.Set();
        //     });
        //     waitHandle.WaitOne(Timeout.Infinite);
        // }
    }
}
