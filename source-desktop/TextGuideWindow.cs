namespace CasterDesk.Desktop;

internal sealed class TextGuideWindow : Window
{
    public TextGuideWindow(string file)
    {
        Title="Pacote visual · guia para o designer";Width=780;Height=660;Background=Ui.Canvas;
        var content=new Grid{Margin=new Thickness(16),RowDefinitions=new("*,Auto")};
        content.Children.Add(new TextBox{Text=File.ReadAllText(file),IsReadOnly=true,AcceptsReturn=true,TextWrapping=TextWrapping.Wrap});
        var close=Ui.Button("Fechar",()=>Close());content.Children.Add(close);Grid.SetRow(close,1);Content=content;
        WindowStartupLocation=WindowStartupLocation.CenterOwner;
    }
}
