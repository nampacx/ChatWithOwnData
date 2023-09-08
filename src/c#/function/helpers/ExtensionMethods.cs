using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Azure.Storage.Blobs.Models;

static class ExtensionMethods
{
    public static List<string> ParseTXT(this string fileContent)
    {
        return fileContent.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    public static List<string> ParseVTT(this string fileContent)
    {
        return fileContent.Split(new[] { $"{Environment.NewLine}{Environment.NewLine}" }, StringSplitOptions.RemoveEmptyEntries).Select(f =>
        {
            var lines = f.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Where(l => !l.Contains("-->")).Select(l => l.Replace("<v ", "").Replace("</v>", "").Replace(">", ": ")).ToList();
            return string.Join(Environment.NewLine, lines);
        }).ToList();
    }

    public static string RemoveSpecialCharacters(this string name)
    {
        var pattern = "[^a-zA-Z0-9]+";
        var replacement = "";
        var output = Regex.Replace(Path.GetFileNameWithoutExtension(name), pattern, replacement);
        return output;
    }

    public static string StreamToString(this Stream stream)
    {
        stream.Position = 0;
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }
}
