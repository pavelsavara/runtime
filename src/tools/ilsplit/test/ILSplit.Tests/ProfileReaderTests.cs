// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace ILSplit.Tests;

public class ProfileReaderTests
{
    [Fact]
    public void Read_ParsesHotClassNames()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, """
                # Comment line
                System.String
                System.Object

                System.Int32
                """);

            var result = ProfileReader.Read(tempFile);

            Assert.Contains("System.String", result);
            Assert.Contains("System.Object", result);
            Assert.Contains("System.Int32", result);
            Assert.Equal(3, result.Count);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Read_SkipsCommentsAndEmptyLines()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, """
                # This is a comment
                # Another comment

                System.String

                """);

            var result = ProfileReader.Read(tempFile);

            Assert.Single(result);
            Assert.Contains("System.String", result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
