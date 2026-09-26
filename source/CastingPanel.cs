using System.Text;
using System.Text.Json;

namespace CronoLive;

public sealed class Talent
{
    public string Name { get; set; } = "";
    public string Handle { get; set; } = "";
}

public sealed class CastingSettings
{
    public string Folder { get; set; } = Path.Combine(AppPaths.DataDirectory, "Casting");
    public Talent[] People { get; set; } = [new(), new(), new(), new()];
}

public static class CastingFiles
{
    public static readonly string[] Keys = ["narrador", "comentarista", "convidado1", "convidado2"];
    public static string Handle(string value) => string.IsNullOrWhiteSpace(value.Trim().TrimStart('@')) ? "" : "@" + value.Trim().TrimStart('@');
    public static void Write(string folder, Talent[] people)
    {
        Directory.CreateDirectory(folder);
        for (int i = 0; i < Keys.Length; i++)
        {
            File.WriteAllText(Path.Combine(folder, Keys[i] + ".txt"), people[i].Name.Trim(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(folder, "@" + Keys[i] + ".txt"), Handle(people[i].Handle), new UTF8Encoding(false));
        }
    }
}

public sealed class CastingPanel : UserControl
{
    readonly string config = Path.Combine(AppPaths.DataDirectory, "casting-settings.json");
    CastingSettings settings = new();
    readonly TextBox[] names = new TextBox[4], handles = new TextBox[4];
    readonly TextBox folder = new() { ReadOnly = true, Dock = DockStyle.Fill };
    readonly Label status = new() { AutoSize = true, Text = L.T("Preencha os campos e clique em Aplicar no OBS.") };

    public CastingPanel()
    {
        Dock = DockStyle.Fill;
        try { settings = JsonSerializer.Deserialize<CastingSettings>(File.ReadAllText(config)) ?? new(); } catch { }
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 125)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 125));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(layout);
        string[] titles = [L.T("Narrador / Caster"), L.T("Comentarista"), L.T("Convidado 1"), L.T("Convidado 2")];
        for (int i = 0; i < 4; i++)
        {
            var person = settings.People?.ElementAtOrDefault(i) ?? new Talent();
            var group = new ThemeGroup { Text = titles[i], Dock = DockStyle.Fill };
            var fields = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8, 5, 8, 4), ColumnCount = 2, RowCount = 3 };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68)); fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 30)); fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            names[i] = new TextBox { Text = person.Name, Dock = DockStyle.Fill };
            handles[i] = new TextBox { Text = person.Handle, Dock = DockStyle.Fill };
            fields.Controls.Add(new ThemeCaption { Text = L.T("Nome:"), Dock = DockStyle.Fill }, 0, 0);
            fields.Controls.Add(new ThemeCaption { Text = "@", Dock = DockStyle.Fill }, 0, 1);
            fields.Controls.Add(names[i], 1, 0); fields.Controls.Add(handles[i], 1, 1);
            var hint = new Label { Text = CastingFiles.Keys[i] + ".txt  /  @" + CastingFiles.Keys[i] + ".txt", AutoSize = true, ForeColor = SystemColors.GrayText };
            fields.Controls.Add(hint, 0, 2); fields.SetColumnSpan(hint, 2);
            names[i].TextChanged += (_, _) => Pending(); handles[i].TextChanged += (_, _) => Pending();
            group.Controls.Add(fields); layout.Controls.Add(group, i % 2, i / 2);
        }
        var output = new ThemeGroup { Text = L.T("Pasta dos oito arquivos TXT"), Dock = DockStyle.Fill };
        var pathRow = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8), ColumnCount = 2 };
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        folder.Text = settings.Folder;
        pathRow.Controls.Add(folder, 0, 0); pathRow.Controls.Add(Btn(L.T("Escolher pasta"), ChooseFolder), 1, 0);
        output.Controls.Add(pathRow); layout.Controls.Add(output, 0, 2); layout.SetColumnSpan(output, 2);
        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        actions.Controls.Add(status, 0, 0); actions.Controls.Add(Btn(L.T("Aplicar no OBS"), Apply), 1, 0);
        layout.Controls.Add(actions, 0, 3); layout.SetColumnSpan(actions, 2);
    }
    static Button Btn(string title, Action action)
    {
        var button = new ThemeButton { Text = title, Dock = DockStyle.Top, Height = 25, UseVisualStyleBackColor = true };
        button.Click += (_, _) => action(); return button;
    }
    void Pending() { status.Text = L.T("Alterações pendentes. Clique em Aplicar no OBS."); status.ForeColor = SystemColors.ControlText; }
    void ChooseFolder()
    {
        using var dialog = new FolderBrowserDialog { Description = L.T("Escolha a pasta dos TXT do casting"), UseDescriptionForTitle = true };
        if (dialog.ShowDialog(this) == DialogResult.OK) { folder.Text = dialog.SelectedPath; Pending(); }
    }
    void CaptureFields()
    {
        settings.Folder = folder.Text;
        settings.People = Enumerable.Range(0, 4).Select(i => new Talent { Name = names[i].Text, Handle = handles[i].Text }).ToArray();
    }
    public void SaveDraft()
    {
        CaptureFields();
        try { File.WriteAllText(config, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true })); }
        catch (Exception ex) { MessageBox.Show(this, L.T("Não foi possível salvar o casting: ") + ex.Message); }
    }
    void Apply()
    {
        CaptureFields();
        try
        {
            CastingFiles.Write(settings.Folder, settings.People);
            SaveDraft(); status.Text = L.T("8 TXT atualizados às ") + DateTime.Now.ToString("HH:mm:ss"); status.ForeColor = Color.DarkGreen;
        }
        catch (Exception ex) { status.Text = L.T("Falha ao atualizar. Alguns TXT podem ter sido gravados."); status.ForeColor = Color.Firebrick; MessageBox.Show(this, ex.Message, L.T("Saída do casting")); }
    }
}


