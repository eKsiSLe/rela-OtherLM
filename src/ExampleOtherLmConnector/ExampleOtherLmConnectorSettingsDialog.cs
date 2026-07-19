using System.Windows;
using System.Windows.Controls;

namespace ExampleOtherLmConnector;

internal static class ExampleOtherLmConnectorSettingsDialog
{
    public static bool TryCollect(ExampleOtherLmConnectorSettings settings)
    {
        var window = new Window
        {
            Title = "Example Device Settings",
            Width = 440,
            Height = 220,
            MinWidth = 400,
            MinHeight = 200,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ShowInTaskbar = false
        };

        var owner = Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(candidate => candidate.IsActive);
        if (owner != null)
        {
            window.Owner = owner;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        var root = new Grid { Margin = new Thickness(20) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var handedness = AddChoice(
            root,
            row: 0,
            label: "Handedness",
            values: new[] { "Right-handed", "Left-handed" });
        handedness.SelectedIndex = settings.Handedness == "LH" ? 1 : 0;

        var mode = AddChoice(
            root,
            row: 1,
            label: "Mode",
            values: new[] { "Normal", "Chipping", "Putting" });
        mode.SelectedIndex = settings.Mode switch
        {
            "PUTTING" => 2,
            "CHIPPING" => 1,
            _ => 0
        };

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        var cancel = new Button
        {
            Content = "Cancel",
            MinWidth = 90,
            Height = 32,
            Margin = new Thickness(0, 0, 8, 0),
            IsCancel = true
        };
        var save = new Button
        {
            Content = "Save",
            MinWidth = 90,
            Height = 32,
            IsDefault = true
        };

        cancel.Click += (_, _) => window.DialogResult = false;
        save.Click += (_, _) =>
        {
            settings.Handedness = handedness.SelectedIndex == 1 ? "LH" : "RH";
            settings.Mode = mode.SelectedIndex switch
            {
                2 => "PUTTING",
                1 => "CHIPPING",
                _ => "NORMAL"
            };
            window.DialogResult = true;
        };

        actions.Children.Add(cancel);
        actions.Children.Add(save);
        Grid.SetRow(actions, 2);
        Grid.SetColumnSpan(actions, 2);
        root.Children.Add(actions);

        window.Content = root;
        return window.ShowDialog() == true;
    }

    private static ComboBox AddChoice(
        Grid root,
        int row,
        string label,
        IReadOnlyList<string> values)
    {
        var text = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 12)
        };
        Grid.SetRow(text, row);
        root.Children.Add(text);

        var choice = new ComboBox
        {
            Height = 32,
            ItemsSource = values,
            Margin = new Thickness(0, 0, 0, 12),
            VerticalContentAlignment = VerticalAlignment.Center
        };
        Grid.SetRow(choice, row);
        Grid.SetColumn(choice, 1);
        root.Children.Add(choice);
        return choice;
    }
}
