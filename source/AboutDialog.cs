using System.Reflection;
namespace CronoLive;

public sealed class AboutDialog : Form
{
    public AboutDialog()
    {
        Text="Sobre o CasterDesk"; StartPosition=FormStartPosition.CenterParent;
        ClientSize=new Size(650,500); MinimumSize=new Size(530,430);
        Font=new Font("Segoe UI",10); AutoScaleMode=AutoScaleMode.Dpi;
        MinimizeBox=false; MaximizeBox=false; ShowInTaskbar=false;
        var version=typeof(AboutDialog).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "beta";
        var text=new TextBox {Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Dock=DockStyle.Fill,BorderStyle=BorderStyle.None,TabStop=true,AccessibleName="Créditos e avisos",Text=
            $"CASTERDESK • {version}\r\n\r\n"+
            "Criado por Jeferson “Moska” Sampaio.\r\nFerramentas para community casters • uso interno.\r\n\r\n"+
            "RECURSOS VISUAIS E MARCAS\r\n"+
            "Os ícones de operadores, imagens de mapas e logos de Rainbow Six Siege utilizados neste aplicativo pertencem à Ubisoft e/ou aos respectivos titulares. Rainbow Six, Ubisoft e suas marcas relacionadas pertencem aos respectivos titulares. Logos de campeonatos, equipes e plataformas pertencem aos seus proprietários.\r\n\r\n"+
            "O CasterDesk é uma ferramenta independente. Não representa aprovação, patrocínio ou certificação pela Ubisoft ou pela Microsoft. A autorização de transmissão dos usuários não equivale, por si só, à autorização de redistribuição dos recursos do aplicativo.\r\n\r\n"+
            "USO E RESPONSABILIDADE\r\n"+
            "Utilize a ferramenta e as imagens geradas conforme as autorizações aplicáveis, as regras dos campeonatos e os direitos de terceiros. O usuário é responsável pelos dados e materiais que insere e pelo uso que faz dos resultados, nos limites da legislação aplicável. Este aviso não concede direitos sobre recursos de terceiros nem afasta responsabilidades previstas em lei.\r\n\r\n"+
            "LICENÇAS\r\nO código próprio possui licença MIT. Recursos visuais de terceiros não estão incluídos nessa licença. O acesso ao repositório e às versões permanece privado. Consulte os arquivos LICENSE e THIRD-PARTY-NOTICES.md que acompanham o instalador."};
        var body=new Panel {Dock=DockStyle.Fill,Padding=new Padding(22)};body.Controls.Add(text);Controls.Add(body);
        var footer=new Panel {Dock=DockStyle.Bottom,Height=55,Padding=new Padding(12)};
        var close=new ThemeButton {Text="Fechar",Dock=DockStyle.Right,Width=110,DialogResult=DialogResult.OK};footer.Controls.Add(close);Controls.Add(footer);
        AcceptButton=close;CancelButton=close;AppTheme.Apply(this);
        Shown+=(_,_)=>{text.SelectionStart=0;text.SelectionLength=0;close.Focus();};
    }
}
