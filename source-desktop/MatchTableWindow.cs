using System.Diagnostics;

namespace CasterDesk.Desktop;

public sealed partial class StudioShell
{
    async Task OpenMatchTable()
    {
        Championship.FlushNames();
        if (menuOpen) ToggleMenu();
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 8 };
        panel.Children.Add(Ui.Text("TABELA DA PARTIDA · EXCEL", true));
        panel.Children.Add(Ui.Text("Atualiza a tabela com os times, a série e os rounds do mapa em andamento. Compatível com MD1 e MD3."));
        var enabled = new CheckBox { Content = "Atualizar automaticamente", IsChecked = matchTable.Enabled };
        panel.Children.Add(enabled);
        panel.Children.Add(Ui.Text("Arquivo fixo para usar na transmissão"));
        var path = new TextBox { Text = matchTable.OutputPath, Name = "match-table-path" }; panel.Children.Add(path);
        var state = Ui.RawText(matchTable.Status); state.TextWrapping = TextWrapping.Wrap;
        void Status() => state.Text = matchTable.Status;
        matchTable.Changed += Status;
        panel.Children.Add(Ui.Button("Escolher arquivo…", async () => {
            string? picked = await Ui.PickFile(this, "Arquivo da tabela", "xlsx", true, "partida-atual.xlsx");
            if (picked != null) path.Text = picked;
        }));
        panel.Children.Add(Ui.Text("As cópias da partida e de cada mapa ficam na pasta Tabelas dos arquivos da partida. As células vermelhas do modelo continuam sendo preenchidas manualmente."));
        var actions = new UniformGrid { Columns = 2 };
        var apply = Ui.Button("Aplicar", async () => {
            try { await matchTable.ConfigureAsync(enabled.IsChecked == true, path.Text ?? ""); await matchTable.FlushAsync(); }
            catch (Exception ex) { state.Text = ex.Message; }
        }, true);
        var update = Ui.Button("Atualizar agora", async () => {
            try {
                await matchTable.ConfigureAsync(enabled.IsChecked == true, path.Text ?? "");
                await matchTable.UpdateAsync();
            } catch (Exception ex) { state.Text = ex.Message; }
        });
        actions.Children.Add(apply); actions.Children.Add(update); panel.Children.Add(actions);
        panel.Children.Add(Ui.Button("Copiar caminho do arquivo", () => Ui.CopyText(matchTable.OutputPath)));
        panel.Children.Add(Ui.Button("Abrir cópias da partida", () => {
            try {
                string folder = Path.GetDirectoryName(MatchTable.ArchivePath(Championship.Session.Current))!;
                Directory.CreateDirectory(folder); Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
            } catch (Exception ex) { state.Text = ex.Message; }
        }));
        panel.Children.Add(state);
        using var loginCancellation = new CancellationTokenSource();
        var google = GoogleTablePanel(loginCancellation.Token, out var detachGoogle);
        var tabs = new TabControl { ItemsSource = new[] {
            new TabItem { Header = "EXCEL LOCAL", Content = new ScrollViewer { Content = panel } },
            new TabItem { Header = "GOOGLE SHEETS", Content = new ScrollViewer { Content = google } }
        }};
        var root = new Grid { RowDefinitions = new("*,Auto") }; root.Children.Add(tabs);
        var dialog = Ui.Dialog(this, "Tabela da partida · experimental", root, 740, 740);
        var close = Ui.Button("Fechar", () => dialog.Close()); root.Children.Add(close); Grid.SetRow(close, 1);
        try { await dialog.ShowDialog(this); } finally { loginCancellation.Cancel(); matchTable.Changed -= Status; detachGoogle(); }
    }
}
