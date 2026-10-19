using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CasterDesk.Core;

public sealed record SheetsTarget(string SpreadsheetId, int SheetId, string Title);

// The same input projection drives Excel and Sheets. RAW keeps nicknames and
// map scores as literal text; one request updates inputs without touching artwork.
public sealed class GoogleSheetsTable(HttpClient http, Func<CancellationToken, Task<string>> accessToken)
{
    public const string SheetTitle = "Round Breakdown + Series Layout";
    public static (string Id, int? Gid) ParseLink(string value)
    {
        value = value.Trim();
        if (Regex.IsMatch(value, "^[A-Za-z0-9_-]{20,200}$")) return (value, null);
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "docs.google.com")
            throw new InvalidDataException("Cole o link de uma planilha do Google Sheets.");
        var match = Regex.Match(uri.AbsolutePath, @"^/spreadsheets/d/([A-Za-z0-9_-]{20,200})(?:/|$)");
        if (!match.Success) throw new InvalidDataException("Link de planilha inválido.");
        var gid = Regex.Match(uri.Query + "&" + uri.Fragment.TrimStart('#'), @"(?:[?&]|^)gid=(\d+)(?:&|$)");
        if (gid.Success && !int.TryParse(gid.Groups[1].Value, out _)) throw new InvalidDataException("Identificador de aba inválido.");
        return (match.Groups[1].Value, gid.Success ? int.Parse(gid.Groups[1].Value) : null);
    }
    async Task<JsonDocument> Request(HttpMethod method, string path, object? body, CancellationToken cancellation)
    {
        using var request = new HttpRequestMessage(method, "https://sheets.googleapis.com/v4/spreadsheets/" + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await accessToken(cancellation));
        if (body != null) request.Content = JsonContent.Create(body);
        using var response = await http.SendAsync(request, cancellation);
        if (!response.IsSuccessStatusCode) {
            // Do not log Google's response bodies: they can contain account or document data.
            throw new InvalidOperationException(response.StatusCode switch {
                HttpStatusCode.Unauthorized => "A autorização Google expirou. Reconecte a conta.",
                HttpStatusCode.Forbidden => "Sem permissão para editar. Confira o compartilhamento da planilha e a ativação da Google Sheets API.",
                HttpStatusCode.NotFound => "Planilha não encontrada ou sem acesso para esta conta.",
                HttpStatusCode.TooManyRequests => "Limite temporário do Google. A conexão tentará novamente.",
                _ => "O Google Sheets não respondeu à atualização (HTTP " + (int)response.StatusCode + ")."
            });
        }
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation));
    }
    public async Task<SheetsTarget> VerifyAsync(string link, CancellationToken cancellation = default)
    {
        var (id, gid) = ParseLink(link);
        using var response = await Request(HttpMethod.Get, id + "?fields=sheets(properties(sheetId,title,gridProperties(rowCount,columnCount)))", null, cancellation);
        var matches = response.RootElement.GetProperty("sheets").EnumerateArray().Select(s => s.GetProperty("properties"))
            .Where(p => gid.HasValue ? p.GetProperty("sheetId").GetInt32() == gid.Value : p.GetProperty("title").GetString() == SheetTitle).ToArray();
        if (matches.Length != 1 || matches[0].GetProperty("title").GetString() != SheetTitle)
            throw new InvalidDataException("Selecione a aba “" + SheetTitle + "” do modelo da tabela.");
        var grid = matches[0].GetProperty("gridProperties");
        if (grid.GetProperty("rowCount").GetInt32() < 22 || grid.GetProperty("columnCount").GetInt32() < 9)
            throw new InvalidDataException("A aba precisa conter pelo menos as células A1:I22 do modelo.");
        string range = Uri.EscapeDataString("'" + SheetTitle + "'!A1:I7");
        using var headers = await Request(HttpMethod.Get, id + "/values/" + range + "?fields=values", null, cancellation);
        if (!headers.RootElement.TryGetProperty("values", out var rows)) throw new InvalidDataException("A aba não contém os cabeçalhos do modelo.");
        string Cell(int row, int col) => rows.GetArrayLength() > row && rows[row].GetArrayLength() > col ? rows[row][col].ToString().Trim() : "";
        if (Cell(0,0) != "Time1" || Cell(0,1) != "Time2" || Cell(6,0) != "Rounds" || Cell(6,1) != "Vencedor" || Cell(6,2) != "Formato Vitória")
            throw new InvalidDataException("Os cabeçalhos da aba não correspondem ao modelo da tabela. Nenhuma célula foi alterada.");
        return new(id, matches[0].GetProperty("sheetId").GetInt32(), SheetTitle);
    }
    public static object Batch(SheetsTarget target, IReadOnlyDictionary<string, object> values) => new {
        valueInputOption = "RAW",
        data = values.Select(cell => new { range = "'" + target.Title.Replace("'", "''") + "'!" + cell.Key,
            majorDimension = "ROWS", values = new[] { new[] { cell.Value } } }).ToArray()
    };
    public async Task UpdateAsync(string link, SiegeMatch match, SiegeCatalog catalog, CancellationToken cancellation = default)
    {
        var values = MatchTable.Inputs(match, catalog);
        var target = await VerifyAsync(link, cancellation); // Recheck destination before every write.
        using var response = await Request(HttpMethod.Post, target.SpreadsheetId + "/values:batchUpdate", Batch(target, values), cancellation);
    }
}
