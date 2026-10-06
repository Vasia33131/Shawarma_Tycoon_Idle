using System;
using System.Collections.Generic;
using System.Globalization;
using ShaurmaTycoon.Localization;
using ShaurmaTycoon.Platform;

namespace ShaurmaTycoon.Shop
{
    /// <summary>
    /// Цены товаров из каталога платформы (требования Яндекса 1.13.2, 1.13.4, 1.13.6): число и валюта — из SDK, а не из конфига.
    /// Пока каталог не пришёл или платформа его не дала — показывается DisplayPrice из конфига в янах.
    /// Каталог загружен, а товара в нём нет — товар в консоли не создан или выключен: в игре его не показываем.
    /// </summary>
    public sealed class PriceCatalog
    {
        public const string YanCode = "YAN";

        private readonly Dictionary<string, ProductPrice> _prices = new Dictionary<string, ProductPrice>(8);

        /// <summary>Каталог получен (пусть даже пустой) — по нему можно скрывать отсутствующие товары.</summary>
        public bool IsLoaded { get; private set; }

        /// <summary>Каталог пришёл; UI перерисовывает цены.</summary>
        public event Action Changed;

        public void Load(IPlatformService platform)
        {
            if (platform == null)
                return;
            platform.GetCatalog(Apply);
        }

        /// <param name="products">null — каталог недоступен (нет SDK, ошибка сети): остаются цены из конфига.</param>
        public void Apply(ProductPrice[] products)
        {
            if (products == null)
                return;

            _prices.Clear();
            for (int i = 0; i < products.Length; i++)
            {
                if (!string.IsNullOrEmpty(products[i].Id))
                    _prices[products[i].Id] = products[i];
            }

            IsLoaded = true;
            Changed?.Invoke();
        }

        /// <summary>Товар можно показывать: каталог ещё не загружен или товар в нём есть.</summary>
        public bool IsAvailable(string productId)
        {
            return !IsLoaded || (productId != null && _prices.ContainsKey(productId));
        }

        /// <summary>Текст цены для кнопки: «99 ян» / «99 YAN»; другая валюта портала — «99 &lt;код&gt;».</summary>
        public string Format(Loc loc, string productId, int fallbackYan)
        {
            if (productId != null && _prices.TryGetValue(productId, out ProductPrice price) && !string.IsNullOrEmpty(price.Value))
            {
                if (string.IsNullOrEmpty(price.CurrencyCode) || string.Equals(price.CurrencyCode, YanCode, StringComparison.OrdinalIgnoreCase))
                    return loc.Format("shop.price_yan", price.Value);
                return price.Value + " " + price.CurrencyCode;
            }

            return loc.Format("shop.price_yan", fallbackYan.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Разбор ответа моста: «id=цена=валюта;id=цена=валюта». Пустая строка — пустой каталог.</summary>
        public static ProductPrice[] Parse(string payload)
        {
            if (string.IsNullOrEmpty(payload))
                return new ProductPrice[0];

            string[] items = payload.Split(';');
            var result = new List<ProductPrice>(items.Length);
            for (int i = 0; i < items.Length; i++)
            {
                string[] parts = items[i].Split('=');
                if (parts.Length < 2 || parts[0].Length == 0)
                    continue;
                result.Add(new ProductPrice(parts[0], parts[1], parts.Length > 2 ? parts[2] : string.Empty));
            }
            return result.ToArray();
        }
    }
}
