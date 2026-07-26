using CloneAmazonBack.Models;
using Microsoft.EntityFrameworkCore;

namespace CloneAmazonBack.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserAddress> UserAddresses => Set<UserAddress>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Seller> Sellers => Set<Seller>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
    public DbSet<ProductGallery> ProductGalleries => Set<ProductGallery>();
    public DbSet<ProductVideo> ProductVideos => Set<ProductVideo>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<PromoCode> PromoCodes => Set<PromoCode>();
    public DbSet<PromoCodeProduct> PromoCodeProducts => Set<PromoCodeProduct>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<GiftCard> GiftCards => Set<GiftCard>();
    public DbSet<ProductQuestion> ProductQuestions => Set<ProductQuestion>();
    public DbSet<ProductAnswer> ProductAnswers => Set<ProductAnswer>();
    public DbSet<QuestionVote> QuestionVotes => Set<QuestionVote>();
    public DbSet<HistoryPriceProduct> HistoryPriceProducts => Set<HistoryPriceProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<SellerStatus>();
        modelBuilder.HasPostgresEnum<ProductStatus>();
        modelBuilder.HasPostgresEnum<DiscountType>();
        modelBuilder.HasPostgresEnum<OrderStatus>();

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.UserRole)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.UserRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(u => u.Profile)
                .WithOne(p => p.User)
                .HasForeignKey<UserProfile>(p => p.UserId);

            entity.HasOne(u => u.Seller)
                .WithOne(s => s.User)
                .HasForeignKey<Seller>(s => s.UserId);
        });

        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.HasOne(a => a.User)
                .WithMany(u => u.Addresses)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(p => p.UserId);
        });

        modelBuilder.Entity<Seller>(entity =>
        {
            entity.HasIndex(s => s.StoreSlug).IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => c.Slug).IsUnique();

            entity.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Slug).IsUnique();
            entity.HasIndex(p => p.Sku).IsUnique();

            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            entity.Property(p => p.OldCost).HasColumnType("decimal(18,2)");

            entity.HasOne(p => p.Seller)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductAttributeValue>(entity =>
        {
            entity.HasOne(a => a.Product)
                .WithMany(p => p.AttributeValues)
                .HasForeignKey(a => a.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductGallery>(entity =>
        {
            entity.HasOne(g => g.Product)
                .WithMany(p => p.Galleries)
                .HasForeignKey(g => g.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductVideo>(entity =>
        {
            entity.HasOne(v => v.Product)
                .WithMany(p => p.Videos)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Gallery)
                .WithMany(g => g.Reviews)
                .HasForeignKey(r => r.ProductGalleryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(r => r.Video)
                .WithMany(v => v.Reviews)
                .HasForeignKey(r => r.ProductVideoId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasOne(c => c.User)
                .WithMany(u => u.Carts)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.PromoCode)
                .WithMany(p => p.Carts)
                .HasForeignKey(c => c.PromoCodeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.Property(i => i.Price).HasColumnType("decimal(18,2)");

            entity.HasOne(i => i.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PromoCode>(entity =>
        {
            entity.HasIndex(p => p.Code).IsUnique();

            entity.Property(p => p.DiscountValue).HasColumnType("decimal(18,2)");
            entity.Property(p => p.MinOrderAmount).HasColumnType("decimal(18,2)");
            entity.Property(p => p.MaxDiscountAmount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<PromoCodeProduct>(entity =>
        {
            entity.HasOne(pp => pp.Product)
                .WithMany(p => p.PromoCodeProducts)
                .HasForeignKey(pp => pp.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pp => pp.PromoCode)
                .WithMany(p => p.PromoCodeProducts)
                .HasForeignKey(pp => pp.PromoCodeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            entity.Property(o => o.DiscountAmount).HasColumnType("decimal(18,2)");

            entity.HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Seller)
                .WithMany(s => s.Orders)
                .HasForeignKey(o => o.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.PromoCode)
                .WithMany(p => p.Orders)
                .HasForeignKey(o => o.PromoCodeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(i => i.ProductPrice).HasColumnType("decimal(18,2)");
            entity.Property(i => i.TotalPrice).HasColumnType("decimal(18,2)");

            entity.HasOne(i => i.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<GiftCard>(entity =>
        {
            entity.HasIndex(g => g.Code).IsUnique();

            entity.Property(g => g.InitialBalance).HasColumnType("decimal(18,2)");
            entity.Property(g => g.CurrentBalance).HasColumnType("decimal(18,2)");

            entity.HasOne(g => g.PurchasedByUser)
                .WithMany(u => u.PurchasedGiftCards)
                .HasForeignKey(g => g.PurchasedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(g => g.ActivatedByUser)
                .WithMany(u => u.ActivatedGiftCards)
                .HasForeignKey(g => g.ActivatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProductQuestion>(entity =>
        {
            entity.HasOne(q => q.Product)
                .WithMany(p => p.Questions)
                .HasForeignKey(q => q.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(q => q.User)
                .WithMany(u => u.Questions)
                .HasForeignKey(q => q.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductAnswer>(entity =>
        {
            entity.HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.User)
                .WithMany(u => u.Answers)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<QuestionVote>(entity =>
        {
            entity.HasOne(v => v.Question)
                .WithMany(q => q.Votes)
                .HasForeignKey(v => v.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.User)
                .WithMany(u => u.QuestionVotes)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<HistoryPriceProduct>(entity =>
        {
            entity.Property(h => h.Price).HasColumnType("decimal(18,2)");

            entity.HasOne(h => h.Product)
                .WithMany(p => p.PriceHistory)
                .HasForeignKey(h => h.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
