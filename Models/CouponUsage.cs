namespace PharmeasyAPI.Models;

public class CouponUsage
{
    public int Id { get; set; }
    public int CustomerProfileId { get; set; }
    public int CouponId { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public CustomerProfile CustomerProfile { get; set; } = null!;
    public Coupon Coupon { get; set; } = null!;
}
