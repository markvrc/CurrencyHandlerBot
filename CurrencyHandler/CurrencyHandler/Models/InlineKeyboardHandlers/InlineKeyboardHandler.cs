﻿using System;
using CurrencyHandler.Models.Database.Repositories;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using CurrencyHandler.Models.Extensions;

namespace CurrencyHandler.Models.InlineKeyboardHandlers
{
    public abstract class InlineKeyboardHandler
    {
        public abstract string Name { get; }

        protected CurrenciesRepository Repository { get; }

        protected InlineKeyboardHandler(CurrenciesRepository repo)
        {
            Repository = repo;
        }

        public bool Contains(string callBackData)
        {
            const int firstIndex = 0;
            return callBackData.IndexOf(Name, StringComparison.Ordinal) == firstIndex; 
        }

        public abstract void SendKeyboard(Message message, TelegramBotClient client);

        public abstract void HandleCallBack(CallbackQuery callbackQuery);

        // ReSharper disable once InconsistentNaming (should be implemented as async)
        public abstract Task SendKeyboardAsync(Message message, TelegramBotClient client);

        // ReSharper disable once InconsistentNaming (should be implemented as async)
        public abstract Task HandleCallBackAsync(CallbackQuery callbackQuery);

        protected InlineKeyboardMarkup StringArrayToKeyboard(string[] data)
        {
            var displayData = data.Split(5);

            return new InlineKeyboardMarkup(ToInlineKeyBoardButtons(displayData));            
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

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new InlineKeyboardButton[arr[i].Length];
            }

            for (int i = 0; i < arr.Length; i++)
            {
                for (int j = 0; j < arr[i].Length; j++)
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

        protected string GetTextFromCallbackData(CallbackQuery data)
        {
            return data.Data.TrimStart(Name.ToCharArray());
        }
    }
}
