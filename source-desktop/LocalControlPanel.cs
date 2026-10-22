using Avalonia.Media.Imaging;
using System.Diagnostics;
using System.Text.Json;
using QRCoder;

namespace CasterDesk.Desktop;

public sealed partial class StudioShell
{
    Control CreateLocalControlPanel(out Action cleanup)
    {
        var panel=new StackPanel{Spacing=8,Margin=new Thickness(0,20,0,0),Name="local-control-panel"};
        panel.Children.Add(Ui.Text("CONTROLE PELO CELULAR",true));
        panel.Children.Add(Ui.Text("Ative o acesso e leia o QR no celular conectado à mesma rede Wi-Fi do PC. Mantenha o CasterDesk aberto. Depois feche as opções para liberar os controles."));
        var state=Ui.RawText("");state.Name="local-control-status";panel.Children.Add(state);
        var addresses=new ComboBox{Name="local-control-address",HorizontalAlignment=HorizontalAlignment.Stretch};panel.Children.Add(addresses);
        var url=new TextBox{Name="local-control-url",IsReadOnly=true,TextWrapping=TextWrapping.Wrap,PlaceholderText=L.T("Ative o controle local para gerar o link.")};panel.Children.Add(url);
        var qr=new Image{Name="local-control-qr",Width=220,Height=220,HorizontalAlignment=HorizontalAlignment.Left,Stretch=Stretch.Uniform};panel.Children.Add(qr);
        Bitmap? bitmap=null;
        void ShowLink(){bitmap?.Dispose();bitmap=null;var link=addresses.SelectedItem as string??"";url.Text=link;qr.IsVisible=link.Length>0;if(link.Length>0){using var data=QRCodeGenerator.GenerateQrCode(link,QRCodeGenerator.ECCLevel.M);using var png=new PngByteQRCode(data);using var stream=new MemoryStream(png.GetGraphic(5));bitmap=new Bitmap(stream);qr.Source=bitmap;}else qr.Source=null;}
        void RefreshLinks(){addresses.ItemsSource=localControl.Links;addresses.SelectedIndex=localControl.Links.Length>0?0:-1;addresses.IsVisible=localControl.Links.Length>1;ShowLink();}
        addresses.SelectionChanged+=(_,_)=>ShowLink();
        var enable=Ui.Button("Ativar controle local",async()=>{try{Championship.FlushNames();await localControl.StartAsync();localSettings.Enabled=true;MatchFiles.AtomicText(AppPaths.ConfigFile("local-control.json"),JsonSerializer.Serialize(localSettings));RefreshLinks();}catch(Exception ex){AppLog.Write("Local control: "+ex.GetType().Name);state.Text=L.T("Não foi possível ativar. Confira se a porta 43828 está disponível.");}});enable.Name="local-control-enable";panel.Children.Add(enable);
        panel.Children.Add(Ui.Button("Copiar link para o celular",()=>{if(!string.IsNullOrEmpty(url.Text))Ui.CopyText(url.Text);}));
        panel.Children.Add(Ui.Button("Gerar novo link e desconectar dispositivos",()=>{if(localControl.Running){localControl.Revoke();RefreshLinks();}}));
        panel.Children.Add(Ui.Button("Desativar controle local",async()=>{await localControl.DisposeAsync();localSettings.Enabled=false;MatchFiles.AtomicText(AppPaths.ConfigFile("local-control.json"),JsonSerializer.Serialize(localSettings));RefreshLinks();}));
        panel.Children.Add(Ui.Text("Se o celular não abrir o link, prepare a rede abaixo e confira se o Windows está em uma rede privada. Redes de convidados podem bloquear a comunicação entre os dispositivos."));
        panel.Children.Add(Ui.Button("Preparar acesso na rede privada",async()=>{
            if(Program.Headless)return;
            try{
                var start=new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell/v1.0/powershell.exe")){UseShellExecute=true,Verb="runas",WindowStyle=ProcessWindowStyle.Hidden};
                start.Arguments="-NoProfile -ExecutionPolicy Bypass -File \""+Path.Combine(AppContext.BaseDirectory,"assets/control/Configure-Network.ps1")+"\" -Executable \""+Path.Combine(AppContext.BaseDirectory,"CasterDesk.exe")+"\" -Port "+localSettings.Port;
                using var process=Process.Start(start);if(process!=null){await process.WaitForExitAsync();state.Text=L.T(process.ExitCode==0?"Rede preparada. Abra o link novamente no celular.":"Não foi possível preparar a rede. Confira a permissão do Windows.");}
            }catch(System.ComponentModel.Win32Exception){state.Text=L.T("A preparação da rede não foi autorizada.");}
        }));
        var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(2)};
        timer.Tick+=(_,_)=>{enable.IsEnabled=!localControl.Running;state.Text=L.T(!localControl.Running?"Controle local desativado":localControl.Links.Length==0?"Nenhuma rede local encontrada. Conecte o PC à rede e reative o controle.":localControl.HasClient?"Celular conectado":"Aguardando conexão do celular");};timer.Start();
        RefreshLinks();enable.IsEnabled=!localControl.Running;state.Text=L.T(localControl.Running?"Aguardando conexão do celular":"Controle local desativado");
        cleanup=()=>{timer.Stop();qr.Source=null;bitmap?.Dispose();};return panel;
    }
}
