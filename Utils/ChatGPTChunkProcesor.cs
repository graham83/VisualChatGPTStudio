using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.AxHost;

namespace JeffPires.VisualChatGPTStudio
{
    public enum ChatResponseState { 
        FirstResponse, 
        Summary, 
        Code, 
        Remarks 
    };

    internal class ChatGPTChunkProcessor
    {
        private StringBuilder formattedResponse = new StringBuilder();
        private StringBuilder currentSectionText = new StringBuilder();

        private StringBuilder summarySection = new StringBuilder();
        private StringBuilder codeSection = new StringBuilder();
        private StringBuilder remarksSection = new StringBuilder();

        private ChatResponseState currentState = ChatResponseState.FirstResponse;

        internal ChatResponseState CurrentState { get => currentState; set => currentState = value; }
        public string SummarySection { get => summarySection.ToString();}
        public string CodeSection { get => codeSection.ToString(); }
        public string RemarksSection { get => GetFormattedRemarks(); }

        internal ChatResponseState ProcessChunk(string chunk)
        {

            currentSectionText.Append(chunk);
            string text = currentSectionText.ToString();
            int delimiterIndex = text.IndexOf("```");

            if (CurrentState == ChatResponseState.Summary)
            {
                
                if (delimiterIndex >= 0) // Found <code> tag
                {
                    var summartyText = text.Substring(0, delimiterIndex);
                    
                    var wrappedSummary = WrapText(summartyText, "/// ");
                    summarySection.Clear();
                    summarySection.Append("/// <summary>\n");
                    summarySection.Append(wrappedSummary);
                    summarySection.Append("/// </summary>\n");
                    currentSectionText.Clear();
                    CurrentState = ChatResponseState.Code;
                }
                else
                {
                    summarySection.Append(chunk);
                }
            }
            else if (CurrentState == ChatResponseState.Code)
            {
                if (delimiterIndex >= 0) // Found </code> tag
                {
                    var codeText = text.Substring(0, delimiterIndex);
                    codeSection.Clear();
                    codeSection.Append(codeText);
                    currentSectionText.Clear();
                    currentSectionText.Append(text.Substring(delimiterIndex + 3));
                    CurrentState = ChatResponseState.Remarks;
                }
                else
                {
                    codeSection.Append(chunk);
                }
            }
            else if (CurrentState == ChatResponseState.Remarks)
            {
                remarksSection.Append(chunk);   
            }

            return CurrentState;
        }

        public string GetFormattedResponse()
        {
            if (CurrentState == ChatResponseState.Summary)
            {
                var wrappedSummary = WrapText(summarySection.ToString(), "/// ");
                summarySection.Clear();
                summarySection.Append("/// <summary>\n");
                remarksSection.Append(wrappedSummary);
                remarksSection.Append("/// </summary>\n");
            }

            if (CurrentState == ChatResponseState.Remarks)
            {
                GetFormattedRemarks();
            }

            formattedResponse.Append(summarySection);
            formattedResponse.Append(codeSection);
            formattedResponse.Append(remarksSection);

            return formattedResponse.ToString();
        }

        public string GetFormattedRemarks()
        {
            var wrappedRemarks = WrapText(remarksSection.ToString(), "/// ");
            remarksSection.Clear();
            remarksSection.Append("/// <remarks>\n");
            remarksSection.Append(wrappedRemarks);
            remarksSection.Append("/// </remarks>\n");
            return remarksSection.ToString();
        }

        public void Reset()
        {
            formattedResponse.Clear();
            currentSectionText.Clear();
            summarySection.Clear();
            codeSection.Clear();
            remarksSection.Clear();
            CurrentState = ChatResponseState.FirstResponse;
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
