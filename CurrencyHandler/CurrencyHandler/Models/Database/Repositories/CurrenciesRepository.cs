﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CurrencyHandler.Models.Database.Contexts;
using CurrencyHandler.Models.Database.Models;
using CurrencyHandler.Models.Extensions;
using CurrencyHandler.Models.HelperClasses;
using Microsoft.EntityFrameworkCore;

namespace CurrencyHandler.Models.Database.Repositories
{
    public class CurrenciesRepository : ICurrenciesRepository
    {
        protected ChatSettingsContext Context { get; }

        protected ICurrenciesEmojisRepository CurrenciesEmojisRepository { get; }

        public CurrenciesRepository(ChatSettingsContext ctx, ICurrenciesEmojisRepository repo) // TODO: DI this
        {
            Context = ctx;
            CurrenciesEmojisRepository = repo;
        }

        protected ChatSettings AddChat(ChatSettings entity)
        {
            Context.ChatSettings.Add(entity);

            Context.SaveChanges();

            return entity;
        }

        protected ChatSettings InitChat(long chatId)
        {
            var entity = DefaultValues.DefaultEntity;
            entity.ChatId = chatId;

            Context.ChatSettings.Add(entity);

            Context.SaveChanges();

            return entity;
        }

        protected async Task<ChatSettings> AddChatAsync(ChatSettings entity)
        {
            await Context.ChatSettings.AddAsync(entity);

            await Context.SaveChangesAsync();

            return entity;
        }

        protected async Task<ChatSettings> InitChatAsync(long chatId)
        {
            var entity = DefaultValues.DefaultEntity;
            entity.ChatId = chatId;

            await Context.ChatSettings.AddAsync(entity);

            await Context.SaveChangesAsync();

            return entity;
        }

        public async Task RemoveDisplayEmojisAsync(string emoji, long chatID)
        {
            ChatSettings chat;
            string currency; // ChatSettings store only Currency, so we'll need to get the currency for given emoji

            chat = Context.ChatSettings.FirstOrDefault(t => t.ChatId == chatID);

            if (chat == null) // if null, then init chat and get currency from emoji in parallel
            {
                var chatTask = InitChatAsync(chatID);
                var currencyTask = Context.CurrencyEmojis.FirstAsync(i => i.Emoji == emoji);

                await Task.WhenAll(chatTask, currencyTask);

                chat = chatTask.Result;
                currency = currencyTask.Result.Currency;
            }
            else // if chat is already initialized, no parallel - only 1 task possible
            {
                currency = (await Context.CurrencyEmojis.FirstAsync(i => i.Emoji == emoji)).Currency;
            }

            var isRemoved = chat.DisplayCurrencies.Remove(currency);

            if (!isRemoved)
                throw new Exception(
                    $"Attempt to remove {currency} from displayCurrencies. " +
                    $"{currency} is already not in the list");

            Context.Entry(chat).State = EntityState.Modified; // ef doesn't see it itself that it's changed

            Context.SaveChanges();
        }

        public async Task AddDisplayEmojisAsync(string emoji, long chatID)
        {
            ChatSettings chat;
            string currency; // ChatSettings store only Currency, so we'll need to get the currency for given emoji

            chat = Context.ChatSettings.FirstOrDefault(t => t.ChatId == chatID);

            if (chat == null) // if null, then init chat and get currency from emoji in parallel
            {
                var chatTask = InitChatAsync(chatID);
                var currencyTask = Context.CurrencyEmojis.FirstAsync(i => i.Emoji == emoji);

                await Task.WhenAll(chatTask, currencyTask);

                chat = chatTask.Result;
                currency = currencyTask.Result.Currency;
            }
            else // if chat is already initialized, no parallel - only 1 task possible
            {
                currency = (await Context.CurrencyEmojis.FirstAsync(i => i.Emoji == emoji)).Currency;
            }

            chat.DisplayCurrencies.Add(currency);

            Context.Entry(chat).State = EntityState.Modified; // not having this line cost me 2 hours (ef didn't see it as modified)

            Context.SaveChanges();
        }

        public async Task<string[]> GetDisplayEmojisAsync(long chatId)
        {
            var chat = await Context.ChatSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cs => cs.ChatId == chatId)
                ?? await InitChatAsync(chatId);

