// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Wasi.HttpServer.App;

namespace Wasi.HttpServer.Sample;

public class SampleProgram
{
    // this is just dummy wrapper, this is main not called
    public static void Main(string[] args)
    {
        Program.Init();
    }
}
