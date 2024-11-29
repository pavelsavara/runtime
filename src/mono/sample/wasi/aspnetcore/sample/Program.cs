using WasiHttpWorld.wit.exports.wasi.http.v0_2_0;
using WasiHttpWorld.wit.imports.wasi.http.v0_2_0;

namespace Wasi.HttpServer.Sample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
        }
    }
}

namespace WasiHttpWorld.wit.exports.wasi.http.v0_2_0
{
    internal static class IncomingHandlerImpl
    {
        internal static void Handle(ITypes.IncomingRequest request, ITypes.ResponseOutparam response)
        {
            Console.WriteLine("IncomingHandlerImpl.Handle");
        }
    }
}
