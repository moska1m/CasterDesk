using System.IO.Compression;
using System.Xml.Linq;

namespace CasterDesk.Core;

// Updates only the input cells of the match table template. Its artwork-directory cells,
// labels, styles and validations are intentionally outside this write set.
public static class MatchTable
{
    static readonly XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    public static string DefaultPath => Path.Combine(AppPaths.DataDirectory, "Tabelas", "partida-atual.xlsx");
    public static string TemplatePath => Path.Combine(AppContext.BaseDirectory, "assets", "match-table-template.xlsx");
    public static MapRecord? CurrentMap(SiegeMatch match) => match.Complete
        ? match.Maps.LastOrDefault(m => m.Confirmed) : match.Maps.FirstOrDefault(m => !m.Confirmed) ?? match.Maps.LastOrDefault();

    public static Dictionary<string, object> Inputs(SiegeMatch match, SiegeCatalog catalog, MapRecord? map = null)
    {
        if (match.Format == "MD5" || match.Maps.Count > 3)
            throw new InvalidOperationException("O modelo de tabela aceita MD1 e MD3. MD5 precisa de um modelo ampliado; nenhum arquivo foi substituído.");
        map ??= CurrentMap(match);
        var values = new Dictionary<string, object> {
            ["A2"] = match.TeamAIsDefault ? "Equipe Azul" : match.TeamA,
            ["B2"] = match.TeamBIsDefault ? "Equipe Laranja" : match.TeamB,
            ["E2"] = match.Maps.Count(m => m.Confirmed && m.Score().Winner == 0),
            ["F2"] = match.Maps.Count(m => m.Confirmed && m.Score().Winner == 1),
            ["B4"] = Team(map?.InitialAttack ?? -1), ["B5"] = Team(map?.OtAttack ?? -1)
        };
        for (int i = 0; i < 3; i++) {
            string col = ((char)('G' + i)).ToString(); var item = match.Maps.ElementAtOrDefault(i);
            bool live = item != null && item == CurrentMap(match) && !match.Complete && !item.Confirmed && (item.InitialAttack >= 0 || item.HasData);
            values[col + "2"] = item == null ? "" : catalog.Maps.Single(m => m.Id == item.MapId).Name;
            values[col + "4"] = item?.Confirmed == true ? "JOGADO" : live ? "AO VIVO" : "NÃO JOGADO";
            var score = item?.Score();
            values[col + "8"] = item?.Confirmed == true ? $"{score!.Value.A}-{score.Value.B}" : live ? "AO VIVO" : "-";
        }
        for (int i = 0; i < 15; i++) {
            var round = map?.Rounds.ElementAtOrDefault(i);
            values["B" + (i + 8)] = Team(round?.Winner ?? -1);
            values["C" + (i + 8)] = round?.Ending switch {
                "elimination" => "Eliminação", "defuser_completed" or "defuser_disabled" => "Desativador", "time" => "Tempo", _ => ""
            };
        }
        return values;
    }
    static string Team(int team) => team == 0 ? "Time1" : team == 1 ? "Time2" : "";

