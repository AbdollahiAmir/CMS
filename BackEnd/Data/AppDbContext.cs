using BackEnd.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        public DbSet<ContentPage> ContentPages { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostCategory> PostCategories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<SiteSetting> SiteSettings { get; set; }
        public DbSet<ContentRevision> ContentRevisions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<ProductAttributeValue> ProductAttributeValues { get; set; }

        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Brand>()
                .HasIndex(b => b.Slug)
                .IsUnique();

            builder.Entity<ProductCategory>()
                .HasIndex(c => c.Slug)
                .IsUnique();

            builder.Entity<ProductCategory>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Product>()
                .HasIndex(p => p.Slug)
                .IsUnique();

            builder.Entity<Product>()
                .HasIndex(p => new { p.IsActive, p.CreatedAt });

            builder.Entity<Product>()
                .HasIndex(p => p.Sku)
                .IsUnique()
                .HasFilter("[Sku] IS NOT NULL");

            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            builder.Entity<Product>()
                .Property(p => p.SalePrice)
                .HasPrecision(18, 2);

            builder.Entity<Product>()
                .HasOne(p => p.Brand)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Product>()
                .HasMany(p => p.Categories)
                .WithMany(c => c.Products)
                .UsingEntity(j => j.ToTable("ProductCategoryLinks"));

            builder.Entity<Cart>()
                .HasIndex(c => c.UserId)
                .IsUnique();

            builder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CartItem>()
                .HasIndex(i => new { i.CartId, i.ProductId })
                .IsUnique();

            builder.Entity<CartItem>()
                .HasOne(i => i.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();

            builder.Entity<Order>()
                .HasIndex(o => o.UserId);

            builder.Entity<Order>()
                .HasIndex(o => new { o.Status, o.CreatedAt });

            builder.Entity<Order>()
                .Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<OrderItem>()
                .Property(i => i.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(i => i.LineTotal)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .HasOne(i => i.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItem>()
                .HasOne(i => i.Product)
                .WithMany()
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<ContentPage>()
                .HasIndex(p => p.Slug)
                .IsUnique();

            builder.Entity<ContentPage>()
                .HasIndex(p => new { p.IsPublished, p.PublishedAt });

            builder.Entity<Post>()
                .HasIndex(p => p.Slug)
                .IsUnique();

            builder.Entity<Post>()
                .HasIndex(p => new { p.IsPublished, p.PublishedAt });

            builder.Entity<Post>()
                .HasIndex(p => p.CategoryId);

            builder.Entity<Post>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Posts)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Post>()
                .HasMany(p => p.Tags)
                .WithMany(t => t.Posts)
                .UsingEntity(j => j.ToTable("PostTagLinks"));

            builder.Entity<PostCategory>()
                .HasIndex(c => c.Slug)
                .IsUnique();

            builder.Entity<Tag>()
                .HasIndex(t => t.Slug)
                .IsUnique();

            builder.Entity<MediaFile>()
                .HasIndex(m => m.StoredName)
                .IsUnique();

            builder.Entity<MediaFile>()
                .HasIndex(m => m.CreatedAt);

            builder.Entity<Menu>()
                .HasIndex(m => m.Key)
                .IsUnique();

            builder.Entity<MenuItem>()
                .HasOne(i => i.Menu)
                .WithMany(m => m.Items)
                .HasForeignKey(i => i.MenuId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MenuItem>()
                .HasOne(i => i.Parent)
                .WithMany(i => i.Children)
                .HasForeignKey(i => i.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProductImage>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductImage>()
                .HasIndex(i => new { i.ProductId, i.SortOrder });

            builder.Entity<ProductAttribute>()
                .HasIndex(a => a.Slug)
                .IsUnique();

            builder.Entity<ProductAttributeValue>()
                .HasOne(v => v.Product)
                .WithMany(p => p.AttributeValues)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductAttributeValue>()
                .HasOne(v => v.Attribute)
                .WithMany()
                .HasForeignKey(v => v.AttributeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductAttributeValue>()
                .HasIndex(v => new { v.ProductId, v.AttributeId })
                .IsUnique();

            builder.Entity<ProductAttributeValue>()
                .HasIndex(v => new { v.AttributeId, v.Value });

            builder.Entity<ContentRevision>()
                .HasIndex(r => new { r.EntityType, r.EntityId, r.CreatedAt });

            builder.Entity<AuditLog>()
                .HasIndex(l => l.CreatedAt);

            builder.Entity<AuditLog>()
                .HasIndex(l => l.UserId);

            builder.Entity<AuditLog>()
                .HasIndex(l => new { l.Controller, l.EntityId });

            builder.Entity<RefreshToken>()
                .HasIndex(t => t.TokenHash)
                .IsUnique();

            builder.Entity<RefreshToken>()
                .HasIndex(t => t.UserId);

            builder.Entity<RefreshToken>()
                .HasIndex(t => t.FamilyId);

            builder.Entity<RefreshToken>()
                .HasIndex(t => t.ExpiresAt);

            builder.Entity<RefreshToken>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}