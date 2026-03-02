using System.Collections.Generic;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class TypeOfComparisonConcreteTypes
	{
		public static void Main ()
		{
			TestConcreteTypeofEqualityTrue ();
			TestConcreteTypeofEqualityFalse ();
			TestConcreteTypeofInequalityTrue ();
			TestConcreteTypeofInequalityFalse ();
			TestConcreteTypeofReferenceTypes ();
			TestConcreteTypeofGenericTypes ();
			TestTypeofEqualityThroughProperty ();
			TestTypeofInequalityThroughProperty ();
			TestOpenGenericTypeofNotFolded<int> ();
			TestOpenGenericNestedTypeofNotFolded<int> ();
			TestMultipleBranches ();
		}

		[Kept]
		[ExpectBodyModified]
		static void TestConcreteTypeofEqualityTrue ()
		{
			if (typeof (int) == typeof (int)) {
				TypeofReached_1 ();
			} else {
				TypeofNeverReached_1 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestConcreteTypeofEqualityFalse ()
		{
			if (typeof (int) == typeof (byte)) {
				TypeofNeverReached_2 ();
			} else {
				TypeofReached_2 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestConcreteTypeofInequalityTrue ()
		{
			if (typeof (int) != typeof (byte)) {
				TypeofReached_3 ();
			} else {
				TypeofNeverReached_3 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestConcreteTypeofInequalityFalse ()
		{
			if (typeof (int) != typeof (int)) {
				TypeofNeverReached_4 ();
			} else {
				TypeofReached_4 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestConcreteTypeofReferenceTypes ()
		{
			if (typeof (string) == typeof (string)) {
				TypeofReached_5 ();
			} else {
				TypeofNeverReached_5 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestConcreteTypeofGenericTypes ()
		{
			if (typeof (List<int>) == typeof (List<byte>)) {
				TypeofNeverReached_6 ();
			} else {
				TypeofReached_6 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestTypeofEqualityThroughProperty ()
		{
			if (IsInt) {
				TypeofNeverReached_7 ();
			} else {
				TypeofReached_7 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestTypeofInequalityThroughProperty ()
		{
			if (IsNotInt) {
				TypeofReached_8 ();
			} else {
				TypeofNeverReached_8 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestOpenGenericTypeofNotFolded<T> ()
		{
			if (typeof (T) == typeof (int)) {
				TypeofReached_9 ();
			} else {
				TypeofReached_10 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestOpenGenericNestedTypeofNotFolded<T> ()
		{
			if (typeof (List<T>) == typeof (List<int>)) {
				TypeofReached_12 ();
			} else {
				TypeofReached_13 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestMultipleBranches ()
		{
			if (typeof (int) == typeof (byte)) {
				TypeofNeverReached_11 ();
			} else if (typeof (int) == typeof (int)) {
				TypeofReached_11 ();
			}
		}

		static bool IsInt {
			get {
				return typeof (int) == typeof (byte);
			}
		}

		static bool IsNotInt {
			get {
				return typeof (int) != typeof (byte);
			}
		}

		[Kept]
		static void TypeofReached_1 () { }

		[Kept]
		static void TypeofReached_2 () { }

		[Kept]
		static void TypeofReached_3 () { }

		[Kept]
		static void TypeofReached_4 () { }

		[Kept]
		static void TypeofReached_5 () { }

		[Kept]
		static void TypeofReached_6 () { }

		[Kept]
		static void TypeofReached_7 () { }

		[Kept]
		static void TypeofReached_8 () { }

		[Kept]
		static void TypeofReached_9 () { }

		[Kept]
		static void TypeofReached_10 () { }

		[Kept]
		static void TypeofReached_11 () { }

		[Kept]
		static void TypeofReached_12 () { }

		[Kept]
		static void TypeofReached_13 () { }

		static void TypeofNeverReached_1 () { }

		static void TypeofNeverReached_2 () { }

		static void TypeofNeverReached_3 () { }

		static void TypeofNeverReached_4 () { }

		static void TypeofNeverReached_5 () { }

		static void TypeofNeverReached_6 () { }

		static void TypeofNeverReached_7 () { }

		static void TypeofNeverReached_8 () { }

		static void TypeofNeverReached_11 () { }
	}
}
