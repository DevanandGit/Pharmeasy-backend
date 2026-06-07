using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmeasyAPI.Data;
using PharmeasyAPI.DTOs;
using PharmeasyAPI.Helpers;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.Controllers;

/// <summary>Discount coupon management and application.</summary>
[ApiController]
[Route("coupons")]
[Produces("application/json")]
public class CouponsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CouponsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Create a new discount coupon. (Admin only)</summary>
    /// <response code="200">Coupon created and returned.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="409">A coupon with this name already exists.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCouponRequest req)
    {
        var exists = await _db.Coupons.AnyAsync(c => c.CouponName == req.CouponName);
        if (exists) return Conflict(new { message = "Coupon with this name already exists." });

        var coupon = new Coupon
        {
            CouponName = req.CouponName,
            Discount = req.Discount,
            CouponType = req.CouponType,
            Value = req.Value,
            UsageLimit = req.UsageLimit
        };

        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync();

        return Ok(coupon);
    }

    /// <summary>List all coupons including their usage records.</summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Items per page, 1–100 (default 20).</param>
    /// <response code="200">Paginated, alphabetically ordered list of coupons.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        return Ok(await _db.Coupons
            .Include(c => c.CouponUsages)
            .OrderBy(c => c.CouponName)
            .PaginateAsync(page, limit));
    }

    /// <summary>Retrieve a single coupon by ID.</summary>
    /// <param name="id">Coupon ID.</param>
    /// <response code="200">Coupon found and returned.</response>
    /// <response code="404">No coupon with the given ID.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var coupon = await _db.Coupons
            .Include(c => c.CouponUsages)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (coupon is null) return NotFound(new { message = "Coupon not found." });
        return Ok(coupon);
    }

    /// <summary>Partially update a coupon (only supplied fields are changed). (Admin only)</summary>
    /// <param name="id">Coupon ID.</param>
    /// <response code="200">Coupon updated and returned.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="404">No coupon with the given ID.</response>
    [HttpPatch("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(int id, [FromBody] PatchCouponRequest req)
    {
        var coupon = await _db.Coupons.FindAsync(id);
        if (coupon is null) return NotFound(new { message = "Coupon not found." });

        if (!string.IsNullOrWhiteSpace(req.CouponName)) coupon.CouponName = req.CouponName!;
        if (!string.IsNullOrWhiteSpace(req.Discount)) coupon.Discount = req.Discount!;
        if (req.CouponType.HasValue) coupon.CouponType = req.CouponType.Value;
        if (req.Value.HasValue) coupon.Value = req.Value.Value;
        if (req.UsageLimit.HasValue) coupon.UsageLimit = req.UsageLimit.Value;

        coupon.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(coupon);
    }

    /// <summary>Delete a coupon by ID. (Admin only)</summary>
    /// <param name="id">Coupon ID.</param>
    /// <response code="200">Coupon deleted.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="404">No coupon with the given ID.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var coupon = await _db.Coupons.FindAsync(id);
        if (coupon is null) return NotFound(new { message = "Coupon not found." });
        _db.Coupons.Remove(coupon);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Coupon deleted." });
    }

    /// <summary>Apply a coupon to the authenticated customer's cart and record usage.</summary>
    /// <remarks>Validates the coupon name and the customer's remaining usage allowance before applying the discount.</remarks>
    /// <response code="200">Discount amount and new cart total returned.</response>
    /// <response code="400">Coupon not applicable or customer/cart not found.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">Coupon name not recognised.</response>
    [HttpPost("apply")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Apply([FromBody] ApplyCouponRequest req)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.CouponName == req.CouponName);
        if (coupon is null) return NotFound(new { message = "Coupon not found." });

        var customerProfile = await _db.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
        if (customerProfile is null)
        {
            customerProfile = new CustomerProfile { UserId = userId };
            _db.CustomerProfiles.Add(customerProfile);
            await _db.SaveChangesAsync();
        }

        var usageCount = await _db.CouponUsages.CountAsync(cu => cu.CouponId == coupon.Id && cu.CustomerProfileId == customerProfile.Id);
        if (coupon.UsageLimit > 0 && usageCount >= coupon.UsageLimit)
            return BadRequest(new { message = "coupon cannot be applicable" });

        var cart = await _db.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart is null) return BadRequest(new { message = "Cart not found for user." });

        decimal discountAmount = coupon.CouponType == CouponType.Percentage
            ? Math.Round(cart.TotalPrice * (coupon.Value / 100m), 2)
            : coupon.Value;

        var newTotal = Math.Max(cart.TotalPrice - discountAmount, 0m);
        cart.TotalPrice = newTotal;
        cart.UpdatedAt = DateTime.UtcNow;

        _db.CouponUsages.Add(new CouponUsage
        {
            CouponId = coupon.Id,
            CustomerProfileId = customerProfile.Id,
            UsedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Ok(new { message = "coupon applied successfull", discount = discountAmount, newTotal = cart.TotalPrice });
    }
}
