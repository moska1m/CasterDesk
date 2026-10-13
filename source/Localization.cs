using System.Globalization;
using System.Text.Json;
namespace CronoLive;
// Only developer-authored labels/templates are translated, never interpolated user data.
// Language changes apply on restart, keeping UI and background exports consistent.
public static class L
{
    public static readonly string[] Languages = ["pt-BR", "en", "es"];
    public static string Current { get; private set; } = "pt-BR";
    static readonly Dictionary<string,string[]> Texts = Load();
    static Dictionary<string,string[]> Load()
    {
        using var stream=typeof(L).Assembly.GetManifestResourceStream("CasterDesk.translations.json")!;
        return JsonSerializer.Deserialize<Dictionary<string,string[]>>(stream)!;
    }
    public static void Set(string? language) => Current=Languages.Contains(language)?language!:"pt-BR";
    public static void LoadPreference()
    {
        try { using var doc=JsonDocument.Parse(File.ReadAllText(AppPaths.ConfigFile("settings.json")));Set(doc.RootElement.TryGetProperty("Language",out var lang)?lang.GetString():null); }
        catch { Set(null); }
    }
    public static string T(string text) => Current=="pt-BR"?text:Texts.TryGetValue(text,out var translated)?translated[Current=="en"?0:1]:text;
    public static string F(FormattableString text)=>string.Format(CultureInfo.InvariantCulture,T(text.Format),text.GetArguments());
    public static string Role(string role)=>role=="ATQ"?Current switch {"en"=>"ATK","es"=>"ATQ",_=>"ATQ"}:role;
    public static string FormatName(string format)=>Current=="en"?format.Replace("MD","BO"):format;
    public static string FileStatus(bool complete)=>Current switch {"en"=>complete?"completed":"open","es"=>complete?"finalizada":"abierta",_=>complete?"concluida":"aberta"};
    public static IReadOnlyDictionary<string,string[]> Entries=>Texts;
}
