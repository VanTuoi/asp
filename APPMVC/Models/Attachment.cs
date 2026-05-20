namespace APPMVC.Models
{
    public class Attachment
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int PostId { get; set; }
    }
}
