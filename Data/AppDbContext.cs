using Microsoft.EntityFrameworkCore;
using BarterSystem.Models;

namespace BarterSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Trade> Trades { get; set; }
        public DbSet<TradeItem> TradeItems { get; set; }
        public DbSet<TradeRequest> TradeRequests { get; set; }
        public DbSet<Message> Messages { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique username
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // User -> Items relationship
            modelBuilder.Entity<Item>()
                .HasOne(i => i.Owner)
                .WithMany(u => u.Items)
                .HasForeignKey(i => i.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Trade relationships
            modelBuilder.Entity<Trade>()
                .HasOne(t => t.RequestedBy)
                .WithMany()
                .HasForeignKey(t => t.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Trade>()
                .HasOne(t => t.RequestedTo)
                .WithMany()
                .HasForeignKey(t => t.RequestedToId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TradeItem>()
                .HasOne(ti => ti.Trade)
                .WithMany(t => t.TradeItems)
                .HasForeignKey(ti => ti.TradeId);

            modelBuilder.Entity<TradeItem>()
                .HasOne(ti => ti.Item)
                .WithMany()
                .HasForeignKey(ti => ti.ItemId);

            // TradeRequest relationships
            modelBuilder.Entity<TradeRequest>()
                .HasOne(tr => tr.Requester)
                .WithMany()
                .HasForeignKey(tr => tr.RequesterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TradeRequest>()
                .HasOne(tr => tr.Receiver)
                .WithMany()
                .HasForeignKey(tr => tr.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TradeRequest>()
                .HasOne(tr => tr.OfferedItem)
                .WithMany()
                .HasForeignKey(tr => tr.OfferedItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TradeRequest>()
                .HasOne(tr => tr.RequestedItem)
                .WithMany()
                .HasForeignKey(tr => tr.RequestedItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Message relationships
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}