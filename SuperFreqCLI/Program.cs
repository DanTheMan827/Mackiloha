﻿using System;
using System.IO;
using CommandLine;
using Mackiloha.App;
using Mackiloha.App.Extensions;
using SuperFreqCLI.Options;

namespace SuperFreqCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Dir2MiloOptions, Milo2DirOptions>(args)
                .WithParsed<Dir2MiloOptions>(Dir2MiloOptions.Parse)
                .WithParsed<Milo2DirOptions>(Milo2DirOptions.Parse)
                .WithNotParsed(errors => { });
        }
    }
}
