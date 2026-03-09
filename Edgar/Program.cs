using Edgar.Config;
using Edgar.Pipeline;

public class Program
{
    public static async Task Main(string[] args)
    {
        AppSettings settings = AppSettings.Load();

        var pipeline = new PanelBuilder(settings);
        await pipeline.RunAsync();
    }
}
