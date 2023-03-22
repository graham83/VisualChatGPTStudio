﻿using Community.VisualStudio.Toolkit;
using EnvDTE;
using JeffPires.VisualChatGPTStudio.Options;
using JeffPires.VisualChatGPTStudio.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using OpenAI_API.Chat;
using OpenAI_API.Completions;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Constants = JeffPires.VisualChatGPTStudio.Utils.Constants;
using Span = Microsoft.VisualStudio.Text.Span;
using System.Text.RegularExpressions;



namespace JeffPires.VisualChatGPTStudio.Commands
{
    /// <summary>
    /// Base abstract class for commands
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <seealso cref="BaseCommand&lt;&gt;" />
    internal abstract class BaseChatGPTCommand<TCommand> : BaseCommand<TCommand> where TCommand : class, new()
    {
        protected DocumentView docView;
        private string selectedText;
        private int position;
        private int positionStart;
        private int positionEnd;
        private int lineLength;
        private bool firstInteration;
        private bool responseStarted;
        private ChatResponseState prevState = ChatResponseState.FirstResponse;
        private ChatGPTChunkProcessor chunkProcessor = new ChatGPTChunkProcessor();

        /// <summary>
        /// Gets the OptionsGeneral property of the VisuallChatGPTStudioPackage.
        /// </summary>
        protected OptionPageGridGeneral OptionsGeneral
        {
            get
            {
                return ((VisuallChatGPTStudioPackage)this.Package).OptionsGeneral;
            }
        }

        /// <summary>
        /// Gets the OptionsCommands property of the VisuallChatGPTStudioPackage.
        /// </summary>
        protected OptionPageGridCommands OptionsCommands
        {
            get
            {
                return ((VisuallChatGPTStudioPackage)this.Package).OptionsCommands;
            }
        }

        /// <summary>
        /// Gets the type of command.
        /// </summary>
        /// <param name="selectedText">The selected text.</param>
        /// <returns>The type of command.</returns>
        protected abstract CommandType GetCommandType(string selectedText);

        /// <summary>
        /// Gets the command for the given selected text.
        /// </summary>
        /// <param name="selectedText">The selected text.</param>
        /// <returns>The command.</returns>
        protected abstract string GetCommand(string selectedText);

        /// <summary>
        /// Executes asynchronously when the command is invoked and <see cref="M:Community.VisualStudio.Toolkit.BaseCommand.Execute(System.Object,System.EventArgs)" /> isn't overridden.
        /// </summary>
        /// <param name="e"></param>
        /// <remarks>
        /// Use this method instead of <see cref="M:Community.VisualStudio.Toolkit.BaseCommand.Execute(System.Object,System.EventArgs)" /> if you're invoking any async tasks by using async/await patterns.
        /// </remarks>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(OptionsGeneral.ApiKey))
                {
                    await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, Constants.MESSAGE_SET_API_KEY, buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);

                    Package.ShowOptionPage(typeof(OptionPageGridGeneral));

                    return;
                }

                var type = GetCommandType(selectedText);

                if (type == CommandType.Reset)
                {
                    ChatGPT.ResetConversation();
                    return;
                }

                firstInteration = true;
                responseStarted = false;
                lineLength = 0;

                await Package.JoinableTaskFactory.SwitchToMainThreadAsync();

                docView = await VS.Documents.GetActiveDocumentViewAsync();

                if (docView?.TextView == null) return;

                position = docView.TextView.Caret.Position.BufferPosition.Position;
                positionStart = docView.TextView.Selection.Start.Position.Position;
                positionEnd = docView.TextView.Selection.End.Position.Position;

                selectedText = docView.TextView.Selection.StreamSelectionSpan.GetText();

                var instructionText = TextFormat.ExtractFirstComment(selectedText);

                IVsTextManager textManager = (IVsTextManager)ServiceProvider.GlobalProvider.GetService(typeof(SVsTextManager));
                IVsTextView textView;
                textManager.GetActiveView(1, null, out textView);

                IWpfTextView wpfTextView = GetWpfTextView(textView);
                string contextText = wpfTextView.TextSnapshot.GetText();
                

                if (string.IsNullOrWhiteSpace(selectedText))
                {
                    await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, "Please select the code.", buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);

