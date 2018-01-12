using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Disquuun
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<SomeTest>();
        }
    }

    public class SomeTest
    {
        public int[] _array;
        public List<int> _list;


        public SomeTest()
        {
            _array = Enumerable.Range(0, 10000).ToArray();
            _list = _array.ToList();
        }

        [Benchmark]
        public int UseArrayFor()
        {
            var accum = 0;

            for (int i = 0; i < _array.Length; i++)
            {
                accum += _array[i];
            }

            return accum;
        }


        [Benchmark]
        public int UseArrayForEach()
        {
            var accum = 0;

            foreach (var i in _array)
            {
                accum += i;
            }

            return accum;
        }

        [Benchmark]
        public int UseListFor()
        {
            var accum = 0;

            for (int i = 0; i < _list.Count; i++)
            {
                accum += _list[i];
            }

            return accum;
        }

        [Benchmark]
        public int UseListForEach()
        {
            var accum = 0;

            foreach (var i in _list)
            {
                accum += _list[i];
            }

            return accum;
        }
    }
}
