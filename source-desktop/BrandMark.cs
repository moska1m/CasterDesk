using System.Globalization;
using System.Xml.Linq;

namespace CasterDesk.Desktop;

// The approved kit contains outlined paths only. Render those paths directly,
// keeping the SVG geometry and proportions without installing a font.
internal static class BrandMark
{
    public static Control Create(bool white, double width = 240, bool symbol = false)
    {
        string file = Path.Combine(AppContext.BaseDirectory, "assets", "branding",
            symbol ? (white ? "casterdesk-symbol-white.svg" : "casterdesk-symbol-black.svg") : white ? "casterdesk-horizontal-white.svg" : "casterdesk-horizontal-black.svg");
        var root = XDocument.Load(file).Root!;
        var box = root.Attribute("viewBox")!.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => double.Parse(v, CultureInfo.InvariantCulture)).ToArray();
        var canvas = new Canvas { Width = box[2], Height = box[3] };
        foreach (var element in root.Elements().Where(e => e.Name.LocalName == "path")) {
            var geometry = StreamGeometry.Parse((element.Attribute("fill-rule")?.Value == "evenodd" ? "F0 " : "F1 ") + element.Attribute("d")!.Value);
            canvas.Children.Add(new global::Avalonia.Controls.Shapes.Path {
                Data = geometry, Fill = Brush.Parse(element.Attribute("fill")!.Value)
            });
        }
        var mark = new Viewbox { Name = symbol ? "casterdesk-frame-symbol" : "casterdesk-brand", Width = width,
            Height = width * box[3] / box[2], Stretch = Stretch.Uniform, Child = canvas };
        global::Avalonia.Automation.AutomationProperties.SetName(mark, "CasterDesk");
        return mark;
    }
}
