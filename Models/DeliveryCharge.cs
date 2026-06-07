namespace PharmeasyAPI.Models;

public class DeliveryCharge
{
    public int Id { get; set; }
    public string Pincode { get; set; } = string.Empty;
    public decimal Charge { get; set; }
    public bool IsDeliverable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