    public static byte[] Render(byte[] source, IReadOnlyDictionary<string, object> values)
    {
        using var output = new MemoryStream(); output.Write(source);
        using (var zip = new ZipArchive(output, ZipArchiveMode.Update, true)) {
            var entry = zip.GetEntry("xl/worksheets/sheet1.xml") ?? throw new InvalidDataException("Use uma cópia do modelo de tabela do CasterDesk.");
            XDocument doc; using (var stream = entry.Open()) doc = XDocument.Load(stream);
            var data = doc.Root!.Element(S + "sheetData") ?? throw new InvalidDataException("Tabela inválida.");
            // Reject unrelated workbooks rather than silently overwrite their data.
            using (var stream = zip.GetEntry("xl/workbook.xml")!.Open()) {
                var book = XDocument.Load(stream);
                if (!book.Descendants(S + "sheet").Any(e => (string?)e.Attribute("name") == "Round Breakdown + Series Layout"))
                    throw new InvalidDataException("O destino não usa o modelo de tabela do CasterDesk. Escolha outro arquivo.");
            }
            foreach (var pair in values) {
                int number = int.Parse(new string(pair.Key.Where(char.IsDigit).ToArray()));
                var row = data.Elements(S + "row").FirstOrDefault(r => (int?)r.Attribute("r") == number);
                if (row == null) { row = new XElement(S + "row", new XAttribute("r", number)); var next = data.Elements(S + "row").FirstOrDefault(r => (int?)r.Attribute("r") > number); if (next == null) data.Add(row); else next.AddBeforeSelf(row); }
                var cell = row.Elements(S + "c").FirstOrDefault(c => (string?)c.Attribute("r") == pair.Key);
                if (cell == null) { cell = new XElement(S + "c", new XAttribute("r", pair.Key)); var next = row.Elements(S + "c").FirstOrDefault(c => string.CompareOrdinal(new string(((string?)c.Attribute("r") ?? "").TakeWhile(char.IsLetter).ToArray()), new string(pair.Key.TakeWhile(char.IsLetter).ToArray())) > 0); if (next == null) row.Add(cell); else next.AddBeforeSelf(cell); }
                cell.Elements().Remove();
                if (pair.Value is int numeric) { cell.SetAttributeValue("t", "n"); cell.Add(new XElement(S + "v", numeric)); }
                else { cell.SetAttributeValue("t", "inlineStr"); cell.Add(new XElement(S + "is", new XElement(S + "t", new XAttribute(XNamespace.Xml + "space", "preserve"), pair.Value.ToString() ?? ""))); }
            }
            entry.Delete(); using var target = zip.CreateEntry("xl/worksheets/sheet1.xml").Open(); doc.Save(target);
        }
        return output.ToArray();
    }

    public static string ArchivePath(SiegeMatch match) => Path.Combine(MatchFiles.DirectoryFor(match), "Tabelas", "partida.xlsx");
    public static void Save(SiegeMatch match, SiegeCatalog catalog, string livePath, bool writeLive = true)
    {
        var inputs = Inputs(match, catalog); // Validate before reading or writing any destination.
        livePath = Path.GetFullPath(livePath);
        if (!string.Equals(Path.GetExtension(livePath), ".xlsx", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Escolha um arquivo .xlsx.");
        string archive = ArchivePath(match);
        string tables = Path.GetFullPath(Path.Combine(MatchStore.Folder)) + Path.DirectorySeparatorChar;
        if (livePath.StartsWith(tables, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("O arquivo fixo deve ficar fora da pasta Partidas, para preservar as cópias de cada jogo.");
        // Carry manual artwork paths from the live file across matches. If Excel
        // locks it, still archive the new match from its last valid copy/template.
        byte[] source;
        try { source = File.Exists(livePath) ? File.ReadAllBytes(livePath) : File.ReadAllBytes(File.Exists(archive) ? archive : TemplatePath); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { source = File.ReadAllBytes(File.Exists(archive) ? archive : TemplatePath); }
        byte[] current = Render(source, inputs);
        lock (FileBatch.Gate) {
            using var batch = new FileBatch(); batch.Add(archive, p => File.WriteAllBytes(p, current));
            foreach (var map in match.Maps.Where(m => m.HasData || m.InitialAttack >= 0 || m.Confirmed)) {
                string id = catalog.Maps.Single(m => m.Id == map.MapId).Id;
                if (id != Path.GetFileName(id) || id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new InvalidDataException("Mapa inválido.");
                byte[] individual = Render(source, Inputs(match, catalog, map));
                batch.Add(Path.Combine(Path.GetDirectoryName(archive)!, "mapa-" + id + ".xlsx"), p => File.WriteAllBytes(p, individual));
            }
            batch.Commit();
        }
        if (!writeLive) return;
        Directory.CreateDirectory(Path.GetDirectoryName(livePath)!);
        string temp = livePath + ".tmp-" + Guid.NewGuid().ToString("N");
        try { File.WriteAllBytes(temp, current); File.Move(temp, livePath, true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { throw new IOException("A cópia da partida foi salva. Não foi possível substituir o arquivo fixo. Feche-o no Excel e verifique a permissão da pasta; depois clique em Atualizar agora.", ex); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
