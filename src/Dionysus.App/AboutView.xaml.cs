// SPDX-License-Identifier: GPL-3.0-only
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Dionysus.App;

public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
    }

    // Opens every link on this page in the default browser
    private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}