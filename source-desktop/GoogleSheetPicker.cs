namespace CasterDesk.Desktop;

// Reading the list never applies a destination or writes spreadsheet cells.
internal sealed class GoogleSheetPicker : Grid
{
    readonly TextBox link;
    readonly Func<string,CancellationToken,Task<IReadOnlyList<SheetsTarget>>> load;
    readonly Action<string> status;
    readonly CancellationToken cancellation;
    readonly ComboBox choice=Ui.RawCombo();
    readonly Button refresh;
    bool selecting,busy;
    internal GoogleSheetPicker(TextBox link,Func<string,CancellationToken,Task<IReadOnlyList<SheetsTarget>>> load,Action<string> status,CancellationToken cancellation)
    {
        this.link=link;this.load=load;this.status=status;this.cancellation=cancellation;
        ColumnDefinitions=new("*,Auto");
        choice.Name="google-table-sheet";choice.PlaceholderText="Carregue as abas e escolha o destino";choice.Margin=new Thickness(0,0,8,0);choice.SelectedIndex=-1;
        refresh=Ui.Button("Carregar abas",()=>_=LoadAsync());refresh.Name="google-load-sheets";refresh.Margin=new Thickness(0);
        Children.Add(choice);Children.Add(refresh);SetColumn(refresh,1);
        link.TextChanged+=(_,_)=>{
            if(selecting||choice.SelectedItem is SheetsTarget current&&(link.Text??"").Trim()==GoogleSheetsTable.TargetLink(current))return;
            choice.ItemsSource=Array.Empty<SheetsTarget>();choice.SelectedIndex=-1;
        };
        choice.SelectionChanged+=(_,_)=>{
            if(choice.SelectedItem is not SheetsTarget target)return;
            selecting=true;try{link.Text=GoogleSheetsTable.TargetLink(target);}finally{selecting=false;}
            status("Aba selecionada: "+target.Title+". Clique em Verificar e aplicar para salvar o destino.");
        };
    }
    internal async Task LoadAsync()
    {
        if(busy)return;
        busy=true;refresh.IsEnabled=choice.IsEnabled=false;
        string requested=(link.Text??"").Trim();status("Carregando abas…");
        try {
            var tabs=await load(requested,cancellation);
            if(cancellation.IsCancellationRequested)return;
            if((link.Text??"").Trim()!=requested){status("O link mudou. Carregue as abas da nova planilha.");return;}
            choice.ItemsSource=tabs;choice.SelectedIndex=-1;
            var (_,gid)=GoogleSheetsTable.ParseLink(requested);
            var selected=gid.HasValue?tabs.FirstOrDefault(t=>t.SheetId==gid):tabs.FirstOrDefault(t=>t.Title==GoogleSheetsTable.SheetTitle)??(tabs.Count==1?tabs[0]:null);
            if(selected!=null)choice.SelectedItem=selected;
            else status(tabs.Count==0?"Nenhuma aba de células foi encontrada nesta planilha.":"Escolha a aba que deve receber os dados. O nome pode ser diferente do modelo.");
        } catch(OperationCanceledException) when(cancellation.IsCancellationRequested) { }
        catch(Exception ex){status(ex.Message);}
        finally{busy=false;refresh.IsEnabled=choice.IsEnabled=true;}
    }
}
