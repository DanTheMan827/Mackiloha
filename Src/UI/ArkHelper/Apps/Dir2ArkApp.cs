using ArkHelper.Helpers;
using ArkHelper.Options;
using Mackiloha;
using Mackiloha.Ark;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ArkHelper.Apps
{
    public class Dir2ArkApp
    {
        protected readonly ICacheHelper CacheHelper;
        protected readonly IScriptHelper ScriptHelper;

        public Dir2ArkApp(ICacheHelper cacheHelper, IScriptHelper scriptHelper)
        {
            CacheHelper = cacheHelper;
            ScriptHelper = scriptHelper;
        }

        public void Parse(Dir2ArkOptions op)
        {
            var dtaRegex = new Regex("(?i).dta$");
            var genPathedFile = new Regex(@"(?i)gen[\/][^\/]+$");
            var dotRegex = new Regex(@"\([.]+\)/");
            var forgeScriptRegex = new Regex("(?i).((dta)|(fusion)|(moggsong)|(script))$");
            uint arkPartSizeLimit = uint.MaxValue;

            if (!Directory.Exists(op.InputPath))
            {
                throw new DirectoryNotFoundException($"Can't find directory \"{op.InputPath}\"");
            }

            string arkDir = Path.GetFullPath(op.OutputPath);

            // Set encrypted data
            if (op.ArkVersion < 3)
            {
                // Don't encrypt hdr-less arks
                op.Encrypt = false;
                op.EncryptKey = default;
            }
            else if (op.Encrypt && !op.EncryptKey.HasValue)
            {
                op.EncryptKey = 0x5A_4C_4F_4C;
            }
            else if (op.EncryptKey.HasValue)
            {
                // Don't encrypt unless explicitly stated
                op.EncryptKey = default;
            }

            // Load ark cache if path given
            bool usingCache = false;
            if (op.CachePath is not null)
            {
                CacheHelper.LoadCache(op.CachePath, op.ArkVersion, op.Encrypt);
                usingCache = true;
            }

            // Create directory if it doesn't exist
            if (!Directory.Exists(arkDir))
            {
                _ = Directory.CreateDirectory(arkDir);
            }

            // If name is all caps, match extension
            string hdrExt = op.ArkVersion >= 3
                ? ".hdr"
                : ".ark";

            if (op.ArkName.All(c => char.IsUpper(c)))
            {
                hdrExt = hdrExt.ToUpper();
            }

            // Create ark
            string hdrPath = Path.Combine(arkDir, $"{op.ArkName}{hdrExt}");
            var ark = ArkFile.Create(hdrPath, ArkVersion.V9, -1865432917); // ps3: -967906000, ps4: -1865432917
            ark.ForcedXor = 0xFF;
            ark.ForcedExtraFlag = 0xDDB682F0; // CSettings::mbPS4 ? 0xDDB682F0 : 0x7D401F60

            string[] files = Directory.GetFiles(op.InputPath, "*", SearchOption.AllDirectories);

            uint currentPartSize = 0u;

            foreach (string file in files)
            {
                string internalPath = FileHelper.GetRelativePath(file, op.InputPath)
                    .Replace("\\", "/"); // Must be "/" in ark

                string inputFilePath = file;

                if (dotRegex.IsMatch(internalPath))
                {
                    internalPath = dotRegex.Replace(internalPath, x => $"{x.Value.Substring(1, x.Length - 3)}/");
                }

                // Check part limit
                long fileSizeLong = new FileInfo(inputFilePath).Length;
                uint fileSize = (uint)fileSizeLong;
                uint potentialPartSize = currentPartSize + fileSize;

                if (fileSizeLong > uint.MaxValue)
                {
                    throw new NotSupportedException($"File size above 4GB is unsupported for \"{file}\"");
                }
                else if ((int)ark.Version >= 3 && potentialPartSize >= arkPartSizeLimit)
                {
                    // Kind of hacky but multiple part writing isn't implemented in commit changes yet
                    ark.CommitChanges(true);
                    ark.AddAdditionalPart();

                    currentPartSize = 0;
                }

                string fileName = Path.GetFileName(internalPath);
                string dirPath = Path.GetDirectoryName(internalPath).Replace("\\", "/"); // Must be "/" in ark

                var pendingEntry = new PendingArkEntry(fileName, dirPath)
                {
                    LocalFilePath = inputFilePath
                };

                ark.AddPendingEntry(pendingEntry);
                Console.WriteLine($"Added {pendingEntry.FullPath}");

                currentPartSize += fileSize;
            }

            ark.CommitChanges(true);
            if (op.ArkVersion < 3)
            {
                Console.WriteLine($"Wrote ark to \"{hdrPath}\"");
            }
            else
            {
                Console.WriteLine($"Wrote hdr to \"{hdrPath}\"");
            }

            if (usingCache)
            {
                CacheHelper.SaveCache();
            }

            using var header = new FileStream(hdrPath, FileMode.Open, FileAccess.ReadWrite);
            header.Seek(0, SeekOrigin.Begin);
            using var writer = new AwesomeWriter(header);
            writer.Write(0x6f303f55);
        }

        protected virtual string GuessPlatform(string arkPath)
        {
            // TODO: Get platform as arg?
            var platformRegex = new Regex("(?i)_([a-z0-9]+)([.]hdr)$");
            var match = platformRegex.Match(arkPath);

            if (!match.Success)
            {
                return null;
            }

            return match
                .Groups[1]
                .Value
                .ToLower();
        }

        protected virtual string CreateTemporaryDirectory()
        {
            // Create directory in temp path
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }

            return tempDir;
        }

        protected virtual void DeleteDirectory(string dirPath)
        {
            // Clean up files
            if (Directory.Exists(dirPath))
            {
                Directory.Delete(dirPath, true);
            }
        }
    }
}
