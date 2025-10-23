using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BarterSystem.Models
{
    public class Item
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Item name is required")]
        [StringLength(100, ErrorMessage = "Item name cannot exceed 100 characters")]
        public string Name { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        [ForeignKey("Owner")]
        public int OwnerId { get; set; }
        public User Owner { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // For file upload (not stored in database)
        [NotMapped]
        public IFormFile? ImageFile { get; set; }
    }
}