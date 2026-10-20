using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CasterDesk.Core;

public sealed class ReplayCounter
{
    public int? Kills { get; set; }
    public int? Deaths { get; set; }
    public int? Assists { get; set; }
    public int? InitialKills { get; set; }
    public int? InitialDeaths { get; set; }
    public int? InitialAssists { get; set; }
}
public sealed class ReplayTeam
{
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public int StartingScore { get; set; }
    public int Score { get; set; }
    public bool Won { get; set; }
    public string WinCondition { get; set; } = "";
}
public sealed class ReplayPlayer
{
    public string Username { get; set; } = "";
    public string ProfileID { get; set; } = "";
    public int TeamIndex { get; set; }
}
public sealed class ReplayMap { public long Id { get; set; } public string Name { get; set; } = ""; }
public sealed class ReplayEventType { public string Name { get; set; } = ""; }
public sealed class ReplayEvent
{
    public ReplayEventType Type { get; set; } = new();
    public string Username { get; set; } = "";
    public string Target { get; set; } = "";
    public bool? Headshot { get; set; }
}
public sealed class ReplayDocument
{
    public int Schema { get; set; }
    public string Reader { get; set; } = "";
    public string MatchID { get; set; } = "";
    public DateTimeOffset Timestamp { get; set; }
    public int RoundNumber { get; set; }
    public string Site { get; set; } = "";
    public ReplayMap Map { get; set; } = new();
    public List<ReplayTeam> Teams { get; set; } = [];
    public List<ReplayPlayer> Players { get; set; } = [];
    public Dictionary<string, ReplayCounter> Counters { get; set; } = [];
    public List<ReplayEvent>? Events { get; set; }
    public bool Concluded => Teams.Count(t => t.Won) == 1 && Teams.Sum(t => t.Score - t.StartingScore) == 1;
}
public sealed record ReplayFile(string Name, string Copy, string Sha256, string Json, ReplayDocument Data);
public sealed record ReplayFolderPreview(ReplayDocument? Header, string Error);
public sealed record ReplaySession(string Id, IReadOnlyList<ReplayFile> Files)
{
    public ReplayDocument Last => Files.Where(f => f.Data.Concluded).OrderBy(f => f.Data.RoundNumber).ThenBy(f => f.Data.Timestamp).Last().Data;
    public override string ToString() => $"{Last.Timestamp:g} · round {Last.RoundNumber + 1} · {string.Join(" × ", Last.Teams.Select(t => t.Score))} · {Id[..8]}";
}

public sealed class ReplayImport : IDisposable
{
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    readonly string temporaryDirectory;
    public IReadOnlyList<ReplayFile> Files { get; private set; } = [];
    public IReadOnlyList<ReplaySession> Sessions { get; private set; } = [];
    public List<string> Warnings { get; } = [];
    public string Report { get; private set; } = "";
    ReplayImport(string temporaryDirectory) { this.temporaryDirectory = temporaryDirectory; }

    public static string DefaultDirectory()
    {
        try { using var config = JsonDocument.Parse(File.ReadAllText(AppPaths.ConfigFile("replay-settings.json"))); return config.RootElement.GetProperty("Directory").GetString() ?? ""; } catch { }
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Ubisoft", "Ubisoft Game Launcher", "games", "Tom Clancy's Rainbow Six Siege", "MatchReplay");
        return Directory.Exists(path) ? path : "";
    }
    public static void RememberDirectory(string path) => MatchFiles.AtomicText(AppPaths.ConfigFile("replay-settings.json"), JsonSerializer.Serialize(new { Directory = path }));

