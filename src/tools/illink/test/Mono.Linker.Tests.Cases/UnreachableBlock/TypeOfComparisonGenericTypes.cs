using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class TypeOfComparisonGenericTypes
	{
		public static void Main ()
		{
			TestMethodLevelGenericDeadBranch<int> ();
			TestMethodLevelGenericDeadBranch<byte> ();
			TestTypeLevelGenericDeadBranch ();
			TestMethodLevelGenericAliveBranch<int> ();
			TestMethodLevelGenericAliveBranch<byte> ();
			TestMultipleDeadBranches<int> ();
			TestMultipleDeadBranches<byte> ();
		}

		[Kept]
		[ExpectBodyModified]
		static void TestMethodLevelGenericDeadBranch<T> ()
		{
			// typeof(T)==typeof(float) is false for BOTH int and byte → fold to false
			if (typeof (T) == typeof (float)) {
				GenericDeadBranch_1 ();
			} else {
				GenericReached_1 ();
			}
		}

		[Kept]
		static void TestTypeLevelGenericDeadBranch ()
		{
			MyGenericType<int>.TestTypeofDeadBranch ();
			MyGenericType<byte>.TestTypeofDeadBranch ();
		}

		[Kept]
		static void TestMethodLevelGenericAliveBranch<T> ()
		{
			// typeof(T)==typeof(int) is true for int, false for byte → disagreement → NOT folded
			if (typeof (T) == typeof (int)) {
				GenericReached_2 ();
			} else {
				GenericReached_3 ();
			}
		}

		[Kept]
		[ExpectBodyModified]
		static void TestMultipleDeadBranches<T> ()
		{
			// With instantiations {int, byte}:
			// float branch: false for both → dead → fold
			// double branch: false for both → dead → fold
			// int branch: true for int, false for byte → alive → keep
			// byte branch: false for int, true for byte → alive → keep
			if (typeof (T) == typeof (float)) {
				GenericDeadBranch_3 ();
			} else if (typeof (T) == typeof (double)) {
				GenericDeadBranch_4 ();
			} else if (typeof (T) == typeof (int)) {
				GenericReached_4 ();
			} else if (typeof (T) == typeof (byte)) {
				GenericReached_5 ();
			}
		}

		[Kept]
		class MyGenericType<T>
		{
			[Kept]
			[ExpectBodyModified]
			public static void TestTypeofDeadBranch ()
			{
				// typeof(T)==typeof(long) is false for BOTH int and byte → fold to false
				if (typeof (T) == typeof (long)) {
					GenericDeadBranch_2 ();
				} else {
					GenericReached_6 ();
				}
			}
		}

		// Methods that should be reachable (Kept because they're marked during initial pass)
		[Kept]
		static void GenericReached_1 () { }

		[Kept]
		static void GenericReached_2 () { }

		[Kept]
		static void GenericReached_3 () { }

		[Kept]
		static void GenericReached_4 () { }

		[Kept]
		static void GenericReached_5 () { }

		[Kept]
		static void GenericReached_6 () { }

		// Methods from dead branches — still marked from initial pass (marking happens before deferred typeof optimization)
		[Kept]
		static void GenericDeadBranch_1 () { }

		[Kept]
		static void GenericDeadBranch_2 () { }

		[Kept]
		static void GenericDeadBranch_3 () { }

		[Kept]
		static void GenericDeadBranch_4 () { }
	}
}
