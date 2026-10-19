namespace CasterDesk.Desktop;
internal static class BroadcastChannel
{
    internal static string Normalize(string value) {
        value = value.Trim();
        if (value is "" or "twitch.tv/" or "https://twitch.tv/") return "";
        if (value.Contains('/') || value.Contains('.')) return value;
        return "twitch.tv/" + value.TrimStart('@');
    }
}
