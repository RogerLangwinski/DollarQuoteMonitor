using System.Text.Json.Serialization;

namespace DollarQuoteMonitor.Models;

/// <summary>
/// Representa a cotação de um par de moedas retornado pela AwesomeAPI.
/// </summary>
public class QuoteModel
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("codein")]
    public string CodeIn { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("bid")]
    public string Bid { get; set; } = string.Empty;

    [JsonPropertyName("ask")]
    public string Ask { get; set; } = string.Empty;

    [JsonPropertyName("create_date")]
    public string CreateDate { get; set; } = string.Empty;

    /// <summary>Valor de compra como decimal.</summary>
    public decimal BidValue => decimal.TryParse(Bid,
        System.Globalization.NumberStyles.Any,
        System.Globalization.CultureInfo.InvariantCulture,
        out var v) ? v : 0m;
}
