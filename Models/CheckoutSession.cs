namespace PharmeasyAPI.Models;

public class CheckoutSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CartId { get; set; }
    public int? CouponId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PayableAmount { get; set; }
    public string PaymentLinkId { get; set; } = string.Empty;
    public string PaymentLinkUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "Created";
    public string CartItemsSnapshot { get; set; } = string.Empty;
    public string? RazorpayPaymentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Coupon? Coupon { get; set; }
}
