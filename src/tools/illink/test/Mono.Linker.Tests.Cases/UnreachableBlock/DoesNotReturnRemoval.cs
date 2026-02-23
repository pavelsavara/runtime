using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class DoesNotReturnRemoval
	{
		public static void Main ()
		{
			TestSimpleDoesNotReturn ();
			TestDoesNotReturnInBranch (true);
			TestDoesNotReturnWithReturnValue ();
			TestMultipleDoesNotReturnCalls ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.DoesNotReturnRemoval::ThrowAlways()",
			"ldnull",
			"throw",
		})]
		static void TestSimpleDoesNotReturn ()
		{
			ThrowAlways ();
			NeverReached ();
		}

		[Kept]
		[ExpectBodyModified]
		static void TestDoesNotReturnInBranch (bool condition)
		{
			if (condition) {
				ThrowAlways ();
				NeverReached ();
			}

			AlwaysReached ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.DoesNotReturnRemoval::ThrowAlways()",
			"ldnull",
			"throw",
		})]
		static int TestDoesNotReturnWithReturnValue ()
		{
			ThrowAlways ();
			NeverReached ();
			return 42;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"call System.Void Mono.Linker.Tests.Cases.UnreachableBlock.DoesNotReturnRemoval::ThrowAlways()",
			"ldnull",
			"throw",
		})]
		static void TestMultipleDoesNotReturnCalls ()
		{
			ThrowAlways ();
			ThrowAlways ();
			NeverReached ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DoesNotReturnAttribute))]
		[DoesNotReturn]
		static void ThrowAlways ()
		{
			throw new System.NotSupportedException ();
		}

		static void NeverReached ()
		{
		}

		[Kept]
		static void AlwaysReached ()
		{
		}
	}
}
