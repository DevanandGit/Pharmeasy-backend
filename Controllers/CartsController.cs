using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmeasyAPI.Data;
using PharmeasyAPI.DTOs;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.Controllers;

/// <summary>Shopping cart management for the authenticated user.</summary>
[ApiController]
[Route("cart")]
[Authorize]
[Produces("application/json")]
public class CartsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CartsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Add a product to the cart (or increase its quantity if already present).</summary>
    /// <response code="200">Updated cart with all items and recalculated total.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">Product not found.</response>
    [HttpPost("add")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest req)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var product = await _db.Products.FindAsync(req.ProductId);
        if (product is null) return NotFound(new { message = "Product not found." });

        var cart = await _db.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null)
        {
            cart = new Cart { UserId = userId, TotalPrice = 0m };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
            cart = await _db.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.Id == cart.Id) ?? cart;
        }

        var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == req.ProductId);
        if (existingItem is null)
        {
            existingItem = new CartItem { CartId = cart.Id, ProductId = req.ProductId, Quantity = req.Quantity };
            _db.CartItems.Add(existingItem);
        }
        else
        {
            existingItem.Quantity += req.Quantity;
            existingItem.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await RecalculateCartTotal(cart.Id);

        var updated = await _db.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.Id == cart.Id);

        return Ok(updated);
    }

    /// <summary>Remove a product from the cart (or decrease its quantity).</summary>
    /// <remarks>If <c>quantity</c> is omitted or equals/exceeds the current quantity the item is removed entirely.</remarks>
    /// <response code="200">Updated cart after removal.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">Cart or product not found.</response>
    [HttpPost("remove")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromCart([FromBody] RemoveFromCartRequest req)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var cart = await _db.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null) return NotFound(new { message = "Cart not found." });

        var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == req.ProductId);
        if (item is null) return NotFound(new { message = "Product not in cart." });

        if (!req.Quantity.HasValue || req.Quantity.Value >= item.Quantity)
            _db.CartItems.Remove(item);
        else
        {
            item.Quantity -= req.Quantity.Value;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await RecalculateCartTotal(cart.Id);

        var updated = await _db.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.Id == cart.Id);

        return Ok(updated);
    }

    /// <summary>Retrieve the current user's cart with all items and the recalculated total.</summary>
    /// <param name="couponName">Optional coupon code to preview the discounted total (coupon is NOT applied or recorded).</param>
    /// <response code="200">Cart with items and total price. When a valid coupon is supplied, includes a <c>couponPreview</c> object with the discount breakdown.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">No active cart for this user.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCart([FromQuery] string? couponName)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var cart = await _db.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null) return NotFound(new { message = "Cart not found." });

        await RecalculateCartTotal(cart.Id);
        cart = await _db.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.Id == cart.Id);

        if (string.IsNullOrWhiteSpace(couponName))
            return Ok(cart);

        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.CouponName == couponName);
        if (coupon is null)
            return Ok(new { cart, couponPreview = new { applicable = false, message = "Coupon not found." } });

        var customerProfile = await _db.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
        if (customerProfile is null)
            return Ok(new { cart, couponPreview = new { applicable = false, message = "Customer profile not found." } });

        var usageCount = await _db.CouponUsages.CountAsync(cu =>
            cu.CouponId == coupon.Id && cu.CustomerProfileId == customerProfile.Id);

        if (coupon.UsageLimit > 0 && usageCount >= coupon.UsageLimit)
            return Ok(new { cart, couponPreview = new { applicable = false, message = "Coupon usage limit reached." } });

        decimal discountAmount = coupon.CouponType == CouponType.Percentage
            ? Math.Round(cart!.TotalPrice * (coupon.Value / 100m), 2)
            : coupon.Value;

        decimal totalAfterDiscount = Math.Max(cart!.TotalPrice - discountAmount, 0m);

        return Ok(new
        {
            cart,
            couponPreview = new
            {
                applicable = true,
                couponName = coupon.CouponName,
                discountAmount,
                totalAfterDiscount
            }
        });
    }

    /// <summary>Delete the current user's entire cart and all its items.</summary>
    /// <response code="200">Cart deleted.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">No active cart for this user.</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCart()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var cart = await _db.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null) return NotFound(new { message = "Cart not found." });

        _db.CartItems.RemoveRange(cart.CartItems);
        _db.Carts.Remove(cart);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Cart deleted." });
    }

    private async Task RecalculateCartTotal(int cartId)
    {
        var items = await _db.CartItems
            .Where(ci => ci.CartId == cartId)
            .Include(ci => ci.Product)
            .ToListAsync();

        decimal total = items.Sum(it => it.Product.DiscountedPrice * it.Quantity);

        var cart = await _db.Carts.FindAsync(cartId);
        if (cart is not null)
        {
            cart.TotalPrice = total;
            cart.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
