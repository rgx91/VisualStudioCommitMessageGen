using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;
using Microsoft.VisualStudio.ProjectSystem.Query;

namespace vs22gptcommitviaapi
{
     /// <summary>
    /// Command handler for generating a commit message from the current Git changes.
    /// </summary>
    [VisualStudioContribution]
    internal class GenerateCommitCommand : Command
    {
        private readonly TraceSource _logger;
        private static readonly string OpenAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateCommitCommand"/> class.
        /// </summary>
        /// <param name="traceSource">A trace source for logging.</param>
        public GenerateCommitCommand(TraceSource traceSource)
        {
            _logger = Requires.NotNull(traceSource, nameof(traceSource));
        }

        /// <inheritdoc/>
        public override CommandConfiguration CommandConfiguration => new("Generate Commit Message")
        {
            Icon = new(ImageMoniker.KnownValues.Extension, IconSettings.IconAndText),
            Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu]
        };

        /// <inheritdoc/>
        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        {
            var solutionPath = await GetSolutionDirectoryAsync(cancellationToken);
            if (string.IsNullOrEmpty(solutionPath))
            {
                await Extensibility.Shell().ShowPromptAsync("Unable to locate the solution directory.", PromptOptions.OK, cancellationToken);
                return;
            }

            var changes = await GetGitChanges(solutionPath);
            if (string.IsNullOrEmpty(changes))
            {
                await Extensibility.Shell().ShowPromptAsync("No changes found in Git.", PromptOptions.OK, cancellationToken);
                return;
            }

            if (changes.Contains("warning: Not a git repository."))
            {
                await Extensibility.Shell().ShowPromptAsync("Not a git repository.", PromptOptions.OK, cancellationToken);
                return;
            }

            try
            {
                var commitMessage = await GenerateCommitMessageAsync(changes, cancellationToken);
                if (string.IsNullOrEmpty(commitMessage))
                {
                    await Extensibility.Shell().ShowPromptAsync("No commit message created.", PromptOptions.OK, cancellationToken);
                }

                RunOnSTAThread(() =>
                {
                    Clipboard.Clear();
                    Clipboard.SetText(commitMessage);
                });

                await Extensibility.Shell().ShowPromptAsync("Commit message has been copied to the clipboard.", PromptOptions.OK, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.TraceEvent(TraceEventType.Error, 0, $"Error generating commit message: {ex}");
                await Extensibility.Shell().ShowPromptAsync($"Error: {ex.Message}", PromptOptions.OK, cancellationToken);
            }
        }

        /// <summary>
        /// Retrieves the directory of the currently open solution.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The path to the solution directory, or an empty string if not found.</returns>
        private async Task<string> GetSolutionDirectoryAsync(CancellationToken cancellationToken)
        {
            var workspace = Extensibility.Workspaces();
            IQueryResults<ISolutionSnapshot> solutions = await workspace.QuerySolutionAsync(
                solution => solution.With(s => new { s.Path, s.Guid, s.ActiveConfiguration, s.ActivePlatform }),
                cancellationToken);

            var solutionPath = solutions.FirstOrDefault()?.Path;
            if (solutionPath != null)
            {
                return Path.GetDirectoryName(solutionPath) ?? string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// Retrieves the current Git changes by running a 'git diff' command.
        /// </summary>
        /// <param name="workingDirectory">The working directory of the repository.</param>
        /// <returns>A string containing the Git diff output, or an error message.</returns>
        private static async Task<string> GetGitChanges(string workingDirectory)
        {
            if (string.IsNullOrEmpty(workingDirectory))
            {
                return string.Empty;
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "diff",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            using var process = new Process
            {
                StartInfo = processStartInfo
            };

            process.Start();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            var results = await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync();

            var output = results[0];
            var error = results[1];

            return string.IsNullOrEmpty(error) ? output : $"Error running git diff: {error}";
        }

        /// <summary>
        /// Calls the OpenAI API to generate a commit message from the given code changes.
        /// </summary>
        /// <param name="changes">The Git changes as a string.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated commit message.</returns>
        private async Task<string> GenerateCommitMessageAsync(string changes, CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", OpenAiApiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a commit message generator. Provide only the detailed commit message, do not add things like this to the message: ```" },
                        new { role = "user", content = $"Here are the changes:\n\n{changes}" }
                    }
                }),
                Encoding.UTF8,
                "application/json");

            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(responseBody);

            return document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }

        /// <summary>
        /// Executes an action on an STA thread, required for clipboard operations.
        /// </summary>
        /// <param name="action">The action to run.</param>
        private void RunOnSTAThread(Action action)
        {
            var thread = new Thread(() =>
            {
                var dispatcher = Dispatcher.CurrentDispatcher;
                action();
                dispatcher.InvokeShutdown();
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
    }
}
