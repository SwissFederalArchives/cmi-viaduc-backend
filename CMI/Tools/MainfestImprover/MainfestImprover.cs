using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Schema;
using Serilog;

namespace CMI.Tools.ManifestImprover
{
    internal class ManifestImprover : IDisposable
    {
        public void Improve(string sourceFolder, string pattern, string substitution, bool isTestRun)
        {
            var sourceFiles = new DirectoryInfo(sourceFolder).GetFiles("*.json", SearchOption.AllDirectories);
            long counter = 0;
            long progressCounter = 0;
            long totalFileCount = sourceFiles.Length;

            ValidateReplacement(pattern, substitution);
            
            var options = RegexOptions.Multiline;
            var regex = new Regex(pattern, options);

            foreach (var sourceFile in sourceFiles)
            {
                try
                {
                    progressCounter++;
                    var percent = totalFileCount > 0 ? (int) ((progressCounter / (double) totalFileCount) * 100) : 0;
                    Log.Information("{percent}% done. Checking file {fullname}", percent,  sourceFile.FullName);

                    var manifestText = File.ReadAllText(sourceFile.FullName);

                    if (regex.IsMatch(manifestText))
                    {
                        counter++;
                        Log.Information("Found a match in: {SourceFileFullName}", sourceFile.FullName);

                        var matches = regex.Matches(manifestText);
                        var groups = matches[0].Groups;

                        if (isTestRun == false)
                        {
                            var result = regex.Replace(manifestText, substitution);
                            File.WriteAllText(sourceFile.FullName, result);
                            Log.Information("Made substitution in: {SourceFileFullName}", sourceFile.FullName);
                        }

                    }

                }
                catch (Exception e)
                {
                    Log.Error(e, "Unexpected error in file {SourceFileFullName}", sourceFile.FullName);
                }
            }

            Log.Information(
                isTestRun
                    ? "Was test run. Would have changed {Counter} files from {SourceFilesLength} Json files"
                    : "Changed {Counter} files from {SourceFilesLength} Json files", counter, sourceFiles.Length);
            Log.Information("#########################################################################");
            Log.Information("#########################################################################");
        }

        static void ValidateReplacement(string pattern, string replacement)
        {
            var regex = new Regex(pattern);

            var groupCount = regex.GetGroupNumbers().Length - 1;

            foreach (Match m in Regex.Matches(replacement, @"\$(\d+)"))
            {
                int group = int.Parse(m.Groups[1].Value);
                if (group > groupCount)
                {
                    throw new InvalidOperationException(
                        $"Replacement refers to non-existing group ${group} (max is ${groupCount})."
                    );
                }
            }
        }

        public void Dispose()
        {
            // TODO release managed resources here
        }
    }
}
