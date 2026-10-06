namespace ShaurmaTycoon.Platform
{
    /// <summary>Товар из каталога платформы: id, цена числом (строкой, как отдаёт SDK) и код валюты портала (YAN и др.).</summary>
    public readonly struct ProductPrice
    {
        public readonly string Id;
        public readonly string Value;
        public readonly string CurrencyCode;

        public ProductPrice(string id, string value, string currencyCode)
        {
            Id = id;
            Value = value;
            CurrencyCode = currencyCode;
        }
    }
}
