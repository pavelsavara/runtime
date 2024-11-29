// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WasiHttpWorld;
using WasiHttpWorld.wit.imports.wasi.http.v0_2_0;
using WasiHttpWorld.wit.imports.wasi.io.v0_2_0;
using static WasiHttpWorld.wit.imports.wasi.http.v0_2_0.ITypes;
using static WasiHttpWorld.wit.imports.wasi.io.v0_2_0.IStreams;

namespace System.Net.Http
{
    internal static class WasiHttpInterop
    {
        public static Task RegisterWasiPollable(IPoll.Pollable pollable, CancellationToken cancellationToken)
        {
            var handle = pollable.Handle;

            // this will effectively neutralize Dispose() of the Pollable()
            // because in the CoreLib we create another instance, which will dispose it
            pollable.Handle = 0;
            GC.SuppressFinalize(pollable);

            return CallRegisterWasiPollableHandle((Thread)null!, handle, true, cancellationToken);

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "RegisterWasiPollableHandle")]
            static extern Task CallRegisterWasiPollableHandle(Thread t, int handle, bool ownsPollable, CancellationToken cancellationToken);
        }
    }
}
