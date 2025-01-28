// File 1: ExtensionEntrypoint.cs

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using System.Diagnostics;

namespace vs22gptcommitviaapi
{
    /// <summary>
    /// Extension entry point for the Visual Studio extensibility extension.
    /// </summary>
    [VisualStudioContribution]
    internal class ExtensionEntrypoint : Extension
    {
        [VisualStudioContribution]
        public static MenuConfiguration MyParentMenu => new("Generate commit message")
        {
            Placements = new[] { CommandPlacement.KnownPlacements.ExtensionsMenu },
            Children = new[]
            {
                MenuChild.Command<NormalCommitCommand>(),
                MenuChild.Command<PirateCommitCommand>()
            }
        };


        /// <inheritdoc/>
        public override ExtensionConfiguration ExtensionConfiguration => new()
        {
            Metadata = new(
                id: "gptcommit2.ec36997a-c76e-4f2e-8dda-09b84422f128",
                version: ExtensionAssemblyVersion,
                publisherName: "Belicza Gábor",
                displayName: "GPT Commit Generator",
                description: "An extension to generate commit messages using OpenAI.")
        };
    }

    [VisualStudioContribution]
    internal class PirateCommitCommand : BaseCommitCommand
    {
        public PirateCommitCommand(TraceSource traceSource)
            : base(traceSource) { }

        protected override string SystemMessage =>
            "You're a pirate! Generate a commit message in pirate jargon. " +
            "Start with a brief summary, then a blank line, followed by bullet points. " +
            "Use terms like 'Arrr', 'matey', 'ahoy'. No markdown. Example:\n\n" +
            "Enhancin' performance loggin' n' updatin' configurations\n\n" +
            "Added a stopwatch t' measure performance...";

        public override CommandConfiguration CommandConfiguration => new("Pirate")
        {
            Icon = new(ImageMoniker.KnownValues.Extension, IconSettings.IconAndText)
        };
    }

    [VisualStudioContribution]
    internal class NormalCommitCommand : BaseCommitCommand
    {
        public NormalCommitCommand(TraceSource traceSource) 
            : base(traceSource) { }

        protected override string SystemMessage =>
            "Generate a commit message starting with a one-line summary, then a blank line, " +
            "followed by bullet points. Each bullet starts with a past tense verb (Added, Fixed, etc.). " +
            "No markdown. Example:\n\n" +
            "Enhance performance logging and update configurations\n\n" +
            "Added Stopwatch for performance measurement...";

        public override CommandConfiguration CommandConfiguration => new("Normal")
        {
            Icon = new(ImageMoniker.KnownValues.Extension, IconSettings.IconAndText)
        };
    }
}