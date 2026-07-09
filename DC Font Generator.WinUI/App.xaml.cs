using System.Text;
using Microsoft.UI.Xaml;

namespace DC_Font_Generator.WinUI;

public partial class App : Application
{
    private Window window;

    public App()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        window = new MainWindow();
        FocusDismissService.Attach(window);
        window.Activate();
    }
}