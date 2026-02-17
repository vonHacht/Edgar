using Edgar.Pipeline;

class Program
{
    static async Task Main(string[] args)
    {
        DotNetEnv.Env.Load();

        var pipeline = new PanelBuilder();
        await pipeline.RunAsync();
    }
}
