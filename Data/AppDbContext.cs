using Microsoft.EntityFrameworkCore;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<DoctorProfile> DoctorProfiles => Set<DoctorProfile>();
    public DbSet<AdminProfile> AdminProfiles => Set<AdminProfile>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<DeliveryCharge> DeliveryCharges => Set<DeliveryCharge>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<CheckoutSession> CheckoutSessions => Set<CheckoutSession>();
    public DbSet<BookingSession> BookingSessions => Set<BookingSession>();
    public DbSet<DoctorUnAvailability> DoctorUnAvailabilities => Set<DoctorUnAvailability>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Covid> Covids => Set<Covid>();
    public DbSet<HealthcareDevice> HealthcareDevices => Set<HealthcareDevice>();
    public DbSet<HealthFoodDrink> HealthFoodDrinks => Set<HealthFoodDrink>();
    public DbSet<Skincare> Skincares => Set<Skincare>();
    public DbSet<HealthcareData> HealthcareData => Set<HealthcareData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // User
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .HasMaxLength(255)
            .IsRequired();

        // One-to-one: User → DoctorProfile
        modelBuilder.Entity<DoctorProfile>()
            .HasOne(d => d.User)
            .WithOne(u => u.DoctorProfile)
            .HasForeignKey<DoctorProfile>(d => d.UserId);

        modelBuilder.Entity<DoctorProfile>()
            .Property(d => d.IsActive)
            .HasDefaultValue(true);

        modelBuilder.Entity<DoctorProfile>()
            .Property(d => d.ConsultationFee)
            .HasColumnType("decimal(18,2)");

        // BookingSession relationships
        modelBuilder.Entity<BookingSession>()
            .HasOne(bs => bs.User)
            .WithMany()
            .HasForeignKey(bs => bs.UserId);

        modelBuilder.Entity<BookingSession>()
            .HasOne(bs => bs.CustomerProfile)
            .WithMany()
            .HasForeignKey(bs => bs.CustomerProfileId);

        modelBuilder.Entity<BookingSession>()
            .HasOne(bs => bs.DoctorProfile)
            .WithMany()
            .HasForeignKey(bs => bs.DoctorProfileId);

        modelBuilder.Entity<BookingSession>()
            .Property(bs => bs.ConsultationFee)
            .HasColumnType("decimal(18,2)");

        // One-to-one: User → AdminProfile
        modelBuilder.Entity<AdminProfile>()
            .HasOne(a => a.User)
            .WithOne(u => u.AdminProfile)
            .HasForeignKey<AdminProfile>(a => a.UserId);

        // One-to-one: User → CustomerProfile
        modelBuilder.Entity<CustomerProfile>()
            .HasOne(c => c.User)
            .WithOne(u => u.CustomerProfile)
            .HasForeignKey<CustomerProfile>(c => c.UserId);

        // One-to-many: User → Products
        modelBuilder.Entity<Product>()
            .HasOne(p => p.User)
            .WithMany(u => u.Products)
            .HasForeignKey(p => p.UserId);

        // One-to-many: Category → Products
        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId);

        // One-to-many: User → Carts
        modelBuilder.Entity<Cart>()
            .HasOne(c => c.User)
            .WithMany(u => u.Carts)
            .HasForeignKey(c => c.UserId);

        // One-to-many: Cart → CartItems
        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Cart)
            .WithMany(c => c.CartItems)
            .HasForeignKey(ci => ci.CartId);

        // Many-to-one: CartItem → Product
        modelBuilder.Entity<CartItem>()
            .HasOne(ci => ci.Product)
            .WithMany()
            .HasForeignKey(ci => ci.ProductId);
        // One-to-many: DoctorProfile -> Unavailabilities
        modelBuilder.Entity<DoctorUnAvailability>()
            .HasOne(d => d.DoctorProfile)
            .WithMany(p => p.Unavailabilities)
            .HasForeignKey(d => d.DoctorProfileId);

        // One-to-many: DoctorProfile -> Bookings
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.DoctorProfile)
            .WithMany(p => p.Bookings)
            .HasForeignKey(b => b.DoctorProfileId);

        // One-to-many: CustomerProfile -> Bookings
        modelBuilder.Entity<Booking>()
            .HasOne(b => b.CustomerProfile)
            .WithMany(p => p.Bookings)
            .HasForeignKey(b => b.CustomerProfileId);

        // One-to-many: CustomerProfile -> Orders
        modelBuilder.Entity<Order>()
            .HasOne(o => o.CustomerProfile)
            .WithMany(p => p.Orders)
            .HasForeignKey(o => o.CustomerProfileId);

        // Many-to-one: Order -> Product
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Product)
            .WithMany()
            .HasForeignKey(o => o.ProductId);

        modelBuilder.Entity<DoctorUnAvailability>()
            .Property(d => d.StartTime)
            .HasColumnType("time");

        modelBuilder.Entity<DoctorUnAvailability>()
            .Property(d => d.EndTime)
            .HasColumnType("time");

        modelBuilder.Entity<Booking>()
            .Property(b => b.StartTime)
            .HasColumnType("time");

        modelBuilder.Entity<Booking>()
            .Property(b => b.EndTime)
            .HasColumnType("time");

        modelBuilder.Entity<Order>()
            .Property(o => o.PurchasePrice)
            .HasColumnType("decimal(18,2)");

        // One-to-many: CustomerProfile -> CouponUsages
        modelBuilder.Entity<CouponUsage>()
            .HasOne(cu => cu.CustomerProfile)
            .WithMany(cp => cp.CouponUsages)
            .HasForeignKey(cu => cu.CustomerProfileId);

        // One-to-many: Coupon -> CouponUsages
        modelBuilder.Entity<CouponUsage>()
            .HasOne(cu => cu.Coupon)
            .WithMany(c => c.CouponUsages)
            .HasForeignKey(cu => cu.CouponId);

        modelBuilder.Entity<CouponUsage>()
            .Property(cu => cu.UsedAt)
            .HasColumnType("datetime(6)");
        // Decimal columns
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Product>()
            .Property(p => p.DiscountedPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Cart>()
            .Property(c => c.TotalPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<DeliveryCharge>()
            .Property(d => d.Charge)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Coupon>()
            .Property(c => c.Value)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Covid>()
            .Property(c => c.Price)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<HealthcareDevice>()
            .Property(h => h.Price)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<HealthFoodDrink>()
            .Property(h => h.Price)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Skincare>()
            .Property(s => s.Price)
            .HasColumnType("decimal(18,2)");
    }
}