                    return;
                }

                if (CheckIfSelectedTwoOrMoreMethods(selectedText))
                {
                    await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, "Please select one method at a time.", buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);

                    return;
                }

                await RequestAsync(selectedText.Replace(instructionText, string.Empty), contextText, instructionText ?? string.Empty);
            }
            catch (Exception ex)
            {
                await VS.StatusBar.ShowProgressAsync(ex.Message, 2, 2);

                await VS.MessageBox.ShowAsync(Constants.EXTENSION_NAME, ex.Message, Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_WARNING, Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);
            }
        }

        private IWpfTextView GetWpfTextView(IVsTextView textView)
        {
            IWpfTextViewHost textViewHost;
            IVsUserData userData = textView as IVsUserData;
            if (userData == null)
            {
                throw new InvalidOperationException("Unable to get IVsUserData from text view.");
            }

            object holder;
            Guid guid = Microsoft.VisualStudio.Editor.DefGuidList.guidIWpfTextViewHost;
            userData.GetData(ref guid, out holder);
            textViewHost = (IWpfTextViewHost)holder;

            return textViewHost.TextView;
        }

        /// <summary>
        /// Requests the specified selected text from ChatGPT
        /// </summary>
        /// <param name="selectedText">The selected text.</param>
        private async Task RequestAsync(string selectedText, string contextText, string instructionText)
        {
            string command = GetCommand(selectedText);
          
            chunkProcessor.Reset();

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                await TerminalWindowCommand.Instance.RequestToWindowAsync(command, contextText, instructionText);

                return;
            }

            await VS.StatusBar.ShowProgressAsync(Constants.MESSAGE_WAITING_CHATGPT, 1, 2);

            if (OptionsGeneral.Model != ModelLanguageEnum.ChatGpt)
            {
                await ChatGPT.CompletionRequestAsync(OptionsGeneral, command, ResultHandler);
            }
            else
            {
                await ChatGPT.ChatRequestAsync(OptionsGeneral, command, ChatResultHandler, OptionsCommands.ChatSystemMessage, contextText, instructionText);
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                //Some documents does not has format
                (await VS.GetServiceAsync<DTE, DTE>()).ExecuteCommand("Edit.FormatDocument", string.Empty);
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Results handler.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="result">The result.</param>
        private void ResultHandler(int index, CompletionResult result)
        {
            const int LINE_LIMIT = 160;

            try
            {
                if (firstInteration)
                {
                    _ = VS.StatusBar.ShowProgressAsync(Constants.MESSAGE_RECEIVING_CHATGPT, 2, 2);

                    CommandType commandType = GetCommandType(selectedText);

                    if (commandType == CommandType.Replace)
                    {
                        position = positionStart;

                        //Erase current code
                        _ = docView.TextBuffer?.Replace(new Span(position, docView.TextView.Selection.StreamSelectionSpan.GetText().Length), String.Empty);
                    }
                    else if (commandType == CommandType.InsertBefore)
                    {
                        position = positionStart;

                        InsertANewLine(false);
                    }
                    else
                    {
                        position = positionEnd;

                        InsertANewLine(true);
                    }

                    if (typeof(TCommand) == typeof(Explain) || typeof(TCommand) == typeof(FindBugs))
                    {
                        docView.TextBuffer?.Insert(position, "//");
                        position += 2;
                    }

                    firstInteration = false;
                }

                string resultText = result.ToString();

                if (!responseStarted && (resultText.Equals("\n") || resultText.Equals("\r") || resultText.Equals(Environment.NewLine)))
                {
                    //Do nothing when API send only break lines on response begin
                    return;
                }

                responseStarted = true;

                if (typeof(TCommand) == typeof(AddSummary) && (resultText.Contains("{") || resultText.Contains("}")))
                {
                    return;
                }

                docView.TextBuffer?.Insert(position, resultText);

                position += resultText.Length;

                lineLength += resultText.Length;

                if (lineLength > LINE_LIMIT && (typeof(TCommand) == typeof(Explain) || typeof(TCommand) == typeof(FindBugs)))
                {
                    MoveToNextLineAndAddCommentPrefix();
                }
            }
            catch (Exception)
            {

            }
        }


        /// <summary>
        /// Chat results handler.
        /// </summary>
        /// <param name="result">The result.</param>
        private void ChatResultHandler(ChatResult result)
        {
            const int LINE_LIMIT = 160;

            if (chunkProcessor.CurrentState == ChatResponseState.FirstResponse)
            {
                _ = VS.StatusBar.ShowProgressAsync("Receiving chatGPT response", 2, 2);

                PreFormatExistingCode();

                chunkProcessor.CurrentState = ChatResponseState.Summary;
            }

            var chunk = result.Choices.First().Delta?.Content;
    
            if (result.Choices.First().FinishReason != null)
            {
                var remarks = chunkProcessor.RemarksSection;
                docView.TextBuffer?.Insert(position, remarks);
                position += remarks.Length;
            }

            if (chunk == null)
            {
                return;
            }
                   
            var state = chunkProcessor.ProcessChunk(chunk);

            if (state != prevState )
            {
                if (state == ChatResponseState.Code)
                {
                    docView.TextBuffer?.Insert(position, chunkProcessor.SummarySection);
                    position += chunkProcessor.SummarySection.Length;
                }

                if (state == ChatResponseState.Remarks)
                {
                    var code = chunkProcessor.CodeSection;

                    docView.TextBuffer?.Insert(position, code);

                    position += code.Length;
                }
                prevState = state;
            }


        }

        private void PreFormatExistingCode()
        {
            CommandType commandType = GetCommandType(selectedText);

            if (commandType == CommandType.Erase)
            {
                position = positionStart;

                //Erase current code
                _ = docView.TextBuffer?.Replace(new Span(position, docView.TextView.Selection.StreamSelectionSpan.GetText().Length), String.Empty);
            }
            else if (commandType == CommandType.InsertBefore)
            {
                position = positionStart;

                InsertANewLine(false);
            }
            else
            {
                position = positionEnd;

                InsertANewLine(true);
            }
        }
   
        private async Task CoolDeleteEffectAsync(ITextBuffer textBuffer, int position, string text)
        {
            var words = text.Split(' ');

            for (int i = words.Length - 1; i >= 0; i--)
            {
                string word = words[i];
                int wordLength = word.Length;

                // Delete the word
                textBuffer.Replace(new Span(position, wordLength), String.Empty);

                // Update the position for the next word
                position -= (wordLength + 1); // Add 1 for the space between words

                // Introduce a delay between deleting words
                await Task.Delay(100); // You can adjust this delay as needed for the desired effect
            }
        }

        /// <summary>
        /// Inserts a new line into the document and optionally moves the position to the start of the next line.
        /// </summary>
        /// <param name="moveToNextLine">Indicates whether the position should be moved to the start of the next line.</param> 
        private void InsertANewLine(bool moveToNextLine)
        {
            ITextSnapshot textSnapshot = docView.TextBuffer?.Insert(position, Environment.NewLine);

            // Get the next line
            ITextSnapshotLine nextLine = textSnapshot.GetLineFromLineNumber(textSnapshot.GetLineNumberFromPosition(position) + 1);

            if (moveToNextLine)
            {
                // Get the position of the first character on the next line
                position = nextLine.Start.Position;
            }
        }

        /// <summary>
        /// Inserts a new line and adds a comment prefix to the next line.
        /// </summary>
        private void MoveToNextLineAndAddCommentPrefix()
        {
            lineLength = 0;

            InsertANewLine(true);

            docView.TextBuffer?.Insert(position, "//");
            position += 2;
        }


        /// <summary>
        /// Check If Selected Two Or More Methods
        /// </summary>
        /// <param name="text">The text to check</param>
        /// <returns>True if has Selected Two Or More Methods</returns>
        private bool CheckIfSelectedTwoOrMoreMethods(string text)
        {
            string[] words = text.Split(' ');

            return words.Count(w => w == "public" || w == "private" || w == "protected") >= 2;
        }
    }

    /// <summary>
    /// Enum to represent the different types of commands that can be used. 
    /// </summary>
    enum CommandType
    {
        Replace,
        InsertBefore,
        InsertAfter,
        Reset
    }
}
