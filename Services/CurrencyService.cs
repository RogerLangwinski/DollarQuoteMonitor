using System.Text.Json;
using DollarQuoteMonitor.Models;

namespace DollarQuoteMonitor.Services;

/// <summary>
/// Consome a API Frankfurter para obter cotações de moedas.
/// </summary>
public class CurrencyService
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private const string BaseUrl = "https://api.frankfurter.app/latest";

    /// <summary>
    /// Retorna a cotação atual USD → BRL.
    /// Lança exceção em caso de falha de rede ou resposta inválida.
    /// </summary>
    public async Task<QuoteModel> GetQuoteAsync(string currencyPair = "USD-BRL")
    {
        // Frankfurter usa ?from=USD&to=BRL
        var response = await _http.GetAsync($"{BaseUrl}?from=USD&to=BRL");

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"API retornou status {(int)response.StatusCode}: {response.ReasonPhrase}");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        // Resposta: { "amount": 1.0, "base": "USD", "date": "2024-01-15", "rates": { "BRL": 4.95 } }
        if (!doc.RootElement.TryGetProperty("rates", out var rates) ||
            !rates.TryGetProperty("BRL", out var brlRate))
        {
            throw new InvalidDataException("Resposta da API não contém a cotação BRL.");
        }

        var quote = new QuoteModel
        {
            Code = "USD",
            CodeIn = "BRL",
            Name = "Dólar Americano/Real Brasileiro",
            Bid = brlRate.GetDecimal().ToString("F4", System.Globalization.CultureInfo.InvariantCulture),
            Ask = brlRate.GetDecimal().ToString("F4", System.Globalization.CultureInfo.InvariantCulture),
            CreateDate = doc.RootElement.GetProperty("date").GetString() ?? DateTime.Now.ToString("yyyy-MM-dd")
        };

        return quote;
    }
}
