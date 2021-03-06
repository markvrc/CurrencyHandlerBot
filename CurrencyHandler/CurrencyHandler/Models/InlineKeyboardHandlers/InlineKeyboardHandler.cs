﻿using System;
using CurrencyHandler.Models.Database.Repositories;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using CurrencyHandler.Models.Extensions;
using CurrencyHandler.Models.InlineKeyboardHandlers.Abstractions;

namespace CurrencyHandler.Models.InlineKeyboardHandlers
{
    public abstract class InlineKeyboardHandler : IInlineKeyboardHandler
    {
        protected ICurrenciesRepository Repository { get; }

        protected ITelegramBotClient Bot { get; }

        protected InlineKeyboardHandler(ICurrenciesRepository repo)
        {
            Repository = repo;
            Bot = Models.Bot.GetClient();
        }

        // if ends with InlineKeyboardHandler, then default name is name with abstract class'es name trimmed
        public virtual string Name => GetType().Name.Substring(0,
            GetType().Name.LastIndexOf(nameof(InlineKeyboardHandler)));

        public bool Contains(string callBackData)
        {
            const int firstIndex = 0;
            return callBackData.IndexOf(Name, StringComparison.Ordinal) == firstIndex;
        }

        public abstract void SendKeyboard(Message message);

        public abstract void HandleCallBack(CallbackQuery callbackQuery);

        // ReSharper disable once InconsistentNaming (should be implemented as async)
        public abstract Task SendKeyboardAsync(Message message);

        // ReSharper disable once InconsistentNaming (should be implemented as async)
        public abstract Task HandleCallBackAsync(CallbackQuery callbackQuery);

        /// <summary>
        /// splits string[] into string[][], then converts string[][] to button[][],
        /// then creates inlineKeyboard out of those buttons.
        /// </summary>
        /// <param name="data">content for buttons</param>
        /// <returns></returns>
        protected InlineKeyboardMarkup StringArrayToKeyboard(string[] data)
        {
            var displayData = data.Split(5);

            var buttons = ToInlineKeyBoardButtons(displayData);

            return new InlineKeyboardMarkup(buttons);
        }

        /// <summary>
        /// converts string[][] to InlineKeyboardButton[][]
        /// <para>specifies CallBackData as the Name of the current keyboard + text of the button</para>
        /// <para>button's text is taken from the jagged array</para>
        /// </summary>
        /// <param name="arr"></param>
        /// <returns></returns>
        protected InlineKeyboardButton[][] ToInlineKeyBoardButtons(string[][] arr)
        {
            var result = new InlineKeyboardButton[arr.Length][];

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new InlineKeyboardButton[arr[i].Length];
            }

            for (var i = 0; i < arr.Length; i++)
            {
                for (var j = 0; j < arr[i].Length; j++)
                {
                    result[i][j] = new InlineKeyboardButton
                    {
                        CallbackData = Name + arr[i][j],
                        Text = arr[i][j]
                    };
                }
            }

            return result;
        }

        protected virtual string GetTextFromCallbackData(CallbackQuery data)
        {
            return data.Data.TrimStart(Name.ToCharArray());
        }

        public void Dispose()
        {
            Repository.Dispose();
        }
    }
}
