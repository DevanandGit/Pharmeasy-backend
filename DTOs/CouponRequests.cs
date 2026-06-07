using System.ComponentModel.DataAnnotations;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.DTOs;

public class CreateCouponRequest
{
    [Required]
    public string CouponName { get; set; } = string.Empty;

    [Required]
    public string Discount { get; set; } = string.Empty;

    [Required]
    public CouponType CouponType { get; set; }

    [Required]
    public decimal Value { get; set; }

    public int UsageLimit { get; set; } = 0;
}

public class PatchCouponRequest
{
    public string? CouponName { get; set; }
    public string? Discount { get; set; }
    public CouponType? CouponType { get; set; }
    public decimal? Value { get; set; }
    public int? UsageLimit { get; set; }
}

public class ApplyCouponRequest
{
    [Required]
    public string CouponName { get; set; } = string.Empty;
}
