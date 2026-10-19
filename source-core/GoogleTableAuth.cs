using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace CasterDesk.Core;

// User tokens are encrypted with Windows DPAPI outside match data/backups.
// The installer may include the public desktop OAuth client configuration;
// this identifies the application and never contains a user's authorization.
public sealed class GoogleTableAuth
{
    const string Scope = "https://www.googleapis.com/auth/spreadsheets";
    readonly HttpClient http;
    readonly string file;
    readonly SemaphoreSlim gate = new(1, 1);
    sealed class Credentials {
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public DateTimeOffset Expires { get; set; }
    }
    Credentials credentials;
    public bool Configured => credentials.ClientId.Length > 0;
    public bool Connected => credentials.RefreshToken.Length > 0;
    public GoogleTableAuth(HttpClient http, string? storage = null, string? desktopClientPath = null)
    {
        this.http = http;
        string identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(AppPaths.DataDirectory).ToUpperInvariant())))[..20];
        file = storage ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CasterDesk", "Auth", identity, "google.bin");
        try { credentials = File.Exists(file) ? JsonSerializer.Deserialize<Credentials>(Unprotect(File.ReadAllBytes(file))) ?? new() : new(); }
        catch { credentials = new(); } // Another Windows user/machine must sign in again.
        if (!Configured) {
            string? bundled = desktopClientPath ?? (storage == null ? Path.Combine(AppContext.BaseDirectory, "assets", "google-desktop-client.json") : null);
            if (bundled != null && File.Exists(bundled)) credentials = ReadClient(bundled);
        }
    }
    static Credentials ReadClient(string path)
    {
        if (new FileInfo(path).Length > 100_000) throw new InvalidDataException("Arquivo de cliente Google inválido.");
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        if (!json.RootElement.TryGetProperty("installed", out var installed)) throw new InvalidDataException("Crie um cliente OAuth do tipo Aplicativo para computador.");
        string id = installed.GetProperty("client_id").GetString() ?? "", secret = installed.TryGetProperty("client_secret", out var s) ? s.GetString() ?? "" : "";
        if (!id.EndsWith(".apps.googleusercontent.com", StringComparison.Ordinal) || id.Length > 250 || secret.Length > 500)
            throw new InvalidDataException("Cliente OAuth inválido.");
        return new Credentials { ClientId = id, ClientSecret = secret };
    }
    public async Task ImportClientAsync(string path)
    {
        var next = ReadClient(path);
        await gate.WaitAsync();
        try { Save(next); credentials = next; }
        finally { gate.Release(); }
    }
    public async Task LoginAsync(Action<string> openBrowser, CancellationToken cancellation = default)
    {
        await gate.WaitAsync(cancellation);
        try {
            if (!Configured) throw new InvalidOperationException("O login Google não está configurado nesta instalação. Instale a edição com conexão Google ou use as opções avançadas.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation); timeout.CancelAfter(TimeSpan.FromMinutes(3));
            string verifier = Base64(RandomNumberGenerator.GetBytes(48)), state = Base64(RandomNumberGenerator.GetBytes(32));
            // Google accepts dynamic loopback ports for desktop OAuth clients.
            int port; var reserve = new TcpListener(IPAddress.Loopback, 0); reserve.Start(); port = ((IPEndPoint)reserve.LocalEndpoint).Port; reserve.Stop();
            string redirect = $"http://127.0.0.1:{port}/oauth2callback/";
            using var listener = new HttpListener(); listener.Prefixes.Add(redirect); listener.Start();
            var query = new Dictionary<string, string> {
                ["client_id"] = credentials.ClientId, ["redirect_uri"] = redirect, ["response_type"] = "code", ["scope"] = Scope,
                ["state"] = state, ["code_challenge"] = Base64(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))),
                ["code_challenge_method"] = "S256", ["access_type"] = "offline", ["prompt"] = "consent select_account"
            };
            openBrowser("https://accounts.google.com/o/oauth2/v2/auth?" + string.Join("&", query.Select(p => Uri.EscapeDataString(p.Key) + "=" + Uri.EscapeDataString(p.Value))));
            while (true) {
                var context = await listener.GetContextAsync().WaitAsync(timeout.Token);
                var received = context.Request.QueryString;
                bool valid = context.Request.HttpMethod == "GET" && received["state"] == state;
                context.Response.StatusCode = valid ? 200 : 400;
                byte[] message = Encoding.UTF8.GetBytes(valid ? "<!doctype html><meta charset=utf-8><title>CasterDesk</title><p>Volte ao CasterDesk para concluir a conexão.</p>" : "Invalid request");
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.OutputStream.WriteAsync(message, timeout.Token); context.Response.Close();
                if (!valid) continue;
                if (!string.IsNullOrEmpty(received["error"])) throw new InvalidOperationException("O login Google foi cancelado ou não autorizado.");
                if (string.IsNullOrEmpty(received["code"])) throw new InvalidOperationException("O Google não retornou a autorização.");
                await TokenAsync(new() { ["grant_type"] = "authorization_code", ["code"] = received["code"]!, ["code_verifier"] = verifier, ["redirect_uri"] = redirect }, timeout.Token);
                break;
            }
        } finally { gate.Release(); }
    }
    public async Task<string> AccessTokenAsync(CancellationToken cancellation = default)
    {
        await gate.WaitAsync(cancellation);
        try {
            if (!Connected) throw new InvalidOperationException("Conecte uma conta Google na tela Tabela da partida.");
            if (credentials.Expires < DateTimeOffset.UtcNow.AddMinutes(1))
                await TokenAsync(new() { ["grant_type"] = "refresh_token", ["refresh_token"] = credentials.RefreshToken }, cancellation);
            return credentials.AccessToken;
        } finally { gate.Release(); }
    }
    async Task TokenAsync(Dictionary<string, string> values, CancellationToken cancellation)
    {
        values["client_id"] = credentials.ClientId;
        if (credentials.ClientSecret.Length > 0) values["client_secret"] = credentials.ClientSecret;
        using var content = new FormUrlEncodedContent(values);
        using var response = await http.PostAsync("https://oauth2.googleapis.com/token", content, cancellation);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Não foi possível renovar a autorização Google. Reconecte a conta e confira os usuários de teste do projeto.");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation));
        var root = json.RootElement;
        var next = new Credentials { ClientId = credentials.ClientId, ClientSecret = credentials.ClientSecret,
            AccessToken = root.GetProperty("access_token").GetString() ?? "",
            RefreshToken = root.TryGetProperty("refresh_token", out var refresh) ? refresh.GetString() ?? "" : credentials.RefreshToken,
            Expires = DateTimeOffset.UtcNow.AddSeconds(root.GetProperty("expires_in").GetInt32()) };
        if (next.AccessToken.Length == 0 || next.RefreshToken.Length == 0) throw new InvalidOperationException("Autorize acesso offline ao reconectar a conta Google.");
        Save(next); credentials = next;
    }
    public async Task DisconnectAsync(CancellationToken cancellation = default)
    {
        await gate.WaitAsync(cancellation);
        try {
            if (Connected) {
                using var content = new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = credentials.RefreshToken });
                using var response = await http.PostAsync("https://oauth2.googleapis.com/revoke", content, cancellation);
                if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.BadRequest)
                    throw new InvalidOperationException("Não foi possível revogar a conexão. Tente novamente com internet.");
            }
            var next = new Credentials { ClientId = credentials.ClientId, ClientSecret = credentials.ClientSecret }; Save(next); credentials = next;
        } finally { gate.Release(); }
    }
    void Save(Credentials value)
    {
        byte[] bytes = Protect(JsonSerializer.SerializeToUtf8Bytes(value)); Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        string temporary = file + ".tmp-" + Guid.NewGuid().ToString("N");
        try { File.WriteAllBytes(temporary, bytes); File.Move(temporary, file, true); } finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    static string Base64(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    [StructLayout(LayoutKind.Sequential)] struct Blob { public int Length; public IntPtr Data; }
    [DllImport("crypt32.dll", SetLastError = true)] static extern bool CryptProtectData(ref Blob input, string? description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out Blob output);
    [DllImport("crypt32.dll", SetLastError = true)] static extern bool CryptUnprotectData(ref Blob input, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out Blob output);
    [DllImport("kernel32.dll")] static extern IntPtr LocalFree(IntPtr value);
    static byte[] Protect(byte[] bytes) => Crypt(bytes, true);
    static byte[] Unprotect(byte[] bytes) => Crypt(bytes, false);
    static byte[] Crypt(byte[] bytes, bool encrypt)
    {
        var input = new Blob { Length = bytes.Length, Data = Marshal.AllocHGlobal(bytes.Length) }; Blob output = default;
        try {
            Marshal.Copy(bytes, 0, input.Data, bytes.Length);
            bool ok = encrypt ? CryptProtectData(ref input, "CasterDesk Google", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output)
                : CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 1, out output);
            if (!ok) throw new CryptographicException("Não foi possível proteger a autorização Google neste Windows.");
            byte[] result = new byte[output.Length]; Marshal.Copy(output.Data, result, 0, result.Length); return result;
        } finally { Marshal.FreeHGlobal(input.Data); if (output.Data != IntPtr.Zero) LocalFree(output.Data); }
    }
}
