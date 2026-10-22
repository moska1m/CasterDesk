using System.Diagnostics;

namespace CasterDesk.Desktop;

public sealed partial class StudioShell
{
    StackPanel GoogleTablePanel(CancellationToken loginCancellation, out Action detach)
    {
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 8 };
        panel.Children.Add(Ui.Text("TABELA DA PARTIDA · GOOGLE SHEETS", true));
        panel.Children.Add(Ui.Text("Usa as mesmas informações do Excel local. Atualiza somente os campos de entrada da aba escolhida e mantém cópias da partida no PC. Você pode renomear a aba, mantendo a estrutura do modelo."));
        var account = Ui.RawText(googleTable.Auth.Connected ? "Conta Google conectada." : googleTable.Auth.Configured ? "Conecte sua conta Google para começar." : "O login Google não está disponível nesta instalação. Confira as opções avançadas.");
        panel.Children.Add(account);
        var state = Ui.RawText(googleTable.Status); state.TextWrapping = TextWrapping.Wrap;
        void Refresh() => state.Text = googleTable.Status;
        googleTable.Changed += Refresh; detach = () => googleTable.Changed -= Refresh;
        var enabled = new CheckBox { Name = "google-table-enabled", Content = "Sincronizar automaticamente com Google Sheets", IsChecked = googleTable.Enabled };
        var advanced = new StackPanel { Spacing = 8 };
        var import = Ui.Button("Importar cliente Google…", async () => {
            try {
                string? path = await Ui.PickFile(this, "Cliente OAuth do Google", "json"); if (path == null) return;
                await googleTable.ConfigureAsync(false, googleTable.Link);
                await googleTable.Auth.ImportClientAsync(path); enabled.IsChecked = false; account.Text = "Conecte sua conta Google para começar.";
            } catch (Exception ex) { state.Text = ex.Message; }
        });
        Button? connect = null;
        connect = Ui.Button(googleTable.Auth.Connected ? "Trocar conta Google" : "Conectar com Google", async () => {
            connect!.IsEnabled = false;
            try {
                await googleTable.ConfigureAsync(false, googleTable.Link); enabled.IsChecked = false;
                state.Text = "Conclua a autorização no seu navegador padrão. O CasterDesk não pede sua senha.";
                await googleTable.Auth.LoginAsync(url => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }), loginCancellation);
                connect.Content = "Trocar conta Google"; account.Text = "Conta Google conectada."; state.Text = "Cole o link da planilha de destino e aplique a configuração.";
            } catch (OperationCanceledException) { state.Text = "Login cancelado ou tempo esgotado. Tente conectar novamente."; }
            catch (Exception ex) { state.Text = ex.Message; }
            finally { connect.IsEnabled = true; }
        }, true);
        connect.Name = "google-connect"; panel.Children.Add(connect);
        panel.Children.Add(Ui.Text("Planilha que deve receber os dados"));
        var link = new TextBox { Name = "google-table-link", Text = googleTable.Link, PlaceholderText = "https://docs.google.com/spreadsheets/d/…/edit#gid=…" }; panel.Children.Add(link);
        panel.Children.Add(Ui.Text("Aba que deve receber os dados"));
        panel.Children.Add(new GoogleSheetPicker(link,googleTable.ListSheetsAsync,message=>state.Text=message,loginCancellation));
        panel.Children.Add(enabled);
        panel.Children.Add(Ui.Text("A conta conectada precisa poder editar a planilha. No primeiro teste, use uma cópia do modelo. A conexão não altera as células vermelhas, fórmulas ou formatação."));
        var actions = new UniformGrid { Columns = 2 };
        actions.Children.Add(Ui.Button("Verificar e aplicar", async () => {
            try { await googleTable.ConfigureAsync(enabled.IsChecked == true, link.Text ?? ""); }
            catch (Exception ex) { state.Text = ex.Message; }
        }, true));
        actions.Children.Add(Ui.Button("Atualizar agora", async () => {
            try {
                if (enabled.IsChecked != true) { state.Text = "Marque a sincronização automática para ativar a conexão antes de atualizar."; return; }
                await googleTable.ConfigureAsync(true, link.Text ?? ""); await googleTable.UpdateAsync();
            } catch (Exception ex) { state.Text = ex.Message; }
        })); panel.Children.Add(actions);
        panel.Children.Add(Ui.Button("Desconectar conta", async () => {
            try {
                await googleTable.ConfigureAsync(false, link.Text ?? ""); enabled.IsChecked = false;
                await googleTable.Auth.DisconnectAsync(loginCancellation); account.Text = "Conta desconectada."; connect.Content = "Conectar com Google";
            } catch (Exception ex) { state.Text = ex.Message; }
        }));
        advanced.Children.Add(Ui.Text("Para desenvolvimento ou uso de um projeto Google próprio.")); advanced.Children.Add(import);
        panel.Children.Add(new Expander { Name = "google-advanced", Header = "Opções avançadas", Content = advanced, IsExpanded = false, HorizontalAlignment = HorizontalAlignment.Stretch });
        panel.Children.Add(Ui.Text("Sem internet, o registro da partida continua funcionando. A sincronização tenta novamente quando a conexão voltar. A autorização Google fica protegida pelo Windows e não acompanha os backups."));
        panel.Children.Add(state);
        return panel;
    }
}
