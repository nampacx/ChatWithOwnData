using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

/// <summary>
/// Helper class for chunking text files into smaller pieces.
/// </summary>
public class Chunker
{
    private const int MaxChunkSize = 3;

    /// <summary>
    /// Gets the chunks of a text file.
    /// </summary>
    /// <param name="fileContent">The content of the file.</param>
    /// <param name="fileExtension">The extension of the file.</param>
    /// <param name="log">The logger instance.</param>
    /// <returns>A list of chunks.</returns>
    public static List<string> GetChunks(string fileContent, string fileExtension, ILogger log)
    {
        var lines = new List<string>();
        if (fileExtension == ".txt")
        {
            lines = fileContent.ParseTXT();
            log.LogInformation($"Parsed {lines.Count} lines from TXT file");
        }
        else if (fileExtension == ".vtt")
        {
            lines = fileContent.ParseVTT();
            log.LogInformation($"Parsed {lines.Count} lines from VTT file");
        }
        else
        {
            log.LogInformation("File extension not supported");
            return null;
        }

        return CreateChunks(lines);
    }

    private static List<string> CreateChunks(List<string> lines)
    {
        var chunks = new List<string>();
        for (var i = 0; i < lines.Count; i++)
        {
            chunks.Add(string.Join(Environment.NewLine, lines.Skip(i).Take(MaxChunkSize)));
        }

        return chunks;
    }
}
