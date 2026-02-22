using System.ComponentModel.DataAnnotations;

namespace CCCInventory
{
    public class OrderAttachment
    {
        [Key]
        public int Id { get; set; }
        public int OrderNumber { get; set; }
        public string FileName { get; set; } = null!;       // original display name
        public string StoredFileName { get; set; } = null!; // guid-based name stored on disk
        public string ContentType { get; set; } = null!;
        public DateTime UploadedAt { get; set; }
    }
}
