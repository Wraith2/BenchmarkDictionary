using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Running;
using Generic.DictionaryDev;
using Wraith.Collections.Generic;

namespace BenchmarkDictionary
{
    class Program
    {
        static void Main(string[] args)
        {

			DictionaryDevConcurrentAccessDetectionTests tests = new DictionaryDevConcurrentAccessDetectionTests();
			tests.DictionaryDevConcurrentAccessDetection_ValueTypeKey(null).GetAwaiter().GetResult();


			//DictionaryDev<int, string> dict = new DictionaryDev<int, string>(5, EqualityComparer<int>.Default);

			//dict.Add(1, "one");
			//dict.Add(2, "two");
			//dict.Add(3, "three");
			//dict.Add(4, "four");
			//dict.Add(5, "five");
			//dict.Add(6, "six");
			//dict.Add(7, "seven");
			//dict.Add(8, "eight");

			//dict.Remove(2);
			//dict.Remove(3);
			//dict.Remove(5);

			//dict.RemoveAll(_ => _==2 ||_==3 || _==5);

			//foreach (KeyValuePair<int, string> pair in dict)
			//{
			//	Console.WriteLine(pair.Key.ToString());
			//}

			//BenchmarkRunner.Run<Benchmarks>();


			Console.ReadLine();
		}
    }

	
	
	[CoreJob]
	[MemoryDiagnoser]
	[DisassemblyDiagnoser(printAsm: true, printSource: true,recursiveDepth:4)]
	public class Benchmarks
	{
		private int[] keys;
		private object[] values;
		private DictionaryDev<int,object> devDict;
		private DictionaryRef<int,object> refDict;

		[GlobalSetup]
		public void Setup()
		{
			keys=new int[10000];
			values=new object[keys.Length];
			for (int index = 0; index<keys.Length; index+=1)
			{
				keys[index]=index;
				values[index]=new object();
			}
			devDict=new DictionaryDev<int, object>(keys.Length);
			refDict=new DictionaryRef<int, object>(keys.Length);
			//for (int index = 0; index<keys.Length; index+=1)
			//{
			//	devDict.Add(keys[index], values[index]);
			//	refDict.Add(keys[index], values[index]);
			//}
		}

		[IterationSetup]
		private void InstanceSetup()
		{
			for (int index = 0; index<keys.Length; index+=1)
			{
				devDict.Add(keys[index], values[index]);
				refDict.Add(keys[index], values[index]);
			}
		}

		[IterationCleanup]
		private void IterationCleanup()
		{
			devDict.Clear();
			refDict.Clear();
		}

		//[Benchmark]
		//public int AddDev()
		//{
		//	devDict.Clear();
		//	int count = 0;
		//	for (int index = 0; index<keys.Length; index+=1)
		//	{
		//		devDict.Add(keys[index], values[index]);
		//		count+=1;
		//	}
		//	return count;
		//}

		//[Benchmark(Baseline = true)]
		//public int AddRef()
		//{
		//	refDict.Clear();
		//	int count = 0;
		//	for (int index = 0; index<keys.Length; index+=1)
		//	{
		//		refDict.Add(keys[index], values[index]);
		//		count+=1;
		//	}
		//	return count;
		//}
		[Benchmark(Baseline = true)]
		public int RemoveOddRef()
		{
			var remove = new List<int>(refDict.Count);
			foreach (var key in refDict.Keys)
			{
				if ((key & 1)==1)
				{
					remove.Add(key);
				}
			}
			foreach (var key in remove)
			{
				refDict.Remove(key);
			}
			return remove.Count;
		}

		[Benchmark]
		public int RemoveOddDev()
		{
			return devDict.RemoveAll(
				(KeyValuePair<int,object> pair) => (pair.Key&1)==1 && !object.ReferenceEquals(pair.Value, null)
			);
		}
	}
}
