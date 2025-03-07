﻿using Community.VisualStudio.Toolkit;
using JeffPires.VisualChatGPTStudio.Commands;
using JeffPires.VisualChatGPTStudio.Utils;

namespace JeffPires.VisualChatGPTStudio
{
    [Command(PackageIds.AddSummary)]
    internal sealed class AddSummary : BaseChatGPTCommand<AddSummary>
    {
        protected override CommandType GetCommandType(string selectedText)
        {
            return CommandType.InsertBefore;
        }

        protected override string GetCommand(string selectedText)
        {
            return TextFormat.FormatCommandForSummary($"{OptionsCommands.AddSummary}\r\n\r\n{{0}}\r\n\r\n", selectedText);
        }
    }
}
