using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarterSystem.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        [Required]
        public string DisplayName { get; set; } = null!;

        [Required]
        public string Email { get; set; } = null!;

        public string? AvatarUrl { get; set; }

        public ICollection<Item> Items { get; set; } = new List<Item>();
    }
}
