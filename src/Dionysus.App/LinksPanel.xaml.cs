// SPDX-License-Identifier: GPL-3.0-only
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Dionysus.App;

// Shows a build's "link:<label>" outputs as clickable links
public partial class LinksPanel : UserControl
{
    private const string Prefix = "link:";

    public LinksPanel()
    {
        InitializeComponent();
    }

    public static bool HasLinks(IReadOnlyDictionary<string, string> outputs) =>
        outputs.Keys.Any(key => key.StartsWith(Prefix));

    public void Show(IReadOnlyDictionary<string, string> outputs)
    {
        LinkList.Children.Clear();
        foreach (var (key, url) in outputs.Where(o => o.Key.StartsWith(Prefix)))
        {
            var link = new Hyperlink(new Run(key[Prefix.Length..])) { NavigateUri = new Uri(url) };
            link.SetResourceReference(TextElement.ForegroundProperty, "AccentTextFillColorPrimaryBrush");
            link.RequestNavigate += (_, e) =>
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            };
            LinkList.Children.Add(new TextBlock(link) { Margin = new Thickness(0, 2, 0, 2) });
        }
    }
}