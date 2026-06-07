using System.ComponentModel.DataAnnotations;

namespace PharmeasyAPI.DTOs;

public class ProductCheckoutRequest
{
    public string? CouponName { get; set; }
}

public class CheckoutProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class ProductCheckoutResponse
{
    public IReadOnlyList<CheckoutProductDto> Products { get; set; } = Array.Empty<CheckoutProductDto>();
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentLinkUrl { get; set; } = string.Empty;
    public string PaymentLinkId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
