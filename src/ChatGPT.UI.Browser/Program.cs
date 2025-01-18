using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("browser")]

namespace ChatGPT.UI.Browser;

internal class Program
{
    private static async Task Main(string[] args) 
        => await BuildAvaloniaApp().StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder
            .Configure<App>()
            .WithInterFont();
}
