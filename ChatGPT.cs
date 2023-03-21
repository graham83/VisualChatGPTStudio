using EnvDTE;
using JeffPires.VisualChatGPTStudio.Options;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.Shell;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JeffPires.VisualChatGPTStudio
{
    public enum RequestType
    {
        CompletionRequest,
        ChatRequest
    }

    /// <summary>
    /// Static class containing methods for interacting with the ChatGPT API.
    /// </summary>
    public static class ChatGPT
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
        public static async Task CompletionRequestAsync(OptionPageGridGeneral options, string request, Action<int, CompletionResult> resultHandler)
        {
            if (api == null)
            {
                api = new(options.ApiKey);
            }
            else if (api.Auth.ApiKey != options.ApiKey)
            {
                api.Auth.ApiKey = options.ApiKey;
            }

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

                CompletionRequest completionRequest = new CompletionRequest(request, model, options.MaxTokens, options.Temperature, presencePenalty: options.PresencePenalty,
                    frequencyPenalty: options.FrequencyPenalty, top_p: options.TopP, stopSequences: GetStopSequenceArray(options.StopSeqeuences));
                    
                await api.Completions.StreamCompletionAsync(completionRequest, resultHandler);
           
        }

        /// <summary>
        /// Requests a completion from the OpenAI API using the given options.
        /// </summary>
        /// <param name="options">The options to use for the request.</param>
        /// <param name="request">The request to send to the API.</param>
        /// <param name="resultHandler">The action to take when the result is received.</param>
        /// <returns>A task representing the completion request.</returns>
        public static async Task ChatRequestAsync(OptionPageGridGeneral options, string request, Action<ChatResult> resultHandler, OptionPageGridCommands commands)
        {
            if (api == null)
            {
                api = new(options.ApiKey);
            }
            else if (api.Auth.ApiKey != options.ApiKey)
            {
                api.Auth.ApiKey = options.ApiKey;
            }

            Model model = Model.ChatGPTTurbo;
           

            if (!chatmessageCache.GetMessages().Any())
            {
                chatmessageCache.AppendSystemMessage(commands.ChatSystemMessage);
                chatmessageCache.AppendUserMessage(commands.ChatSystemMessage);
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
                MultipleStopSequences = GetStopSequenceArray(options.StopSeqeuences)
            };

            await api.Chat.StreamChatAsync(chatRequest, resultHandler);
           
        }

        /// <summary>
        ///  Returns an string array of stop sequences
        /// </summary>
        private static string[] GetStopSequenceArray(string option)
        {
            string[] stopSequenceArray = option.Split(',');
            return stopSequenceArray;
        }

    }
}
