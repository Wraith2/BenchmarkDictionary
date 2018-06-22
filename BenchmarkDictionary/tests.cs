// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Wraith.Collections.Generic;
using Xunit;

namespace Generic.DictionaryDev
{
	public class DictionaryDevConcurrentAccessDetectionTests
	{
		private async Task DictionaryDevConcurrentAccessDetection<TKey, TValue>(DictionaryDev<TKey, TValue> dictionary, bool isValueType, object comparer, Action<DictionaryDev<TKey, TValue>> add, Action<DictionaryDev<TKey, TValue>> get, Action<DictionaryDev<TKey, TValue>> remove, Action<DictionaryDev<TKey, TValue>> removeOutParam, Action<DictionaryDev<TKey, TValue>> removeAll)
		{
			Task task = Task.Factory.StartNew(() =>
			{
				// Get the DictionaryDev into a corrupted state, as if it had been corrupted by concurrent access.
				// We this deterministically by clearing the _entries array using reflection;
				// this means that every Entry struct has a 'next' field of zero, which causes the infinite loop
				// that we want DictionaryDev to break out of
				FieldInfo entriesType = dictionary.GetType().GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance);
				Array entriesInstance = (Array)entriesType.GetValue(dictionary);
				//Array entryArray = (Array)Activator.CreateInstance(entriesInstance.GetType(), new object[] { entriesInstance.Length });
				//entriesType.SetValue(dictionary, entryArray);

				var entryType = entriesType.GetValue(dictionary).GetType().GetElementType();
				var nextField = entryType.GetField("next");
				int firstNext = 0;
				for (int index = 0; index<entriesInstance.Length; index+=1)
				{
					//int next = (int)nextField.GetValue(entriesInstance.GetValue(index));
					//if (next!=0)
					//{
					//	if (firstNext==0)
					//	{
					//		firstNext=next;
					//	}
					//	else
					//	{
					//		nextField.SetValue(entriesInstance.GetValue(index), firstNext);
					//	}
					//}
					nextField.SetValue(entriesInstance.GetValue(index), 0);
				}

				//Assert.Equal(comparer, dictionary.GetType().GetField("_comparer", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(dictionary));
				//Assert.Equal(isValueType, dictionary.GetType().GetGenericArguments()[0].IsValueType);
				//Assert.Equal("ThrowInvalidOperationException_ConcurrentOperationsNotSupported", Assert.Throws<InvalidOperationException>(() => add(dictionary)).TargetSite.Name);
				//Assert.Equal("ThrowInvalidOperationException_ConcurrentOperationsNotSupported", Assert.Throws<InvalidOperationException>(() => get(dictionary)).TargetSite.Name);
				//Assert.Equal("ThrowInvalidOperationException_ConcurrentOperationsNotSupported", Assert.Throws<InvalidOperationException>(() => remove(dictionary)).TargetSite.Name);
				//Assert.Equal("ThrowInvalidOperationException_ConcurrentOperationsNotSupported", Assert.Throws<InvalidOperationException>(() => removeOutParam(dictionary)).TargetSite.Name);

				Assert.Equal("ThrowInvalidOperationException_ConcurrentOperationsNotSupported", Assert.Throws<InvalidOperationException>(() => removeAll(dictionary)).TargetSite.Name);
			}, TaskCreationOptions.LongRunning);

			// If DictionaryDev regresses, we do not want to hang here indefinitely
			Assert.True((await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(60))) == task) && task.IsCompletedSuccessfully);
		}

		//[Theory]
		//[InlineData(null)]
		//[InlineData(typeof(CustomEqualityComparerInt32ValueType))]
		public async Task DictionaryDevConcurrentAccessDetection_ValueTypeKey(Type comparerType)
		{
			IEqualityComparer<int> customComparer = null;

			DictionaryDev<int, int> dic = comparerType == null ?
				new DictionaryDev<int, int>() :
				new DictionaryDev<int, int>((customComparer = (IEqualityComparer<int>)Activator.CreateInstance(comparerType)));

			dic.Add(1, 1);
			dic.Add(4, 2);

			await DictionaryDevConcurrentAccessDetection(dic,
				typeof(int).IsValueType,
				customComparer,
				d => d.Add(1, 1),
				d => { var v = d[1]; },
				d => d.Remove(1),
				d => d.Remove(1, out int value),
				d => d.RemoveAll(kvp => true)
			);
		}

		//[Theory]
		//[InlineData(null)]
		//[InlineData(typeof(CustomEqualityComparerDummyRefType))]
		public async Task DictionaryDevConcurrentAccessDetection_ReferenceTypeKey(Type comparerType)
		{
			IEqualityComparer<DummyRefType> customComparer = null;

			DictionaryDev<DummyRefType, DummyRefType> dic = comparerType == null ?
				new DictionaryDev<DummyRefType, DummyRefType>() :
				new DictionaryDev<DummyRefType, DummyRefType>((customComparer = (IEqualityComparer<DummyRefType>)Activator.CreateInstance(comparerType)));

			var keyValueSample = new DummyRefType() { Value = 1 };

			dic.Add(keyValueSample, keyValueSample);

			await DictionaryDevConcurrentAccessDetection(dic,
				typeof(DummyRefType).IsValueType,
				customComparer,
				d => d.Add(keyValueSample, keyValueSample),
				d => { var v = d[keyValueSample]; },
				d => d.Remove(keyValueSample),
				d => d.Remove(keyValueSample, out DummyRefType value),
				d => d.RemoveAll(kvp => true)
			);
		}
	}

	// We use a custom type instead of string because string use optimized comparer https://github.com/dotnet/coreclr/blob/master/src/System.Private.CoreLib/shared/System/Collections/Generic/DictionaryDev.cs#L79
	// We want to test case with _comparer = null
	[DebuggerStepThrough]
	class DummyRefType
	{
		public int Value { get; set; }
		public override bool Equals(object obj)
		{
			return ((DummyRefType)obj).Equals(this.Value);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}
	[DebuggerStepThrough]
	class CustomEqualityComparerDummyRefType : EqualityComparer<DummyRefType>
	{
		public override bool Equals(DummyRefType x, DummyRefType y)
		{
			return x.Value == y.Value;
		}

		public override int GetHashCode(DummyRefType obj)
		{
			return obj.GetHashCode();
		}
	}

	[DebuggerStepThrough]
	class CustomEqualityComparerInt32ValueType : EqualityComparer<int>
	{
		public override bool Equals(int x, int y)
		{
			return EqualityComparer<int>.Default.Equals(x, y);
		}

		public override int GetHashCode(int obj)
		{
			return EqualityComparer<int>.Default.GetHashCode(obj);
		}
	}
}
