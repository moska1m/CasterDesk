namespace CasterDesk.Avalonia;

internal sealed class HeaderTimer : Border
{
    readonly TextsView texts;
    readonly TextBlock value = new() { Name = "header-timer-value", FontSize = 24, FontWeight = FontWeight.Bold, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6,0) };
    readonly TextBlock state = new() { Foreground = Brush.Parse("#40BAD4"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4,0) };
    readonly Button start, pause, subtract, add;
    bool compact;
    public HeaderTimer(TextsView texts)
    {
        this.texts = texts; Name = "header-timer"; Margin = new Thickness(8,5); Padding = new Thickness(4);
        BorderBrush = Brush.Parse("#425261"); BorderThickness = new Thickness(1); VerticalAlignment = VerticalAlignment.Center;
        var strip = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 }; Child = strip;
        strip.Children.Add(new TextBlock { Text = "◷", FontSize = 24, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center });
        strip.Children.Add(value); strip.Children.Add(state);
        Button Control(string caption, string name, string tooltip, Action action) {
            var button = Ui.Button(caption, action); button.Name = name; button.Margin = new Thickness(1); button.Padding = new Thickness(7,4); button.Foreground = Brushes.White;
            ToolTip.SetTip(button, L.T(tooltip)); global::Avalonia.Automation.AutomationProperties.SetName(button, L.T(tooltip)); strip.Children.Add(button); return button;
        }
        start = Control("▶", "header-timer-start", "Iniciar / Retomar", texts.StartFromHeader);
        pause = Control("Ⅱ", "header-timer-pause", "Pausar", texts.PauseClock);
        subtract = Control("− 1 min", "header-timer-subtract", "− 1 min", () => texts.AdjustClock(-60));
        add = Control("+ 1 min", "header-timer-add", "+ 1 min", () => texts.AdjustClock(60));
        texts.ClockChanged += Update; Update();
    }
    public void Compact(bool value) { compact = value; subtract.Content = value ? "−" : "− 1 min"; add.Content = value ? "+" : "+ 1 min"; Update(); }
    public void Stop() => texts.ClockChanged -= Update;
    void Update() {
        int seconds = (int)Math.Ceiling(texts.Remaining);
        value.Text = seconds >= 3600 ? $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}" : $"{seconds / 60:00}:{seconds % 60:00}";
        state.Text = compact ? (texts.Running ? "●" : "") : L.T(texts.Running ? "● Em andamento" : "Pausado / pronto");
        start.IsEnabled = !texts.Running && seconds > 0; pause.IsEnabled = texts.Running; subtract.IsEnabled = seconds > 0;
    }
}
