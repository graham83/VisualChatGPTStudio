﻿using JeffPires.VisualChatGPTStudio.Options;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JeffPires.VisualChatGPTStudio.Utils
{
    /// <summary>
    /// Static class containing methods for interacting with the ChatGPT API.
    /// </summary>
    static class ChatGPT
    {
        private static OpenAIAPI api;
        private static ChatMessageCache chatmessageCache = new ChatMessageCache();

        /// <summary>
        /// Requests a completion from the OpenAI API using the given options.
        /// </summary>
        /// <param name="options">The options to use for the request.</param>
        /// <param name="request">The request to send to the API.</param>
        /// <param name="resultHandler">The action to take when the result is received.</param>
        /// <returns>A task representing the completion request.</returns>
        public static async Task RequestAsync(OptionPageGridGeneral options, string request, Action<int, CompletionResult> resultHandler)
        {
            CreateApiHandler(options.ApiKey);

            Model model = Model.DavinciText;

            switch (options.Model)
            {
                case ModelLanguageEnum.TextCurie001:
                    model = Model.CurieText;
                    break;
                case ModelLanguageEnum.TextBabbage001:
                    model = Model.BabbageText;
                    break;
                case ModelLanguageEnum.TextAda001:
                    model = Model.AdaText;
                    break;
                case ModelLanguageEnum.CodeDavinci:
                    model = Model.DavinciCode;
                    break;
                case ModelLanguageEnum.CodeCushman:
                    model = Model.CushmanCode;
                    break;
            }

            CompletionRequest completionRequest = new(request,
                                                       model,
                                                       options.MaxTokens,
                                                       options.Temperature,
                                                       presencePenalty: options.PresencePenalty,
                                                       frequencyPenalty: options.FrequencyPenalty,
                                                       top_p: options.TopP,
                                                       stopSequences: GetStopSequenceArray(options.StopSequences));

            await api.Completions.StreamCompletionAsync(completionRequest, resultHandler);
        }

        /// <summary>
        /// Requests a chat request from the OpenAI API using the given options.
        /// </summary>
        /// <param name="options">The options to use for the request.</param>
        /// <param name="request">The derived request command to send to the API.</param>
        /// <param name="resultHandler">The action to take when the result is received.</param>
        /// <param name="contextText">Initial context code provided from the editor.</param>
        /// <param name="instructionText">Additional instructions added to the conversation from code comments.</param>
        /// <returns>A task representing the chat request.</returns>
        public static async Task ChatRequestAsync(OptionPageGridGeneral options, string request, Action<ChatResult> resultHandler,
            string contextText, string instructionText)
        {
            CreateApiHandler(options.ApiKey);

            Model model = Model.ChatGPTTurbo;

            if (!chatmessageCache.GetMessages().Any())
            {
                if (!string.IsNullOrEmpty(options.TurboChatBehavior))
                {
                    // Current OpenAI documentation states that system message does carry much weight in the conversation and
                    // instead to add it as a normal user message: https://platform.openai.com/docs/guides/chat/instructing-chat-models
                    chatmessageCache.AppendUserMessage(options.TurboChatBehavior);
                }
                if (!string.IsNullOrEmpty(contextText))
                {
                    chatmessageCache.AppendUserMessage($"{options.TurboChatContext}```{contextText}```\n");
                }
            }
            if (!string.IsNullOrEmpty(instructionText))
            {
                chatmessageCache.AppendUserMessage(instructionText);
            }

            chatmessageCache.AppendUserMessage(request);

            var chatRequest = new ChatRequest()
            {
                Model = model,
                Messages = chatmessageCache.GetMessages(),
                MaxTokens = options.MaxTokens,
                Temperature = options.Temperature,
                PresencePenalty = options.PresencePenalty,
                FrequencyPenalty = options.FrequencyPenalty,
                TopP = options.TopP,
                MultipleStopSequences = GetStopSequenceArray(options.StopSequences)
            };

            await api.Chat.StreamChatAsync(chatRequest, resultHandler);

        }

        /// <summary>
        /// Creates a new conversation and appends a system message with the specified TurboChatBehavior.
        /// </summary>
        /// <param name="options">The options to use for the conversation.</param>
        /// <returns>The newly created conversation.</returns>
        public static Conversation CreateConversation(OptionPageGridGeneral options)
        {
            CreateApiHandler(options.ApiKey);

            Conversation chat = api.Chat.CreateConversation();

            chat.AppendSystemMessage(options.TurboChatBehavior);

            return chat;
        }

        /// <summary>
        /// Reset the current conversation.
        /// </summary>
        public static void ResetConversation()
        {
            chatmessageCache.ClearMessages();
        }

        /// <summary>
        /// Creates an API handler with the given API key.
        /// </summary>
        /// <param name="apiKey">The API key to use.</param>
        private static void CreateApiHandler(string apiKey)
        {
            if (api == null)
            {
                api = new(apiKey);
            }
            else if (api.Auth.ApiKey != apiKey)
            {
                api.Auth.ApiKey = apiKey;
            }
        }

        /// <summary>
        /// Splits a string into an array of strings based on a comma delimiter.
        /// </summary>
        /// <param name="option">The string to be split.</param>
        /// <returns>An array of strings.</returns>
        private static string[] GetStopSequenceArray(string option)
        {
            string[] stopSequenceArray = option.Split(',');
            return stopSequenceArray;
        }
    }
}
