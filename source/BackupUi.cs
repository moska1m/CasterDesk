using System.Text.Json;
namespace CronoLive;
public static class BackupUi
{
    public static async void Create(Form owner,Action flush)
    {
        using var dialog=new SaveFileDialog{Filter=L.T("Backup CasterDesk")+"|*.zip",InitialDirectory=AppPaths.BackupDirectory,FileName=$"CasterDesk_Backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip"};if(dialog.ShowDialog(owner)!=DialogResult.OK)return;
        try{flush();owner.UseWaitCursor=true;owner.Enabled=false;await Task.Run(()=>DataBackup.Create(AppPaths.DataDirectory,dialog.FileName));owner.Enabled=true;MessageBox.Show(owner,L.T("Backup criado. Arquivos salvos fora da pasta do CasterDesk não estão incluídos. Guarde uma cópia em outro disco."),L.T("Backup concluído"));}
        catch(Exception ex){owner.Enabled=true;MessageBox.Show(owner,L.T("Não foi possível criar o backup:\n")+ex.Message,"CasterDesk");}finally{owner.Enabled=true;owner.UseWaitCursor=false;}
    }
    public static async void Restore(Form owner)
    {
        using var dialog=new OpenFileDialog{Filter=L.T("Backup CasterDesk")+"|*.zip",InitialDirectory=AppPaths.BackupDirectory};if(dialog.ShowDialog(owner)!=DialogResult.OK)return;
        try
        {
            owner.UseWaitCursor=true;owner.Enabled=false;
            string stage=Path.Combine(AppPaths.BackupDirectory,"verificacao-"+Guid.NewGuid().ToString("N"));
            var manifest=await Task.Run(()=>DataBackup.Extract(dialog.FileName,stage));int conflicts=await Task.Run(()=>DataBackup.Conflicts(AppPaths.DataDirectory,manifest));owner.Enabled=true;owner.UseWaitCursor=false;
            bool replace=false;
            if(conflicts>0)
            {
                var choice=MessageBox.Show(owner,L.F($"Backup verificado: {manifest.Files.Count} arquivos.\n{conflicts} arquivos existentes têm conteúdo diferente (incluindo partidas e configurações).\n\nSim: usar as versões do backup.\nNão: manter as versões atuais e adicionar apenas arquivos ausentes.\nCancelar: não restaurar.\n\nUma cópia completa dos dados atuais será criada antes da restauração."),L.T("Conflitos no backup"),MessageBoxButtons.YesNoCancel);
                if(choice==DialogResult.Cancel)return;replace=choice==DialogResult.Yes;
            }
            if(MessageBox.Show(owner,L.F($"Restaurar {manifest.Files.Count} arquivos? O CasterDesk será reiniciado. Uma cópia de segurança será guardada em Backups.\n\nConfira depois os caminhos externos usados no OBS."),L.T("Restaurar backup"),MessageBoxButtons.OKCancel)!=DialogResult.OK)return;
            // Keep the selected archive available even when it lives on removable media.
            string copy=Path.Combine(AppPaths.BackupDirectory,"restaurar-"+Guid.NewGuid().ToString("N")+".zip");File.Copy(dialog.FileName,copy);
            File.WriteAllText(Path.Combine(AppPaths.DataDirectory,"pending-restore.json"),JsonSerializer.Serialize(new{Archive=copy,Replace=replace}));
            Application.Restart();
        }
        catch(Exception ex){owner.Enabled=true;MessageBox.Show(owner,L.T("Não foi possível preparar a restauração:\n")+ex.Message,"CasterDesk");}finally{owner.Enabled=true;owner.UseWaitCursor=false;}
    }
}
