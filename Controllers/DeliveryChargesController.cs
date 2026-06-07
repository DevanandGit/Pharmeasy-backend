using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmeasyAPI.Data;
using PharmeasyAPI.DTOs;
using PharmeasyAPI.Helpers;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.Controllers;

/// <summary>Delivery charge rules keyed by postal pincode.</summary>
[ApiController]
[Route("deliverycharges")]
[Produces("application/json")]
public class DeliveryChargesController : ControllerBase
{
    private readonly AppDbContext _db;

    public DeliveryChargesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Create a delivery charge record for a pincode. (Admin only)</summary>
    /// <response code="200">Record created and returned.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="409">A record for this pincode already exists.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateDeliveryChargeRequest req)
    {
        var existing = await _db.DeliveryCharges.FirstOrDefaultAsync(dc => dc.Pincode == req.Pincode);
        if (existing is not null)
            return Conflict(new { message = "A delivery charge record for this pincode already exists." });

        var deliveryCharge = new DeliveryCharge
        {
            Pincode = req.Pincode,
            Charge = req.Charge,
            IsDeliverable = req.IsDeliverable
        };

        _db.DeliveryCharges.Add(deliveryCharge);
        await _db.SaveChangesAsync();
        return Ok(deliveryCharge);
    }

    /// <summary>List all delivery charge records ordered by pincode.</summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Items per page, 1–100 (default 20).</param>
    /// <response code="200">Paginated list of delivery charge records.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        return Ok(await _db.DeliveryCharges.OrderBy(dc => dc.Pincode).PaginateAsync(page, limit));
    }

    /// <summary>Retrieve a delivery charge record by ID.</summary>
    /// <param name="id">Delivery charge record ID.</param>
    /// <response code="200">Record found and returned.</response>
    /// <response code="404">No record with the given ID.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var deliveryCharge = await _db.DeliveryCharges.FindAsync(id);
        if (deliveryCharge is null)
            return NotFound(new { message = "Delivery charge record not found." });
        return Ok(deliveryCharge);
    }

    /// <summary>Check delivery availability and charge for a specific pincode.</summary>
    /// <param name="pincode">The postal pincode to check.</param>
    /// <response code="200">Delivery status and charge (charge is null when pincode is unknown).</response>
    /// <response code="400">Pincode query parameter is missing.</response>
    [HttpGet("check")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckDelivery([FromQuery] string pincode)
    {
        if (string.IsNullOrWhiteSpace(pincode))
            return BadRequest(new { message = "Pincode is required." });

        var deliveryCharge = await _db.DeliveryCharges.FirstOrDefaultAsync(dc => dc.Pincode == pincode);

        if (deliveryCharge is null)
            return Ok(new { exists = false, isDeliverable = false, charge = (decimal?)null });

        return Ok(new { exists = true, isDeliverable = deliveryCharge.IsDeliverable, charge = deliveryCharge.Charge });
    }

    /// <summary>Partially update a delivery charge record. (Admin only)</summary>
    /// <param name="id">Delivery charge record ID.</param>
    /// <response code="200">Record updated and returned.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="404">No record with the given ID.</response>
    /// <response code="409">Another record already uses the new pincode.</response>
    [HttpPatch("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Patch(int id, [FromBody] PatchDeliveryChargeRequest req)
    {
        var deliveryCharge = await _db.DeliveryCharges.FindAsync(id);
        if (deliveryCharge is null)
            return NotFound(new { message = "Delivery charge record not found." });

        if (!string.IsNullOrWhiteSpace(req.Pincode))
        {
            var duplicate = await _db.DeliveryCharges.AnyAsync(dc => dc.Pincode == req.Pincode && dc.Id != id);
            if (duplicate)
                return Conflict(new { message = "Another delivery charge record with this pincode already exists." });
            deliveryCharge.Pincode = req.Pincode;
        }

        if (req.Charge.HasValue) deliveryCharge.Charge = req.Charge.Value;
        if (req.IsDeliverable.HasValue) deliveryCharge.IsDeliverable = req.IsDeliverable.Value;

        deliveryCharge.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(deliveryCharge);
    }

    /// <summary>Delete a delivery charge record by ID. (Admin only)</summary>
    /// <param name="id">Delivery charge record ID.</param>
    /// <response code="200">Record deleted.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="404">No record with the given ID.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var deliveryCharge = await _db.DeliveryCharges.FindAsync(id);
        if (deliveryCharge is null)
            return NotFound(new { message = "Delivery charge record not found." });
        _db.DeliveryCharges.Remove(deliveryCharge);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Delivery charge record deleted." });
    }
}
