// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Reflection;
using Docfx.Dotnet;
using Docfx.Pdf;
using Spectre.Console.Cli;

namespace Docfx;

class DefaultCommand : Command<DefaultCommand.Options>
{
    [Description("Runs metadata, build and pdf commands")]
    internal class Options : BuildCommandOptions
    {
        [Description("Prints version information")]
        [CommandOption("-v|--version")]
        public bool Version { get; set; }
    }

    public override int Execute(CommandContext context, Options options)
    {
        if (options.Version)
        {
            Console.WriteLine(typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
            return 0;
        }

        return CommandHelper.Run(options, () =>
        {
            var (config, configDirectory) = Docset.GetConfig(options.ConfigFile);
            var outputFolder = options.OutputFolder;
            string serveDirectory = null;

            if (config.metadata is not null)
            {
                DotnetApiCatalog.Exec(config.metadata, new(), configDirectory).GetAwaiter().GetResult();
            }

            if (config.build is not null)
            {
                BuildCommand.MergeOptionsToConfig(options, config.build, configDirectory);
                serveDirectory = RunBuild.Exec(config.build, new(), configDirectory, outputFolder);

                PdfBuilder.CreatePdf(serveDirectory).GetAwaiter().GetResult();
            }

            if (config.pdf is not null)
            {
                BuildCommand.MergeOptionsToConfig(options, config.pdf, configDirectory);
                RunPdf.Exec(config.pdf, new(), configDirectory, outputFolder);
            }

            if (options.Serve && serveDirectory is not null)
            {
                RunServe.Exec(serveDirectory, options.Host, options.Port, options.OpenBrowser, options.OpenFile);
            }
        });
    }
}
