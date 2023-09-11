using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Azure.Storage.Blobs.Models;

static class ExtensionMethods
{
    /// <summary>
    /// Parses a string containing text file content into a list of strings, where each string represents a line of the file.
    /// </summary>
    /// <param name="fileContent">The string containing the file content to parse.</param>
    /// <returns>A list of strings, where each string represents a line of the file.</returns>
    public static List<string> ParseTXT(this string fileContent)
    {
        return fileContent
            .Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
            .ToList();
    }

    /// <summary>
    /// Parses a string containing WebVTT file content into a list of strings, where each string represents a caption block of the file.
    /// </summary>
    /// <param name="fileContent">The string containing the file content to parse.</param>
    /// <returns>A list of strings, where each string represents a caption block of the file.</returns>
    public static List<string> ParseVTT(this string fileContent)
    {
        return fileContent
            .Split(
                new[] { $"{Environment.NewLine}{Environment.NewLine}" },
                StringSplitOptions.RemoveEmptyEntries
            )
            .Select(f =>
            {
                var lines = f.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                    .Where(l => !l.Contains("-->"))
                    .Select(l => l.Replace("<v ", "").Replace("</v>", "").Replace(">", ": "))
                    .ToList();
                return string.Join(Environment.NewLine, lines);
            })
            .ToList();
    }

    /// <summary>
    /// Removes any special characters from the file name and returns the resulting string.
    /// </summary>
    /// <param name="name">The file name to remove special characters from.</param>
    /// <returns>The file name with any special characters removed.</returns>
    public static string RemoveSpecialCharacters(this string name)
    {
        var pattern = "[^a-zA-Z0-9]+";
        var replacement = "";
        var output = Regex.Replace(Path.GetFileNameWithoutExtension(name), pattern, replacement);
        return output;
    }

    /// <summary>
    /// Converts a stream to a string.
    /// </summary>
    /// <param name="stream">The stream to convert to a string.</param>
    /// <returns>The string representation of the stream.</returns>
    public static string StreamToString(this Stream stream)
    {
        stream.Position = 0;
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }
}