            var displayCurrencies = chat.DisplayCurrencies;
            var currenciesEmojis = Context.CurrencyEmojis.AsNoTracking();

            return await currenciesEmojis.Where(i => i.Currency.In(displayCurrencies)).Select(ce => ce.Emoji).ToArrayAsync();
        }

        public string GetCurrency(long chatId)
        {
            var chat = Context.ChatSettings.AsNoTracking().FirstOrDefault(t => t.ChatId == chatId);

            return chat == null ? InitChat(chatId).ValueCurrency : chat.ValueCurrency;
        }

        public decimal GetPercents(long chatId)
        {
            var chat = Context.ChatSettings
                .AsNoTracking()
                .FirstOrDefault(t => t.ChatId == chatId);

            return chat?.Percents ?? InitChat(chatId).Percents;
        }

        public void SetCurrency(string value, long chatId)
        {
            var chat = Context.ChatSettings.FirstOrDefault(t => t.ChatId == chatId);

            if (chat == null)
            {
                chat = DefaultValues.DefaultEntity;
                chat.ChatId = chatId;
                chat.ValueCurrency = value;

                AddChat(chat);
            }
            else
            {
                chat.ValueCurrency = value;

                Context.SaveChanges();
            }
        }

        public void SetPercents(decimal value, long chatId)
        {
            var chat = Context.ChatSettings.FirstOrDefault(t => t.ChatId == chatId);

            if (chat == null)
            {
                chat = DefaultValues.DefaultEntity;
                chat.ChatId = chatId;
                chat.Percents = value;

                AddChat(chat);
            }
            else
            {
                chat.Percents = value;

                Context.SaveChanges();
            }
        }

        public async Task SetCurrencyAsync(string value, long chatId)
        {
            var chat = await Context.ChatSettings.FirstOrDefaultAsync(t => t.ChatId == chatId);

            if (chat == null)
            {
                chat = DefaultValues.DefaultEntity;
                chat.ChatId = chatId;
                chat.ValueCurrency = value;

                await AddChatAsync(chat);
            }
            else
            {
                chat.ValueCurrency = value;

                await Context.SaveChangesAsync();
            }
        }

        public async Task SetPercentsAsync(decimal value, long chatId)
        {
            var chat = await Context.ChatSettings.FirstOrDefaultAsync(t => t.ChatId == chatId);

            if (chat == null)
            {
                chat = DefaultValues.DefaultEntity;
                chat.ChatId = chatId;
                chat.Percents = value;
                
                await AddChatAsync(chat);
            }
            else
            {
                chat.Percents = value;

                await Context.SaveChangesAsync();
            }
        }

        public async Task<string> GetCurrencyAsync(long chatId)
        {
            var chat = await Context.ChatSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.ChatId == chatId);