    public static async Task<ReplayImport> ReadAsync(IEnumerable<string> folders, SiegeMatch match, MapRecord map, IProgress<string>? progress, CancellationToken token)
    {
        string root = Path.Combine(Path.GetTempPath(), "casterdesk-replays-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); var result = new ReplayImport(root);
        try {
            var sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string folder in folders) {
                if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0) throw new IOException("Selecione uma pasta local, sem atalhos de diretório.");
                foreach (string file in Directory.GetFiles(folder, "*.rec", SearchOption.TopDirectoryOnly)) sources.Add(Path.GetFullPath(file));
            }
            if (sources.Count is 0 or > 150) throw new InvalidDataException("Selecione entre 1 e 150 arquivos de round nas pastas da partida.");
            long size = 0; var files = new List<ReplayFile>(); var hashes = new HashSet<string>();
            foreach (string source in sources.Order(StringComparer.OrdinalIgnoreCase)) {
                token.ThrowIfCancellationRequested();
                progress?.Report($"Lendo {files.Count + 1}/{sources.Count} · {Path.GetFileName(source)}");
                var info = new FileInfo(source);
                if ((info.Attributes & FileAttributes.ReparsePoint) != 0 || info.Length is <= 0 or > 200_000_000 || (size += info.Length) > 2_000_000_000) throw new InvalidDataException("Replay inválido ou seleção maior que 2 GB.");
                long length = info.Length; var time = info.LastWriteTimeUtc;
                await Task.Delay(700, token); info.Refresh();
                if (info.Length != length || info.LastWriteTimeUtc != time) throw new IOException("O Siege ainda está gravando um replay. Aguarde e tente novamente.");
                string copy = Path.Combine(root, files.Count.ToString("D3") + ".rec");
                using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var output = new FileStream(copy, FileMode.CreateNew, FileAccess.Write, FileShare.None)) await input.CopyToAsync(output, token);
                info.Refresh();
                if (info.Length != length || info.LastWriteTimeUtc != time) throw new IOException("O replay mudou durante a cópia. Tente novamente após sair do mapa.");
                string hash;
                using (var input = File.OpenRead(copy)) hash = Convert.ToHexString(await SHA256.HashDataAsync(input, token));
                if (!hashes.Add(hash)) { File.Delete(copy); continue; }
                string json = await ReadDocument(copy, token);
                var data = JsonSerializer.Deserialize<ReplayDocument>(json, JsonOptions) ?? throw new InvalidDataException("Replay sem dados.");
                ValidateDocument(data);
                if (MapId(data.Map.Name) != map.MapId) { result.Warnings.Add($"Outro mapa ignorado: {Path.GetFileName(source)} ({data.Map.Name})."); File.Delete(copy); continue; }
                if (data.Teams.Any(t => !new[] { match.TeamA, match.TeamB }.Any(n => SameName(n, t.Name)))) throw new InvalidDataException("As equipes do replay não correspondem às da partida. Confira a seleção e os nomes antes de importar.");
                files.Add(new(Path.GetFileName(source), copy, hash, json, data));
            }
            if (files.Count == 0) throw new InvalidDataException("Nenhum replay corresponde ao mapa selecionado.");
            result.Files = files;
            result.Sessions = files.GroupBy(f => f.Data.MatchID).Where(g => g.Any(f => f.Data.Concluded)).OrderBy(g => g.Min(f => f.Data.Timestamp)).Select(g => new ReplaySession(g.Key, g.ToArray())).ToArray();
            if (result.Sessions.Count == 0) throw new InvalidDataException("Não há round concluído nessa seleção.");
            var duplicates = files.GroupBy(f => (f.Data.MatchID, f.Data.RoundNumber)).Where(g => g.Count(f => f.Data.Concluded) > 1);
            if (duplicates.Any()) throw new InvalidDataException("Há versões diferentes do mesmo round. Selecione somente a tentativa válida.");
            if (files.Any(f => !f.Data.Concluded)) result.Warnings.Add("Há arquivos sem conclusão. Eventos dessas tentativas não entram na soma; o acumulado restaurado pode cobrir o resultado válido.");
            foreach (var f in files.Where(f => f.Data.Concluded)) {
                int i = f.Data.RoundNumber;
                if (i < map.Rounds.Count) {
                    string winner = f.Data.Teams.Single(t => t.Won).Name;
                    if (!SameName(winner, match.Team(map.Rounds[i].Winner))) result.Warnings.Add($"Round {i + 1}: vencedor diverge do registro manual.");
                }
            }
            return result;
        } catch { result.Dispose(); throw; }
    }

    // A folder label is only a navigation aid. Import always rereads and validates
    // every selected round before any values can be applied to the match.
    public static async Task<ReplayFolderPreview> PreviewFolderAsync(string folder, CancellationToken token)
    {
        try {
            token.ThrowIfCancellationRequested();
            if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0) throw new IOException("Pasta não disponível para pré-leitura.");
            var files = Directory.GetFiles(folder, "*.rec").Order(StringComparer.OrdinalIgnoreCase).Take(3).ToArray();
            foreach (string file in files) {
                token.ThrowIfCancellationRequested();
                try {
                    var info = new FileInfo(file);
                    if ((info.Attributes & FileAttributes.ReparsePoint) != 0 || info.Length is <= 0 or > 200_000_000) continue;
                    long length = info.Length; var time = info.LastWriteTimeUtc;
                    string json = await ReadDocument(file, token, headerOnly: true);
                    info.Refresh();
                    if (info.Length != length || info.LastWriteTimeUtc != time) continue;
                    var header = JsonSerializer.Deserialize<ReplayDocument>(json, JsonOptions);
                    if (header == null || header.Schema != 1 || !Guid.TryParse(header.MatchID, out _) || header.Teams.Count != 2 || header.Map.Id <= 0 || header.Map.Name.Length > 100 || header.Teams.Any(t => t.Name.Length > 100)) continue;
                    return new(header, "");
                } catch (OperationCanceledException) { token.ThrowIfCancellationRequested(); }
                catch (Exception ex) when (ex is IOException or InvalidDataException or JsonException or System.ComponentModel.Win32Exception) { }
            }
            return new(null, "Não identificado · arquivo incompleto ou incompatível");
        } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return new(null, "Pré-leitura indisponível · confira a pasta"); }
    }

    static async Task<string> ReadDocument(string file, CancellationToken token, bool headerOnly = false)
    {
        string reader = Path.Combine(AppContext.BaseDirectory, "assets", "replay", "casterdesk-replay.exe");
        if (!File.Exists(reader)) throw new IOException("O leitor de replay não está incluído nesta instalação.");
        var start = new ProcessStartInfo(reader) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = Encoding.UTF8 };
        if (headerOnly) start.ArgumentList.Add("--header");
        start.ArgumentList.Add(file);
        using var process = Process.Start(start) ?? throw new IOException("Não foi possível abrir o leitor de replay.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token); timeout.CancelAfter(TimeSpan.FromSeconds(headerOnly ? 8 : 45));
        var output = process.StandardOutput.ReadToEndAsync(timeout.Token); var error = process.StandardError.ReadToEndAsync(timeout.Token);
        try { await process.WaitForExitAsync(timeout.Token); string json = await output, detail = await error; if (process.ExitCode != 0) throw new InvalidDataException("Replay incompatível ou incompleto: " + detail[..Math.Min(detail.Length, 250)]); if (json.Length > (headerOnly ? 100_000 : 8_000_000)) throw new InvalidDataException("Saída do replay maior que o limite."); return json; }
        finally { if (!process.HasExited) { try { process.Kill(true); } catch { } } }
    }

    public static void ValidateDocument(ReplayDocument d)
    {
        if (d.Schema != 1 || !Guid.TryParse(d.MatchID, out _) || d.RoundNumber is < 0 or > 40 || d.Teams.Count != 2 || d.Teams.Select(t=>t.Name.Trim().ToUpperInvariant()).Distinct().Count()!=2 || d.Teams.Any(t => t.Score is < 0 or > 30 || t.StartingScore is < 0 or > 30) || d.Players.Count != 10 || d.Players.Select(p=>p.Username).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=10 || d.Players.Any(p => p.TeamIndex is < 0 or > 1 || string.IsNullOrWhiteSpace(p.Username) || p.Username.Length > 60 || !Guid.TryParse(p.ProfileID, out _)) || d.Players.Select(p => p.ProfileID).Distinct().Count() != 10 || d.Players.Count(p => p.TeamIndex == 0) != 5) throw new InvalidDataException("A leitura não identificou um round válido com dez jogadores. Use revisão manual.");
        if (d.Counters.Values.Any(c => new[] { c.Kills, c.Deaths, c.Assists, c.InitialKills, c.InitialDeaths, c.InitialAssists }.Any(n => n is < 0 or > 99))) throw new InvalidDataException("Contadores do replay fora dos limites.");
    }
    public static string MapId(string name) => name.Replace("Y10", "", StringComparison.OrdinalIgnoreCase).ToLowerInvariant() switch {
        "villa" => "villa", "bank" => "bank", "chalet" => "chalet", "clubhouse" => "clubhouse", "border" => "border", "consulate" => "consulate", "kafedostoyevsky" => "kafe-dostoyevsky", "nighthavenlabs" => "nighthaven-labs", "fortress" => "fortress", "lair" => "lair", _ => ""
    };
    public static bool SameName(string a, string b) => string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);

    // Hints only: full import still validates and requires explicit session classification.
    public static string ContinuationHint(ReplayDocument a, ReplayDocument b)
    {
        if (a.Map.Id <= 0 || a.Map.Id != b.Map.Id || a.Teams.Count != 2 || b.Teams.Count != 2 || a.Teams.Any(t => string.IsNullOrWhiteSpace(t.Name)) || b.Teams.Any(t => string.IsNullOrWhiteSpace(t.Name)) || a.Teams.Select(t=>t.Name.Trim().ToUpperInvariant()).Distinct().Count()!=2 || b.Teams.Select(t=>t.Name.Trim().ToUpperInvariant()).Distinct().Count()!=2 || !a.Teams.All(t => b.Teams.Any(u => SameName(t.Name, u.Name)))) return "";
        if (Guid.TryParse(a.MatchID, out _) && SameName(a.MatchID, b.MatchID)) return "Mesma sessão";
        var first = a.Timestamp <= b.Timestamp ? a : b; var next = ReferenceEquals(first, a) ? b : a;
        if (first.Timestamp == default || next.Timestamp - first.Timestamp > TimeSpan.FromHours(3) || next.RoundNumber <= first.RoundNumber || next.Teams.Sum(t => t.StartingScore) == 0) return "";
        if (next.Teams.Any(t => t.StartingScore < first.Teams.Single(u => SameName(t.Name, u.Name)).StartingScore)) return "";
        if (first.Players.Count == 10 && next.Players.Count == 10 && !first.Players.Select(p => p.Username.Trim().ToUpperInvariant()).Order().SequenceEqual(next.Players.Select(p => p.Username.Trim().ToUpperInvariant()).Order())) return "";
        return "Possível continuação";
    }

    // Each transition must be explicitly classified by the operator: true = new lobby, false = restored cumulative totals.
    public MapStatistics Consolidate(SiegeMatch match, MapRecord map, IReadOnlyList<bool> newLobby)
    {
        if (newLobby.Count != Sessions.Count - 1) throw new InvalidDataException("Classifique cada retorno ou novo lobby antes de aplicar.");
        bool gapBetweenSessions=false;
        var totals = new Dictionary<string, (ReplayPlayer Player, int Team, int? K, int? D, int? A)>();
        for (int s = 0; s < Sessions.Count; s++) {
            var d = Sessions[s].Last;
            if (s > 0) {
                var previous = Sessions[s-1].Last;
                foreach (var team in d.Teams) if (team.Score < previous.Teams.Single(t => SameName(t.Name, team.Name)).Score) throw new InvalidDataException("Os placares das sessões não formam uma continuação. Selecione somente os trechos válidos.");
                var first=Sessions[s].Files.OrderBy(f=>f.Data.RoundNumber).ThenBy(f=>f.Data.Timestamp).First().Data;
                foreach(var team in first.Teams){int lastScore=previous.Teams.Single(t=>SameName(t.Name,team.Name)).Score;if(team.StartingScore<lastScore)throw new InvalidDataException("Há sobreposição de rounds entre lobbies. Selecione a tentativa válida antes de somar.");if(newLobby[s-1]&&team.StartingScore>lastScore)gapBetweenSessions=true;}
            }
            foreach (var p in d.Players) {
                string teamName = d.Teams[p.TeamIndex].Name; int team = SameName(teamName, match.TeamA) ? 0 : 1;
                d.Counters.TryGetValue(p.Username, out var c); c ??= new();
                int? k = c.Kills, deaths = c.Deaths, a = c.Assists;
                if (s > 0) {
                    if (!totals.TryGetValue(p.ProfileID, out var old) || old.Team != team) throw new InvalidDataException("A escalação mudou entre sessões. A associação precisa de revisão manual.");
                    if (newLobby[s-1]) { k = Add(old.K, k); deaths = Add(old.D, deaths); a = Add(old.A, a); }
                    else if ((k.HasValue && old.K.HasValue && k < old.K) || (deaths.HasValue && old.D.HasValue && deaths < old.D) || (a.HasValue && old.A.HasValue && a < old.A)) throw new InvalidDataException("Os acumulados diminuíram. Confira se houve novo lobby ou reset das estatísticas.");
                }
                totals[p.ProfileID] = (p, team, k, deaths, a);
            }
        }
        var players = totals.Select(item => {
            var p = item.Value;
            var old = match.Maps.SelectMany(m => m.Statistics?.Players ?? []).FirstOrDefault(x => x.Team == p.Team && (x.UbisoftProfileId == item.Key || SameName(x.Name, p.Player.Username)));
            return new PlayerStatistic { Team = p.Team, Name = p.Player.Username, PlayerId = old?.PlayerId ?? "", UbisoftProfileId = item.Key, Kills = p.K, Deaths = p.D, Assists = p.A };
        }).OrderBy(p => p.Team).ThenBy(p => p.Name).ToList();
        var last = Sessions[^1].Last;
        int finalA = last.Teams.Single(t => SameName(t.Name, match.TeamA)).Score, finalB = last.Teams.Single(t => SameName(t.Name, match.TeamB)).Score;
        var covered = Files.Where(f => f.Data.Concluded).Select(f => f.Data.RoundNumber).Distinct().Order().ToArray();
        var savedScore=map.Score();
        bool complete = !gapBetweenSessions && players.Count == 10 && players.All(p => p.Kills.HasValue && p.Deaths.HasValue && p.Assists.HasValue) && map.Rounds.Count == finalA + finalB && savedScore.A==finalA && savedScore.B==finalB;
        if (Sessions[0].Files.Min(f => f.Data.RoundNumber) > 0) complete = false;
        var lines = new List<string> { $"{match.TeamA} {finalA} × {finalB} {match.TeamB}", $"{Files.Count} arquivos · {Sessions.Count} sessões · {covered.Length}/{finalA + finalB} rounds com eventos concluídos", complete ? "E/M/A dos acumulados disponíveis. Confira antes de confirmar." : "COBERTURA PARCIAL: confira campos ausentes e trechos selecionados." };
        lines.AddRange(Warnings.Distinct());
        if(gapBetweenSessions)lines.Add("Faltam trechos entre lobbies. Os totais não cobrem a partida inteira.");
        if (covered.Length != finalA + finalB) lines.Add("Há lacunas no histórico de eventos. Os snapshots podem cobrir E/M/A, mas não reconstituem eventos ausentes.");
        foreach(var p in players) {
            var details=new PlayerReplayDetails { ExpectedRounds=finalA+finalB };
            foreach(var file in Files.Where(f=>f.Data.Concluded && f.Data.Events?.Count>0)){
                var doc=file.Data;
                var participant=doc.Players.SingleOrDefault(x=>x.ProfileID==p.UbisoftProfileId);
                if(participant==null)continue;
                string nick=participant.Username; var events=doc.Events!; details.ObservedRounds++;
                details.ObservedKills+=events.Count(e=>e.Type.Name=="Kill"&&SameName(e.Username,nick));
                details.Headshots+=events.Count(e=>e.Type.Name=="Kill"&&SameName(e.Username,nick)&&e.Headshot==true);
                if(events.FirstOrDefault(e=>e.Type.Name=="Kill") is {} firstKill && SameName(firstKill.Username,nick)) details.OpeningKills++;
                if(events.FirstOrDefault(e=>e.Type.Name is "Kill" or "Death") is {} firstDeath && SameName(firstDeath.Type.Name=="Kill"?firstDeath.Target:firstDeath.Username,nick))details.OpeningDeaths++;
                details.Plants+=events.Count(e=>e.Type.Name=="DefuserPlantComplete"&&SameName(e.Username,nick));
                details.Defuses+=events.Count(e=>e.Type.Name=="DefuserDisableComplete"&&SameName(e.Username,nick));
            }
            p.ReplayDetails=details;
        }
        foreach (var p in players) {
            var old = map.Statistics?.Players.FirstOrDefault(x => x.Team == p.Team && (x.UbisoftProfileId == p.UbisoftProfileId || SameName(x.Name, p.Name)));
            string current = old == null ? "sem registro" : $"{old.Kills}/{old.Deaths}/{old.Assists}";
            lines.Add($"{match.Team(p.Team)} · {p.Name}: {p.Kills?.ToString() ?? "?"}/{p.Deaths?.ToString() ?? "?"}/{p.Assists?.ToString() ?? "?"} (salvo: {current})");
            var extra=p.ReplayDetails!;
            lines.Add($"  HS {extra.Headshots} · primeira eliminação {extra.OpeningKills} · primeira morte {extra.OpeningDeaths} · plants {extra.Plants} · desarmes {extra.Defuses} · eventos {extra.ObservedRounds}/{extra.ExpectedRounds} rounds{(extra.Partial?" · PARCIAL":"")}");
        }
        lines.Add("ROUND A ROUND · apenas consulta; não altera os registros manuais");
        foreach(var f in Files.OrderBy(f=>f.Data.RoundNumber).ThenBy(f=>f.Data.Timestamp)){
            var winner=f.Data.Teams.FirstOrDefault(t=>t.Won);string code=winner?.WinCondition??"";
            string ending=code switch{"KilledOpponents"=>"Eliminação","DisabledDefuser"=>"Defuser desativado","DefusedBomb"=>"Defuser concluído","Time"=>"Tempo esgotado",_=>"Não informado"};
            lines.Add($"R{f.Data.RoundNumber+1:00} · {f.Data.Site} · {(f.Data.Concluded?winner?.Name:"Interrompido")} · {ending} (motivo inferido pelo leitor)");
        }
        Report = string.Join(Environment.NewLine, lines);
        var data = new MapStatistics { Status = complete ? "complete" : "partial", Source = "replay", Players = players };
        StatisticsData.Identify(match, data); StatisticsData.Validate(data); return data;
    }
    static int? Add(int? a, int? b) => a.HasValue && b.HasValue ? checked(a.Value + b.Value) : null;

    public void WriteArchive(string path)
    {
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var f in Files) {
            using (var input = File.OpenRead(f.Copy)) if (!string.Equals(Convert.ToHexString(SHA256.HashData(input)), f.Sha256, StringComparison.Ordinal)) throw new IOException("A cópia de um replay mudou após a leitura. Importe novamente.");
            string folder = f.Sha256[..16] + "/";
            zip.CreateEntryFromFile(f.Copy, folder + f.Name, CompressionLevel.Fastest);
            using var json = new StreamWriter(zip.CreateEntry(folder + f.Name + ".json").Open()); json.Write(f.Json);
        }
        using var report = new StreamWriter(zip.CreateEntry("LEITURA.txt").Open()); report.Write(Report);
    }
    public void Dispose()
    {
        string root = Path.GetFullPath(temporaryDirectory);
        if (Path.GetDirectoryName(root) == Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.GetTempPath())) && Path.GetFileName(root).StartsWith("casterdesk-replays-", StringComparison.Ordinal)) {
            try { Directory.Delete(root, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
