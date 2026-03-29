namespace Edgar.Models
{
    public class CliOptions
    {
        public bool SkipDb { get; set; } = false;
        public string ProjectRoot { get; set; } = "";

        public bool extractFromFile { get; set; } = false;
    }
}
