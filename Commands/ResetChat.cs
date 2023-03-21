using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using System;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.ResetChat)]
    internal sealed class ResetChat : BaseChatGPTCommand<ResetChat>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.Reset;
        }

        protected override string GetCommand(string selectedText)
        {
            return $"Lets start from scratch.";
        }
    }
}
