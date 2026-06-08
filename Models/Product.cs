namespace PharmeasyAPI.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal DiscountedPrice { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Uses { get; set; } = string.Empty;
    public string SideEffects { get; set; } = string.Empty;
    public string DirectionForUse { get; set; } = string.Empty;
    public string QuickTips { get; set; } = string.Empty;
    public string StorageDisposal { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public string ModeOfAction { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
