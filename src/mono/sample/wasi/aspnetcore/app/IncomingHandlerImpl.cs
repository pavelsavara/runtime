// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Wasi.HttpServer.App;
using WasiHttpWorld.wit.exports.wasi.http.v0_2_0;
using WasiHttpWorld.wit.imports.wasi.http.v0_2_0;

namespace WasiHttpWorld.wit.exports.wasi.http.v0_2_0;

internal static class IncomingHandlerImpl
{
    public static void Handle(ITypes.IncomingRequest request, ITypes.ResponseOutparam responseOut)
    {
        Console.WriteLine("IncomingHandlerImpl.Handle");

        Program.Init();

        Func<Task> task = async () =>
        {
            await Program.App.StartAsync();
            await WasiHttpServer.HandleRequestAsync(request, responseOut);
        };
        PollWasiEventLoopUntilResolved(task());

        Console.WriteLine("IncomingHandlerImpl.Handle done");
    }

    public static int PollWasiEventLoopUntilResolved(Task mainTask)
    {
        return PollWasiEventLoopUntilResolvedVoid((Thread)null!, mainTask);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "PollWasiEventLoopUntilResolvedVoid")]
        static extern int PollWasiEventLoopUntilResolvedVoid(Thread t, Task mainTask);
    }
}
