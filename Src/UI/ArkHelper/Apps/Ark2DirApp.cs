using ArkHelper.Helpers;
using ArkHelper.Options;
using Mackiloha.Ark;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static Mackiloha.FileHelper;

namespace ArkHelper.Apps
{
    public class Ark2DirApp
    {
        protected readonly IScriptHelper ScriptHelper;

        public Ark2DirApp(IScriptHelper scriptHelper)
        {
            ScriptHelper = scriptHelper;
        }

        private string CombinePath(string basePath, string path)
        {
            // Consistent slash
            basePath = FixSlashes(basePath ?? "");
            path = FixSlashes(path ?? "");

            path = ReplaceDotsInPath(path);
            return Path.Combine(basePath, path);
        }

        private string ReplaceDotsInPath(string path)
        {
            var dotRegex = new Regex(@"[.]+[\/\\]");

            if (dotRegex.IsMatch(path))
            {
                // Replaces dotdot path
                path = dotRegex.Replace(path, x => $"({x.Value[..^1]}){x.Value.Last()}");
            }

            return path;
        }

        private string ExtractEntry(Archive ark, ArkEntry entry, string filePath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                _ = Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }

            using (var fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using var stream = ark.GetArkEntryFileStream(entry);
                stream.CopyTo(fs);
            }

            return filePath;
        }

        public void Parse(Ark2DirOptions op)
        {
            Archive ark;
            int arkVersion;
            bool arkEncrypted;

            if (Directory.Exists(op.InputPath))
            {
                // Open as directory
                ark = ArkFileSystem.FromDirectory(op.InputPath);

                // TODO: Get from args probably
            }
            else
            {
                // Open as ark
                var arkFile = ArkFile.FromFile(op.InputPath);
                arkVersion = (int)arkFile.Version;
                arkEncrypted = arkFile.Encrypted;

                ark = arkFile;
            }

            foreach (var arkEntry in ark.Entries)
            {
                string filePath = ExtractEntry(ark, arkEntry, CombinePath(op.OutputPath, arkEntry.FullPath));
                Console.WriteLine($"Wrote \"{filePath}\"");
            }

        }
    }
}
