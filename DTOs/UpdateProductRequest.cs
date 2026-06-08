using System.ComponentModel.DataAnnotations;

namespace PharmeasyAPI.DTOs;

public class UpdateProductRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
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

    [Required]
    public int CategoryId { get; set; }
}
