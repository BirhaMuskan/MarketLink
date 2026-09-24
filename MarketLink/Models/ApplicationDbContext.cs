
using Microsoft.EntityFrameworkCore;

namespace MarketLink.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }
        public DbSet<Role> roles { get; set; }

        public DbSet<User> users { get; set; }

        public DbSet<Customer> customers { get; set; }
        public DbSet<Farmer> farmers { get; set; }
        public DbSet<RefreshToken> refreshtokens { get; set; }
        public DbSet<Market> markets { get; set; }
        public DbSet<MarketDay> marketdays { get; set; }
        public DbSet<FarmerMarket> farmermarkets { get; set; }
        public DbSet<FarmerMarketDay> farmermarketdays { get; set; }
        public DbSet<PickupSlot> pickupslots { get; set; }
        public DbSet<Category> categories { get; set; }
        public DbSet<UnitOfMeasure> unitofmeasures { get; set; }
        public DbSet<Product> products { get; set; }
        public DbSet<FarmerProduct> farmerproducts { get; set; }
        public DbSet<ProductImage> productimages { get; set; }
        public DbSet<RecurringStockTemplate> recurringstocktemplates { get; set; }
        public DbSet<Inventory> inventories { get; set; }
        public DbSet<InventoryHistory> inventoryhistories { get; set; }
        public DbSet<Cart> carts { get; set; }
        public DbSet<CartItem> cartitems { get; set; }
        public DbSet<Order> orders { get; set; }
        public DbSet<OrderItem> orderitems { get; set; }
        public DbSet<OrderStatusHistory> orderstatushistories { get; set; }
        public DbSet<FavoriteFarmer> favoritefarmers { get; set; }
        public DbSet<FavoriteProduct> favoriteproducts { get; set; }
        public DbSet<FavoriteMarket> favoritemarkets { get; set; }
        public DbSet<Review> reviews { get; set; }
        public DbSet<ReviewResponse> reviewresponses { get; set; }
        public DbSet<Notification> notifications { get; set; }
        public DbSet<NotificationPreference> notificationpreferences { get; set; }
        public DbSet<Announcement> announcements { get; set; }
        public DbSet<SearchHistory> searchhistories { get; set; }
        public DbSet<AiModelRun> aimodelruns { get; set; }
        public DbSet<DemandPrediction> demandpredictions { get; set; }
        public DbSet<AiRecommendation> airecommendations { get; set; }
        public DbSet<AnomalyDetection> anomalydetections { get; set; }
        public DbSet<WasteRiskPrediction> wasteriskpredictions { get; set; }
        public DbSet<AssistantConversation> assistantconversations { get; set; }
        public DbSet<AssistantMessage> assistantmessages { get; set; }
        public DbSet<AuditLog> auditlogs { get; set; }
        public DbSet<ModerationAction> moderationactions { get; set; }
        public DbSet<GeneratedReport> generatedreports { get; set; }
        public DbSet<SystemSetting> systemsettings { get; set; }










        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<FarmerMarketDay>()
                .HasOne(fmd => fmd.FarmerMarket)
                .WithMany()
                .HasForeignKey(fmd => fmd.FarmerMarketId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FarmerMarketDay>()
                .HasOne(fmd => fmd.MarketDay)
                .WithMany()
                .HasForeignKey(fmd => fmd.MarketDayId)
                .OnDelete(DeleteBehavior.Restrict);

            
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.FarmerProduct)
                .WithMany()
                .HasForeignKey(i => i.FarmerProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.FarmerMarket)
                .WithMany()
                .HasForeignKey(c => c.FarmerMarketId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Farmer)
                .WithMany()
                .HasForeignKey(c => c.FarmerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.FarmerProduct)
                .WithMany()
                .HasForeignKey(ci => ci.FarmerProductId)
                .OnDelete(DeleteBehavior.Restrict);

            
            modelBuilder.Entity<Order>()
                .HasOne(o => o.FarmerMarket)
                .WithMany()
                .HasForeignKey(o => o.FarmerMarketId)
                .OnDelete(DeleteBehavior.Restrict);

           
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Farmer)
                .WithMany()
                .HasForeignKey(o => o.FarmerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.FarmerProduct)
                .WithMany()
                .HasForeignKey(oi => oi.FarmerProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FavoriteFarmer>()
                .HasOne(ff => ff.Farmer)
                .WithMany()
                .HasForeignKey(ff => ff.FarmerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FavoriteProduct>()
                .HasOne(fp => fp.FarmerProduct)
                .WithMany()
                .HasForeignKey(fp => fp.FarmerProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReviewResponse>()
                .HasOne(rr => rr.Review)
                .WithMany()
                .HasForeignKey(rr => rr.ReviewId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DemandPrediction>()
                .HasOne(dp => dp.FarmerProduct)
                .WithMany()
                .HasForeignKey(dp => dp.FarmerProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AnomalyDetection>()
                .HasOne(ad => ad.FarmerProduct)
                .WithMany()
                .HasForeignKey(ad => ad.FarmerProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }

    }
}
