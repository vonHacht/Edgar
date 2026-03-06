namespace Edgar.Models
{
    public class ExtractedSections
    {
        public string Item1AText { get; set; } = string.Empty;
        public string? Item7Text { get; set; }

        public bool FoundItem1A { get; set; }
        public bool FoundItem7 { get; set; }

        public bool Text250WordsItem7
        {
            get
            {
                return Item7Text != null &&
                       Item7Text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length >= 250;
            }
        }

        // Helpful for debugging / reproducibility
        public bool LooksLikeTocHit { get; set; }
        public string ExtractionMethodVersion { get; set; } = "v1";
    }
}

