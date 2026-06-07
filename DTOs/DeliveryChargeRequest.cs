using System.ComponentModel.DataAnnotations;

namespace PharmeasyAPI.DTOs;

public class CreateDeliveryChargeRequest
{
    [Required]
    public string Pincode { get; set; } = string.Empty;

    [Required]
    public decimal Charge { get; set; }

    [Required]
    public bool IsDeliverable { get; set; }
}

public class PatchDeliveryChargeRequest
{
    public string? Pincode { get; set; }
    public decimal? Charge { get; set; }
    public bool? IsDeliverable { get; set; }
}
