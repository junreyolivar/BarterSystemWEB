using System.ComponentModel.DataAnnotations.Schema;

namespace BarterSystem.Models
{
    public class TradeItem
    {
        public int Id { get; set; }

        public int TradeId { get; set; }
        public Trade Trade { get; set; } = null!;

        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public bool IsOfferedItem { get; set; } // True = item from requester, False = item from receiver
    }
}