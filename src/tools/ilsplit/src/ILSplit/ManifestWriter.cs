// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ILSplit;

internal static class ManifestWriter
{
    public static void Write(string originalAssemblyName, List<Cluster> clusters, string outputDir)
    {
        var manifest = new Manifest
        {
            Version = 1,
            OriginalAssembly = originalAssemblyName,
            Clusters = clusters.Select(c => new ClusterInfo
            {
                Name = $"{originalAssemblyName}.{c.Index}.dll",
                Eager = c.IsEager,
                Types = c.Types.Select(t => t.FullName).ToList(),
                SizeBytes = c.EstimatedSize,
            }).ToList(),
            TypeToCluster = clusters
                .SelectMany(c => c.Types.Select(t => (Type: t.FullName, Cluster: $"{originalAssemblyName}.{c.Index}.dll")))
                .ToDictionary(x => x.Type, x => x.Cluster),
        };

        string path = Path.Combine(outputDir, "ilsplit-manifest.json");
        string json = JsonSerializer.Serialize(manifest, ManifestJsonContext.Default.Manifest);
        File.WriteAllText(path, json);
    }
}

internal sealed class Manifest
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("originalAssembly")]
    public string OriginalAssembly { get; set; } = string.Empty;

    [JsonPropertyName("clusters")]
    public List<ClusterInfo> Clusters { get; set; } = new();

    [JsonPropertyName("typeToCluster")]
    public Dictionary<string, string> TypeToCluster { get; set; } = new();
}

internal sealed class ClusterInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("eager")]
    public bool Eager { get; set; }

    [JsonPropertyName("types")]
    public List<string> Types { get; set; } = new();

    [JsonPropertyName("sizeBytes")]
    public int SizeBytes { get; set; }
}

[JsonSerializable(typeof(Manifest))]
internal sealed partial class ManifestJsonContext : JsonSerializerContext
{
}
