namespace Edgar.Models
{
    public class ExtractedSections
    {
        public string Item1AText { get; set; } = string.Empty;
        public string? Item7Text { get; set; }

        public bool FoundItem1A { get; set; }
        public bool FoundItem7 { get; set; }

        public int WordCountItem1A { get; set; }
        public int WordCountItem7 { get; set; }

        // Helpful for debugging / reproducibility
        public bool LooksLikeTocHit { get; set; }
        public string ExtractionMethodVersion { get; set; } = "v1";
    }
}

