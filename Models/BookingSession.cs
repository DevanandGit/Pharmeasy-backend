namespace PharmeasyAPI.Models;

public class BookingSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CustomerProfileId { get; set; }
    public int DoctorProfileId { get; set; }
    public string BookingSnapshot { get; set; } = string.Empty;
    public decimal ConsultationFee { get; set; }
    public string PaymentLinkId { get; set; } = string.Empty;
    public string PaymentLinkUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "Created";
    public string? RazorpayPaymentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public CustomerProfile CustomerProfile { get; set; } = null!;
    public DoctorProfile DoctorProfile { get; set; } = null!;
}
