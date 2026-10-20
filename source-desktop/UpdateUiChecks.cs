using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace CasterDesk.Desktop;

internal static class UpdateUiChecks
{
    sealed class Handler(byte[] feed):HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)=>Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new ByteArrayContent(feed)});
    }
    public static void Run()
    {
        void Need(bool value,string text){if(!value)throw new Exception("Update UI: "+text);}
        void Wait(Task task){var time=System.Diagnostics.Stopwatch.StartNew();while(!task.IsCompleted){Dispatcher.UIThread.RunJobs();if(time.Elapsed.TotalSeconds>20)throw new TimeoutException();Thread.Sleep(5);}task.GetAwaiter().GetResult();Dispatcher.UIThread.RunJobs();}
        using var key=RSA.Create(2048);
        var manifest=new UpdateManifest(1,"release","9.0.0","important","Exemplo de aviso: correção importante disponível.",DateTimeOffset.UtcNow,100,new string('a',64));
        var beta=manifest with{Type="beta",Version="9.1.0-beta.1"};
        byte[] bytes=JsonSerializer.SerializeToUtf8Bytes(new UpdateCatalog(1,[beta,manifest,manifest with{Version="1.0.0"}]));
        byte[] feed=JsonSerializer.SerializeToUtf8Bytes(new{Payload=Convert.ToBase64String(bytes),Signature=Convert.ToBase64String(key.SignData(bytes,HashAlgorithmName.SHA256,RSASignaturePadding.Pss))});
        using var http=new HttpClient(new Handler(feed));using var controller=new UpdateController(new UpdateClient(http,key.ExportSubjectPublicKeyInfoPem()));
        Need(!controller.Preferences.IncludeBetas,"betas disabled by default");
        controller.Configure(true,false);var shell=new StudioShell(updater:controller);shell.Show();Dispatcher.UIThread.RunJobs();
        Wait(controller.CheckAsync(true));
        Need(controller.Versions.Length==2&&controller.Available?.Version=="9.0.0","only releases shown and notified by default");
        Need(shell.GetVisualDescendants().OfType<Border>().Single(b=>b.Name=="update-banner").IsVisible,"important update banner visible");
        Need(shell.IsVisible,"check does not close running application");
        controller.Configure(false,false);Wait(shell.ShowStartupUpdate());Need(!(shell.ActiveSurface?.Name=="startup-update-notice"),"startup notice respects automatic-check preference");controller.Configure(true,false);
        var noticeTask=shell.ShowStartupUpdate();Dispatcher.UIThread.RunJobs();var notice=shell.ActiveSurface!;
        Need(notice.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.Name=="startup-update-notes").Text==manifest.Notes,"startup notice uses the verified release notes");
        using(var bitmap=new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(640,540))){notice.Measure(new Size(640,540));notice.Arrange(new Rect(0,0,640,540));bitmap.Render(notice);bitmap.Save(Path.Combine(AppPaths.DataDirectory,"update-startup-preview.png"));}
        notice.GetVisualDescendants().OfType<Button>().Single(b=>b.Name=="startup-update-later").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Wait(noticeTask);
        Wait(shell.ShowStartupUpdate());Need(!(shell.ActiveSurface?.Name=="startup-update-notice"),"startup notice shown only once per session");
        var dialogTask=shell.OpenUpdates(false);Dispatcher.UIThread.RunJobs();var dialog=shell.ActiveSurface!;
        Need(dialog.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text?.Contains("Atualização importante") == true),"important label");
        Need(dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Name=="download-update").IsVisible,"download action available");
        Need(!dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Name=="install-update").IsVisible,"installation unavailable without verified download");
        var list=dialog.GetVisualDescendants().OfType<ListBox>().Single(b=>b.Name=="update-versions");
        Need(list.ItemCount==2,"version list rendered");
        var toggle=dialog.GetVisualDescendants().OfType<CheckBox>().Single(b=>b.Name=="show-betas");toggle.IsChecked=true;Dispatcher.UIThread.RunJobs();
        Need(list.ItemCount==3&&controller.Available?.Version==beta.Version,"beta filter updates list and available update");
        controller.Select("1.0.0");Dispatcher.UIThread.RunJobs();
        Need(!dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Name=="download-update").IsVisible,"older versions never offered for installation");
        controller.Select(beta.Version);toggle.IsChecked=false;Dispatcher.UIThread.RunJobs();
        Need(controller.Selected?.Version==manifest.Version&&list.ItemCount==2,"hiding betas clears beta selection");
        toggle.IsChecked=true;controller.Select(beta.Version);Dispatcher.UIThread.RunJobs();
        using(var bitmap=new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(720,850))){bitmap.Render(dialog);bitmap.Save(Path.Combine(AppPaths.DataDirectory,"updater-preview.png"));}
        var later=dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Content?.ToString()=="Continuar usando / depois");later.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Wait(dialogTask);
        Need(shell.IsVisible,"postpone keeps app open");
        controller.Configure(false,true);
        var stored=ConfigurationStore.Read<UpdatePreferences>("updates.json");Need(!stored.Automatic&&stored.IncludeBetas,"preferences persisted");
        controller.Configure(true,false);shell.Close();Dispatcher.UIThread.RunJobs();
        File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"casterdesk-updates-check.txt"),"PASS: release/beta list and filter, important banner, manual check, postpone, downgrade prevention, verified-download gate and preferences. No installer or live network used.");
    }
}
