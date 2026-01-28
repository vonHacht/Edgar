using Edgar.Pipeline;

class Program
{
    static async Task Main(string[] args)
    {
        var pipeline = new PanelBuilder();
        await pipeline.RunAsync();
    }
}
