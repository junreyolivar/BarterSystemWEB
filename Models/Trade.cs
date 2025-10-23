using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BarterSystem.Models
{
    public class Trade
    {
        public int Id { get; set; }

        [Required]
        public int RequestedById { get; set; }
        public User RequestedBy { get; set; } = null!;

        [Required]
        public int RequestedToId { get; set; }
        public User RequestedTo { get; set; } = null!;

        [Required]
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<TradeItem> TradeItems { get; set; } = new List<TradeItem>();
    }
}