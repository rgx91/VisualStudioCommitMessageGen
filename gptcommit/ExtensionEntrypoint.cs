// File 1: ExtensionEntrypoint.cs

using Microsoft.VisualStudio.Extensibility;

namespace vs22gptcommitviaapi
{
    /// <summary>
    /// Extension entry point for the Visual Studio extensibility extension.
    /// </summary>
    [VisualStudioContribution]
    internal class ExtensionEntrypoint : Extension
    {
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
}