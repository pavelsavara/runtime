// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WasiHttpWorld.wit.exports.wasi.http.v0_2_0;
using WasiHttpWorld.wit.imports.wasi.http.v0_2_0;

namespace Wasi.HttpServer.App;

public class Program
{
    public static WebApplication? App;

    public static void Init()
    {
        if (App != null)
        {
            return;
        }

        var builder = WebApplication.CreateSlimBuilder(new string[0]);
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(
                0,
                AppJsonSerializerContext.Default
            );
        });
        builder.Services.AddSingleton<IServer, WasiHttpServer>();
        builder.Logging.ClearProviders();
        builder
            .Logging.AddProvider(new WasiLoggingProvider())
            .AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.Error);

        App = builder.Build();
        App.UseStaticFiles();

        App.MapGet("/", () => "Hello, world! See also: /weatherforecast and /mystaticpage.html");

        App.MapGet(
            "/weatherforecast",
            () =>
            {
                var summaries = new[]
                {
                    "Freezing",
                    "Bracing",
                    "Chilly",
                    "Cool",
                    "Mild",
                    "Warm",
                    "Balmy",
                    "Hot",
                    "Sweltering",
                    "Scorching"
                };
                var forecast = Enumerable
                    .Range(1, 5)
                    .Select(index => new Forecast()
                    {
                        Date = DateTime.Now.AddDays(index),
                        TempC = Random.Shared.Next(-20, 55),
                        Summary = summaries[Random.Shared.Next(summaries.Length)]
                    })
                    .ToArray();
                return forecast;
            }
        );

        App.MapPost(
            "/echo",
            async context =>
            {
                context.Response.Headers.ContentType = context.Request.Headers.ContentType;
                await context.Request.Body.CopyToAsync(context.Response.Body);
            }
        );

        App.MapPost("/data", (Message message) => message.Greeting);
    }

}

public class Message
{
    public string Greeting { get; set; }
}

public class Forecast
{
    public DateTime Date { get; set; }
    public int TempC { get; set; }
    public string Summary { get; set; }
}

[JsonSerializable(typeof(Forecast[]))]
[JsonSerializable(typeof(Message))]
internal partial class AppJsonSerializerContext : JsonSerializerContext { }
