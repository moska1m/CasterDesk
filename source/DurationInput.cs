using System.Globalization;

namespace CronoLive;

// A duration, rather than a time of day: supports 24:00:00.
public sealed class DurationInput : NumericUpDown
{
    public DurationInput()
    {
        Minimum = 0; Maximum = 86400; Value = 600;
        TextAlign = HorizontalAlignment.Center;
        AccessibleName = L.T("Tempo inicial em horas, minutos e segundos");
    }
    protected override void UpdateEditText() => Text = MainForm.Format((int)Value, "hh:mm:ss");
    protected override void OnTextBoxKeyPress(object? source, KeyPressEventArgs e)
    {
        if (e.KeyChar == ':') return;
        base.OnTextBoxKeyPress(source, e);
    }
    public static bool TrySeconds(string text, out int seconds)
    {
        seconds = 0;
        var parts = text.Split(':');
        if (parts.Length != 3 || parts.Any(p => p.Length != 2 || !p.All(char.IsAsciiDigit))) return false;
        int h = int.Parse(parts[0], CultureInfo.InvariantCulture);
        int m = int.Parse(parts[1], CultureInfo.InvariantCulture);
        int s = int.Parse(parts[2], CultureInfo.InvariantCulture);
        seconds = h * 3600 + m * 60 + s;
        return m < 60 && s < 60 && seconds <= 86400;
    }
    protected override void ValidateEditText()
    {
        if (TrySeconds(Text, out int seconds)) Value = seconds;
        UserEdit = false;
        UpdateEditText();
    }
    public override void UpButton() => Step(1);
    public override void DownButton() => Step(-1);
    void Step(int direction)
    {
        var editor = Controls.OfType<TextBox>().FirstOrDefault();
        int position = editor?.SelectionStart ?? 6;
        ValidateEditText();
        int unit = position < 3 ? 3600 : position < 6 ? 60 : 1;
        Value = Math.Clamp(Value + direction * unit, Minimum, Maximum);
        editor?.Select(position < 3 ? 0 : position < 6 ? 3 : 6, 2);
    }
}
