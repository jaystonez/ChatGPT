using System.Runtime.InteropServices.JavaScript;

namespace ChatGPT.UI.Browser.Services;

internal static partial class Interop
{
    [JSExport]
    internal static void SaveSettings()
    {
        if (Application.Current is App app)
        {
            app.SaveSettings();
        }
    }
}
