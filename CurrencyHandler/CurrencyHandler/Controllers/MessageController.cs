﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CurrencyHandler.Models;
using CurrencyHandler.Models.Database.Repositories;
using CurrencyHandler.Models.ExceptionsHandling;
using CurrencyHandler.Models.Extensions;
using CurrencyHandler.Models.InlineKeyboardHandlers;
using CurrencyHandler.Models.QueryHandling;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace CurrencyHandler.Controllers
{
    [Route("api/message/update")]
    public class MessageController : Controller
    {
        private static readonly UpdateType[] Types =
        {
            UpdateType.Message,
            UpdateType.InlineQuery,
            UpdateType.CallbackQuery
        };

        private readonly InlineQueryHandler _inlineQueryHandler;
        private readonly CurrenciesRepository _repo;

        public MessageController(CurrenciesRepository repo, CurrenciesEmojisRepository emojiRepo)
        {
            _inlineQueryHandler = new InlineQueryHandler(emojiRepo);

            _repo = repo;
        }

        [HttpGet]
        public string Get()
        {
            return "Hi there! :D";
        }

        [HttpPost]
        public async Task<OkResult> Post([FromBody]Update update)
        {
            if (update == null)
                return Ok();

            if (!update.Type.In(Types))
                return Ok();

            try
            {
                if (update.Type == UpdateType.CallbackQuery)
                {
                    foreach (var i in Keyboards.Get(_repo))
                    {
                        if (i.Contains(update.CallbackQuery.Data))
                        {
                            await i.HandleCallBackAsync(update.CallbackQuery);
                            return Ok();
                        }
                    }
                }

                if (update.Type == UpdateType.InlineQuery)
                {
                    await _inlineQueryHandler.HandleAsync(update.InlineQuery);
                    return Ok();
                }

                var commands = Bot.Commands;
                var botClient = await Bot.Get();
                var message = update.Message;

                if (message == null || String.IsNullOrEmpty(message.Text))
                {
                    return Ok();
                }

                foreach (var command in commands)
                {
                    if (command?.Contains(message.Text) ?? false)
                    {
                        await command.Execute(message, botClient, _repo);

                        break;
                    }
                }
            }
            catch (Exception e)
            {
                await ExceptionsHandling.HandleExceptionAsync(e, update, update.Type);
            }

            return Ok();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _repo.Dispose();
        }
    }
}