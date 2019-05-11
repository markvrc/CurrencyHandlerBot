﻿using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CurrencyHandler.Models.Database.Models;
using CurrencyHandler.Models.Database.Repositories;
using CurrencyHandler.Models.DataCaching;
using CurrencyHandler.Models.HelperClasses;
using Telegram.Bot.Types;
using static System.String;

namespace CurrencyHandler.Models.QueryHandling
{
    public class InlineQueryHandler
    {
        private readonly IReadOnlyList<CurrencyEmoji> currenciesEmojis;

        private readonly CurrenciesEmojisRepository repo;

        public InlineQueryHandler(CurrenciesEmojisRepository repo)
        {
            this.repo = repo;

            if (currenciesEmojis == null)
            {
                currenciesEmojis = repo.GetCurrencyEmojis().ToList().AsReadOnly();
            }
        }

        public async Task HandleAsync(InlineQuery q)
        {
            if (IsNullOrEmpty(q.Query))
                return;

            var bot = await Bot.GetAsync();
            var data = await CurrenciesDataCaching.GetValCursAsync();
            var input = q.Query;

            string currency = null;
            foreach (var i in currenciesEmojis)
                if (input.Contains(i.Currency))
                {
                    currency = i.Currency;
                    break;
                }

            var space = " "; // format: {decimal}{space}{currency} e.g "53.2 UAH" or just {decimal}
            var argsPart = currency == null ? "" : $"{space}{currency}";

            var pattern = $@"[0-9][0-9]*([\.,][0-9][0-9]*)?{argsPart}";

            if (!Regex.IsMatch(input, pattern))
            {
                var text = "format: {decimal}{space}{currency} e.g \"53.2 UAH\" or just {decimal} e.g \"53.2\"";

                await bot.AnswerInlineQueryAsync(
                    q.Id,
                    await InlineAnswerBuilder.ArticleToQueryResultAsync(
                        "Result", text, text)
                );

                return;
            }

            var valueToParse = IsNullOrEmpty(argsPart) ? input : input.Replace(argsPart, "");
            var isValid = decimal.TryParse(valueToParse, out var value);

            if (isValid)
            {
                if (currency == null)
                    currency = DefaultValues.DefaultValueCurrency;

                var currencyEmoji = await repo.GetCurrencyEmojiFromCurrencyAsync(currency);

                var values =
                    await ValuesCalculator.GetCurrenciesValuesAsync(value, currencyEmoji, data, currenciesEmojis);

                var answer1 = await AnswerBuilder.BuildStringFromValuesAsync(values, currencyEmoji);

                await bot.AnswerInlineQueryAsync(
                    q.Id,
                    await InlineAnswerBuilder.ArticleToQueryResultAsync(
                        "Result", answer1, answer1)
                );
            }
        }
    }
}
