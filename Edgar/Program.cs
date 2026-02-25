using Edgar.Pipeline;

public class Program
{
    public static async Task Main(string[] args)
    {
        var importer = new ImporterBuilder();

        importer.LoadDataToMemory();

        if (args.Length > 0)
        {
            if (args[0] == "check")
            {
                importer.EnsureConsistentData();
            }
        }

        //var pipeline = new PanelBuilder();
        //await pipeline.RunAsync();
    }
}