            return chat == null ? (await InitChatAsync(chatId)).ValueCurrency : chat.ValueCurrency;
        }

        public async Task<decimal> GetPercentsAsync(long chatId)
        {
            var chat = await Context.ChatSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.ChatId == chatId)
                ;

            return chat?.Percents ?? (await InitChatAsync(chatId)).Percents;
        }

        public string GetCurrencyFromEmoji(string emoji)
        {
            return GetCurrencyEmojiFromEmoji(emoji).Currency;
        }

        public string GetEmojiFromCurrency(string currency)
        {
            return GetCurrencyEmojiFromCurrency(currency).Emoji;
        }

        public CurrencyEmoji GetCurrencyEmojiFromCurrency(string currency)
        {
            return Context.CurrencyEmojis
                .AsNoTracking()
                .FirstOrDefault(ce => ce.Currency == currency);
        }

        public CurrencyEmoji GetCurrencyEmojiFromEmoji(string emoji)
        {
            return Context.CurrencyEmojis
                .AsNoTracking()
                .FirstOrDefault(ce => ce.Emoji == emoji);
        }

        public CurrencyEmoji GetCurrencyEmoji(long chatId)
        {
            return GetCurrencyEmojiFromCurrency(GetCurrency(chatId));
        }

        public async Task<CurrencyEmoji> GetCurrencyEmojiFromCurrencyAsync(string currency)
        {
            return await Context.CurrencyEmojis
                .AsNoTracking()
                .FirstOrDefaultAsync(ce => ce.Currency == currency);
        }

        public async Task<CurrencyEmoji> GetCurrencyEmojiFromEmojiAsync(string emoji)
        {
            return await Context.CurrencyEmojis
                .AsNoTracking()
                .FirstOrDefaultAsync(ce => ce.Emoji == emoji);
        }

        public async Task<string> GetCurrencyFromEmojiAsync(string emoji)
        {
            return (await GetCurrencyEmojiFromEmojiAsync(emoji)).Currency;
        }

        public async Task<string> GetEmojiFromCurrencyAsync(string currency)
        {
            return (await GetCurrencyEmojiFromCurrencyAsync(currency)).Emoji;
        }

        public async Task SetDisplayCurrenciesAsync(IEnumerable<string> displayCurrencies, long chatId)
        {
            var chat = await Context.ChatSettings.FirstOrDefaultAsync(cs => cs.ChatId == chatId);

            if (chat == null)
            {
                chat = DefaultValues.DefaultEntity;
                chat.ChatId = chatId;
                chat.DisplayCurrencies = displayCurrencies.ToList();

                await AddChatAsync(chat);
            }
            else
            {
                chat.DisplayCurrencies = displayCurrencies.ToList();

                await Context.SaveChangesAsync();
            }
        }

        public async Task AddRangeDisplayCurrenciesAsync(IEnumerable<string> displayCurrencies, long chatId)
        {
            var chat = await Context.ChatSettings.FirstOrDefaultAsync(cs => cs.ChatId == chatId);

            if (chat == null)
            {
                chat = DefaultValues.DefaultEntity;
                chat.ChatId = chatId;
                chat.DisplayCurrencies = displayCurrencies.ToList();

                await AddChatAsync(chat);
            }
            else
            {
                chat.DisplayCurrencies.AddRange(displayCurrencies);

                await Context.SaveChangesAsync();
            }
        }

        public async Task<IReadOnlyList<string>> GetDisplayCurrenciesAsync(long chatId)
        {
            var chat = await Context.ChatSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cs => cs.ChatId == chatId);

            if (chat == null)
            {
                chat = DefaultValues.DefaultEntity;
                chat.ChatId = chatId;

                await AddChatAsync(chat);
            }

            return chat.DisplayCurrencies.AsReadOnly();
        }

        public async Task<IReadOnlyList<CurrencyEmoji>> GetDisplayCurrenciesEmojisAsync(long chatId)
        {
            var chat = await Context.ChatSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cs => cs.ChatId == chatId)
                ;

            if (chat == null)
            {
                chat = DefaultValues.DefaultEntity;
                chat.ChatId = chatId;

                await AddChatAsync(chat);
            }

            var displayCurrencies = chat.DisplayCurrencies;

            var currenciesEmojis = Context.CurrencyEmojis.AsNoTracking();

            return await currenciesEmojis.Where(i => i.Currency.In(displayCurrencies)).ToArrayAsync();
        }

        public async Task<CurrencyEmoji> GetCurrencyEmojiAsync(long chatId)
        {
            return await GetCurrencyEmojiFromCurrencyAsync(await GetCurrencyAsync(chatId));
        }

        public IEnumerable<string> GetAllCurrencies()
        {
            return CurrenciesEmojisRepository.GetCurrencies();
        }

        public IEnumerable<string> GetAllEmojis()
        {
            return CurrenciesEmojisRepository.GetEmojis();
        }

        public async Task<string[]> GetAllCurrenciesAsync()
        {
            return await CurrenciesEmojisRepository.GetCurrenciesAsync();
        }

        public async Task<string[]> GetAllEmojisAsync()
        {
            return await CurrenciesEmojisRepository.GetEmojisAsync();
        }

        /// <summary>
        /// if the chat does not exist in the database, adds the chat. If exists, does nothing.
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        public async Task EnsureChatCreatedAsync(long chatId)
        {
            var chat = await Context.ChatSettings.FirstOrDefaultAsync(i => i.ChatId == chatId);

            if (chat == null)
            {
                await InitChatAsync(chatId);
            }
        }

        public void Dispose()
        {
            Context.Dispose();
            CurrenciesEmojisRepository.Dispose();
        }
    }
}
