namespace PharmeasyAPI.Models;

public class CustomerProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ProfileImage { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
}
