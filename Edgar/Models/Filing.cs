namespace Edgar.Models
{
    public class Filing
    {
        // master.idx fields
        // CIK|Company Name|Form Type|Date Filed|Filename

        public string CIK { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string FormType { get; set; } = string.Empty;
        public DateTime DateFiled { get; set; }

        public string Filename { get; set; } = string.Empty;
    }
}
