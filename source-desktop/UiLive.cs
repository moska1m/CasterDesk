using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace CasterDesk.Desktop;
internal static partial class Ui
{
    sealed class LabelBinding(Control control, string[] source)
    {
        public readonly WeakReference<Control> Target = new(control);
        public readonly string[] Source = source;
        public string Language = L.Current;
    }
    static readonly List<LabelBinding> labels = [];
    static void Track(Control control, params string[] source) {
        if(source.Length==0 || source.All(string.IsNullOrEmpty))return;
        if(labels.Count>3000)labels.RemoveAll(e=>!e.Target.TryGetTarget(out _));
        labels.Add(new(control,source));
    }
    // Only registered developer-authored labels are translated. Never rewrite user input.
    public static void Relocalize()
    {
        foreach(var item in labels.ToArray()) {
            if(!item.Target.TryGetTarget(out var control))continue;
            string Old(string source) => item.Language=="pt-BR"?source:L.Entries.TryGetValue(source,out var pair)?pair[item.Language=="en"?0:1]:source;
            string before=Old(item.Source[0]),after=L.T(item.Source[0]);
            if(control is TextBlock text) {
                if(text.Text==before)text.Text=after;
                else if(text.Text==before.ToUpperInvariant())text.Text=after.ToUpperInvariant();
            } else if(control is Button button && button.Content is string value && value==before)button.Content=after;
            else if(control is ComboBox combo && combo.Items.Cast<object>().Select(x=>x?.ToString()).SequenceEqual(item.Source.Select(Old))) {
                int index=combo.SelectedIndex; combo.ItemsSource=item.Source.Select(L.T).ToArray(); combo.SelectedIndex=index;
            }
            item.Language=L.Current;
        }
        labels.RemoveAll(e=>!e.Target.TryGetTarget(out _));
    }
    public static void Repaint(string theme, params Control[] roots)
    {
        IBrush oldCanvas=Canvas,oldPanel=Panel,oldHeader=Header,oldMenu=MenuSurface,oldInk=Ink,oldFrame=FrameLine;
        Theme(theme);
        IBrush? Replace(IBrush? b) => b==null?null:b.ToString()==oldCanvas.ToString()?Canvas:b.ToString()==oldPanel.ToString()?Panel:b.ToString()==oldHeader.ToString()?Header:b.ToString()==oldMenu.ToString()?MenuSurface:b.ToString()==oldInk.ToString()?Ink:b.ToString()==oldFrame.ToString()?FrameLine:b;
        var controls=roots.SelectMany(r=>r.GetVisualDescendants().OfType<Control>().Concat(r.GetLogicalDescendants().OfType<Control>()).Append(r)).Distinct().ToArray();
        foreach(var c in controls) {
            if(c is Window window){window.RequestedThemeVariant=Dark?global::Avalonia.Styling.ThemeVariant.Dark:global::Avalonia.Styling.ThemeVariant.Light;window.Foreground=Ink;}
            if(c is global::Avalonia.Controls.Shapes.Path shape && shape.GetVisualAncestors().OfType<Viewbox>().Any(v=>v.Name is "casterdesk-brand" or "casterdesk-frame-symbol"))shape.Fill=Dark?Brushes.White:Brushes.Black;
            if(c is Border border){border.Background=Replace(border.Background);border.BorderBrush=Replace(border.BorderBrush);}
            if(c is Panel panel)panel.Background=Replace(panel.Background);
            if(c is TextBlock text){text.Background=Replace(text.Background); if(text.IsSet(TextBlock.ForegroundProperty))text.Foreground=Replace(text.Foreground);}
            if(c is global::Avalonia.Controls.Primitives.TemplatedControl templated){if(templated.IsSet(TemplatedControl.BackgroundProperty))templated.Background=Replace(templated.Background);if(templated.IsSet(TemplatedControl.ForegroundProperty))templated.Foreground=Replace(templated.Foreground);}
        }
    }
}
