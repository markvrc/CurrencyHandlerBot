﻿namespace CurrencyHandler.Models.Database.Models
{
    public class ChatSettings
    {
        public long ChatId { get; set; }

        public decimal Percents { get; set; }

        public string ValueCurrency { get; set; }

        public string[] DisplayCurrencies { get; set; }
    }
}
