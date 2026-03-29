using Edgar.Config;
using Edgar.Pipeline;
using Edgar.Utilities;

public class Program
{
    public static async Task Main(string[] args)
    {
        var cli = CliParser.Parse(args);
        
        AppSettings settings = AppSettings.Load(
            !string.IsNullOrWhiteSpace(cli.ProjectRoot) ? cli.ProjectRoot : ""
            );

        var pipeline = new PanelBuilder(settings);

        await pipeline.RunAsync(skipDb: cli.SkipDb, extractFromFile: cli.extractFromFile);
    }
}
