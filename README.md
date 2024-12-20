# GPT Commit Generator

This extension integrates with Visual Studio to generate meaningful Git commit messages using the OpenAI API. It retrieves your current Git changes, sends them to the OpenAI model, and copies the suggested commit message to your clipboard.

## Features
- **Automatically detect the current Git repository changes.**
- **Generate a commit message using OpenAI.**
- **Display and copy the generated commit message to your clipboard for immediate use.**
- **Minimal setup required—just ensure you have a valid OPENAI_API_KEY environment variable.**

## Requirements
- **Visual Studio 2022 or later.**
- **A valid OPENAI_API_KEY set as an environment variable on your machine.** The extension uses this key to authenticate with the OpenAI API.

## Setup and Usage

1. **Clone this repository** and open the solution in Visual Studio.
2. **Set OPENAI_API_KEY** as an environment variable. On Windows, you can set it using:

   ```powershell
   $env:OPENAI_API_KEY="sk-..."
   ```
3. **Build and run the extension** in the Visual Studio Experimental Instance.
4. **Open a solution** with a Git repository.
5. **Make some changes** in your repository.
6. **Generate a commit message** by navigating to:

   **Extensions > GPT Commit Generator > Generate Commit Message**
7. **View and copy the generated commit message**, which will also be automatically copied to your clipboard.
8. **Paste** the commit message into your Git commit command line or the Visual Studio Git Changes panel.

## Customization
- **Change the model**: The extension currently targets the `gpt-4o-mini` model. You can modify the model name in the `GenerateCommitMessageAsync` method.
- **Modify the prompt**: You can alter the prompt or system role instructions as needed to better suit your commit message preferences.

## Troubleshooting

### "Not a git repository" message
- Verify that your solution folder is under Git source control.

### No commit message generated
- Confirm that your **OPENAI_API_KEY** is valid.
- Ensure that you have pending Git changes to generate a meaningful message.

### Clipboard access issues
- Verify that the **STA thread dispatch logic** is functioning correctly to avoid clipboard access issues.
