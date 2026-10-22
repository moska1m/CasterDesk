using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CasterDesk.Core;

public sealed record SheetsTarget(string SpreadsheetId, int SheetId, string Title)
{
    public override string ToString() => Title;
}

// The same input projection drives Excel and Sheets. RAW keeps nicknames and
// map scores as literal text; one request updates inputs without touching artwork.
public sealed class GoogleSheetsTable(HttpClient http, Func<CancellationToken, Task<string>> accessToken)
{
    public const string SheetTitle = "Round Breakdown + Series Layout";
    public static string TargetLink(SheetsTarget target) => "https://docs.google.com/spreadsheets/d/" + target.SpreadsheetId + "/edit#gid=" + target.SheetId;
    static string Range(string title, string cells) => "'" + title.Replace("'", "''") + "'!" + cells;
    sealed record Sheet(SheetsTarget Target, int Rows, int Columns);
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
    async Task<Sheet[]> ReadSheets(string id, CancellationToken cancellation)
    {
        using var response = await Request(HttpMethod.Get, id + "?fields=sheets(properties(sheetId,title,gridProperties(rowCount,columnCount)))", null, cancellation);
        return response.RootElement.GetProperty("sheets").EnumerateArray().Select(s => s.GetProperty("properties"))
            .Where(p => p.TryGetProperty("gridProperties", out _))
            .Select(p => new Sheet(new(id, p.GetProperty("sheetId").GetInt32(), p.GetProperty("title").GetString()!),
                p.GetProperty("gridProperties").GetProperty("rowCount").GetInt32(), p.GetProperty("gridProperties").GetProperty("columnCount").GetInt32())).ToArray();
    }
    public async Task<IReadOnlyList<SheetsTarget>> ListAsync(string link, CancellationToken cancellation = default)
    {
        var (id, _) = ParseLink(link);
        return (await ReadSheets(id, cancellation)).Select(s => s.Target).ToArray();
    }
    public async Task<SheetsTarget> VerifyAsync(string link, CancellationToken cancellation = default)
    {
        var (id, gid) = ParseLink(link);
        var sheets = await ReadSheets(id, cancellation);
        var matches = sheets.Where(s => gid.HasValue ? s.Target.SheetId == gid.Value : s.Target.Title == SheetTitle).ToArray();
        // Existing links still find the model by its original title. A sole tab
        // can be renamed freely; multi-tab documents require an explicit choice.
        if (!gid.HasValue && matches.Length == 0 && sheets.Length == 1) matches = sheets;
        if (matches.Length != 1)
            throw new InvalidDataException(gid.HasValue ? "A aba selecionada não existe mais. Carregue as abas e escolha novamente." : "Carregue as abas e escolha a que deve receber os dados, ou cole o link dessa aba.");
        var sheet = matches[0]; var target = sheet.Target;
        if (sheet.Rows < 22 || sheet.Columns < 9)
            throw new InvalidDataException("A aba precisa conter pelo menos as células A1:I22 do modelo.");
        string range = Uri.EscapeDataString(Range(target.Title, "A1:I7"));
        using var headers = await Request(HttpMethod.Get, id + "/values/" + range + "?fields=values", null, cancellation);
        if (!headers.RootElement.TryGetProperty("values", out var rows)) throw new InvalidDataException("A aba não contém os cabeçalhos do modelo.");
        string Cell(int row, int col) => rows.GetArrayLength() > row && rows[row].GetArrayLength() > col ? rows[row][col].ToString().Trim() : "";
        if (Cell(0,0) != "Time1" || Cell(0,1) != "Time2" || Cell(6,0) != "Rounds" || Cell(6,1) != "Vencedor" || Cell(6,2) != "Formato Vitória")
            throw new InvalidDataException("Os cabeçalhos da aba não correspondem ao modelo da tabela. Nenhuma célula foi alterada.");
        return target;
    }
    public static object Batch(SheetsTarget target, IReadOnlyDictionary<string, object> values) => new {
        valueInputOption = "RAW",
        data = values.Select(cell => new { range = Range(target.Title, cell.Key),
            majorDimension = "ROWS", values = new[] { new[] { cell.Value } } }).ToArray()
    };
    public async Task UpdateAsync(string link, SiegeMatch match, SiegeCatalog catalog, CancellationToken cancellation = default)
    {
        var values = MatchTable.Inputs(match, catalog);
        var target = await VerifyAsync(link, cancellation); // Recheck destination before every write.
        using var response = await Request(HttpMethod.Post, target.SpreadsheetId + "/values:batchUpdate", Batch(target, values), cancellation);
    }
}
