﻿using System;
using System.IO;
using System.Linq;
using CommandLine;
using Mackiloha.App;
using Mackiloha.App.Extensions;
using Mackiloha.IO;

namespace SuperFreqCLI.Options
{
    [Verb("dir2milo", HelpText = "Creates milo archive from input directory")]
    internal class Dir2MiloOptions
    {
        [Value(0, Required = true, MetaName = "dirPath", HelpText = "Path to input directory")]
        public string InputPath { get; set; }

        [Value(1, Required = true, MetaName = "miloPath", HelpText = "Path to output milo archive")]
        public string OutputPath { get; set; }

        public static void Parse(Dir2MiloOptions op)
        {
            var appState = new AppState(Path.GetDirectoryName(op.InputPath));
            var info = new SystemInfo()
            {
                Version = 24,
                Platform = Platform.PS2,
                BigEndian = false
            };

            appState.UpdateSystemInfo(info);
            appState.BuildMiloArchive(op.InputPath, op.OutputPath);
        }
    }
}
