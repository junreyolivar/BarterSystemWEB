using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarterSystem.Models
{
    public class TradeRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RequesterId { get; set; }

        [ForeignKey("RequesterId")]
        public User? Requester { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [ForeignKey("ReceiverId")]
        public User? Receiver { get; set; }

        [Required]
        public int OfferedItemId { get; set; }

        [ForeignKey("OfferedItemId")]
        public Item? OfferedItem { get; set; }

        public int? RequestedItemId { get; set; }

        [ForeignKey("RequestedItemId")]
        public Item? RequestedItem { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}