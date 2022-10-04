// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Sample
{
    public partial class Test
    {
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Sample.Test", "Wasm.Browser.Bench.Sample")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Sample.AppStartTask.FrameApp", "Wasm.Browser.Bench.Sample")]
        public static int Main(string[] args)
        {
            return 0;
        }
    }
}
