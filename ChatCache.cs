using OpenAI_API.Chat;
using System.Collections.Generic;

namespace JeffPires.VisualChatGPTStudio
{
    public class ChatMessageCache
    {
        private static List<ChatMessage> _chatMessages = new List<ChatMessage>();

        public void AddMessage(ChatMessage message)
        {
            _chatMessages.Add(message);
        }

        public void AppendSystemMessage(string message)
        {
            _chatMessages.Add(new ChatMessage(ChatMessageRole.System, message));
        }

        public void AppendUserMessage(string message)
        {
            _chatMessages.Add(new ChatMessage(ChatMessageRole.User, message));
        }

        public void AppendChatGptResponseMessage(string message)
        {
            _chatMessages.Add(new ChatMessage(ChatMessageRole.Assistant, message));
        }
        public IList<ChatMessage> GetMessages()
        {
            return _chatMessages;
        }

        public void ClearMessages()
        {
            _chatMessages.Clear();
        }
    }
}

