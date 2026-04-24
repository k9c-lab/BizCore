using System.ComponentModel.DataAnnotations;

namespace BizCore.Models.Entities;

public class Announcement
{
    public int AnnouncementId { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Message { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    public DateTime? PublishFromDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? PublishToDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public int? CreatedByUserId { get; set; }
    public int? UpdatedByUserId { get; set; }

    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
}
