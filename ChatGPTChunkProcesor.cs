using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;

namespace JeffPires.VisualChatGPTStudio
{
    public enum State { 
        FirstResponse, 
        Summary, 
        Code, 
        Remarks 
    };

    internal class ChatGPTChunkProcessor
    {
        private StringBuilder formattedResponse = new StringBuilder();
        private StringBuilder currentChunk = new StringBuilder();

        private State currentState = State.FirstResponse;

        internal State CurrentState { get => currentState; set => currentState = value; }

        internal void ProcessChunk(string chunk)
        {
            currentChunk.Append(chunk);
            string text = currentChunk.ToString();
            string[] tokens = text.Split(new[] { "```" }, StringSplitOptions.None);
           
            for (int i = 0; i < tokens.Length; i++)
            {
                switch (CurrentState)
                {
                    case State.Summary:
                        formattedResponse.Clear();
                        if (i + 1 < tokens.Length && tokens[i + 1] == "") // Found <code> tag
                        {
                            formattedResponse.Append("/// <summary>\n");
                            formattedResponse.Append(WrapText(tokens[i], "/// "));
                            formattedResponse.Append("/// </summary>\n");
                            CurrentState = State.Code;
                            i++; // Skip the empty token after <code>
                        }
                        else
                        {
                            formattedResponse.Append(WrapText(tokens[i], "/// "));
                        }
                        break;

                    case State.Code:
                        formattedResponse.Append(tokens[i]);
                        if (i + 1 < tokens.Length && tokens[i + 1] == "") // Found </code> tag
                        {
                            CurrentState = State.Remarks;
                            i++; // Skip the empty token after </code>
                        }
                        break;

                    case State.Remarks:
                        if (i == tokens.Length - 1) // Last token in the current chunk
                        {
                            currentChunk.Clear();
                            currentChunk.Append(tokens[i]);
                        }
                        else
                        {
                            formattedResponse.Append("/// <remarks>\n");
                            formattedResponse.Append(WrapText(tokens[i], "/// "));
                            formattedResponse.Append("/// </remarks>\n");
                            CurrentState = State.Summary;
                        }
                        break;
                }
            }
        }

        public string GetFormattedResponse()
        {
            if (CurrentState == State.Remarks)
            {
                formattedResponse.Append("/// <remarks>\n");
                formattedResponse.Append(WrapText(currentChunk.ToString(), "/// "));
                formattedResponse.Append("/// </remarks>\n");
            }

            return formattedResponse.ToString();
        }

        private string WrapText(string text, string prefix)
        {
            StringBuilder wrappedText = new StringBuilder();
            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (string line in lines)
            {
                wrappedText.Append(prefix);
                wrappedText.Append(line);
                wrappedText.Append('\n');
            }

            return wrappedText.ToString();
        }
    }
}
