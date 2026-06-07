namespace PharmeasyAPI.Models;

public class Coupon
{
    public int Id { get; set; }
    public string CouponName { get; set; } = string.Empty;
    public string Discount { get; set; } = string.Empty;
    public CouponType CouponType { get; set; }
    public decimal Value { get; set; }
    public int UsageLimit { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<CouponUsage> CouponUsages { get; set; } = new List<CouponUsage>();
}
