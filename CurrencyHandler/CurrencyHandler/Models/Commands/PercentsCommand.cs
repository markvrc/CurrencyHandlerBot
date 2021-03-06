﻿using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CurrencyHandler.Models.Commands.Abstractions;
using CurrencyHandler.Models.Database.Repositories;
using CurrencyHandler.Models.InlineKeyboardHandlers.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace CurrencyHandler.Models.Commands
{
    public class PercentsCommand : Command
    {
        public PercentsCommand(IKeyboards keyboards, ICurrenciesRepository repo) : base(keyboards, repo)
        {
        }

        public override string Name => "Percents";

        /// <summary>
        /// Handles percents settings change for the chat 
        /// </summary>
        /// <param name="message">the message a user sent</param>
        /// <param name="client">Bot instance, needed to answer on the message</param>
        /// <param name="repo">Repository for the whole db, allows this command handler to save/read data</param>
        /// <returns>Task to be awaited</returns>
        public override async Task Execute(Message message)
        {
            const string numbers = "0123456789";

            var messageText = message.Text;

            // get substring containing decimal value
            var firstNumIndex = messageText.IndexOfAny(numbers.ToCharArray());
            var lastNumIndex = messageText.LastIndexOfAny(numbers.ToCharArray());
            var length = lastNumIndex - firstNumIndex + 1;
            var substring = messageText
                .Substring(firstNumIndex, length)
                .Replace(',', '.'); // can process both , and . in the number;

            // convert it to decimal and if unsuccessful, return
            if (!decimal.TryParse(substring, out var number))
            {
                var errorMsg = $"could not match {messageText} as percents command. " +
                    $"The number the program tried to parse was: {substring}";

                throw new InvalidOperationException(errorMsg);
            }

            // else, do the processing
            var messageId = message.MessageId;
            var chatId = message.Chat.Id;
            var text = "Successfuly set your new percent settings!";

            await Repo.SetPercentsAsync(number, chatId);

            await Client.SendTextMessageAsync(chatId, text, replyToMessageId: messageId);
        }
    }
}
