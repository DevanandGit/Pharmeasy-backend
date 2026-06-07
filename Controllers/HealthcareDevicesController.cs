using Microsoft.AspNetCore.Mvc;
using PharmeasyAPI.Data;
using PharmeasyAPI.DTOs;
using PharmeasyAPI.Helpers;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.Controllers;

/// <summary>Healthcare device product listings.</summary>
[ApiController]
[Route("HealthcareDevices")]
[Produces("application/json")]
public class HealthcareDevicesController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthcareDevicesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Create a healthcare device entry.</summary>
    /// <response code="201">Entry created and returned.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryItemRequest req)
    {
        var device = new HealthcareDevice { Image = req.Image, Title = req.Title, Price = req.Price };
        _db.HealthcareDevices.Add(device);
        await _db.SaveChangesAsync();
        return StatusCode(201, device);
    }

    /// <summary>List all healthcare device entries.</summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Items per page, 1–100 (default 20).</param>
    /// <response code="200">Paginated list of entries.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        return Ok(await _db.HealthcareDevices.PaginateAsync(page, limit));
    }

    /// <summary>Delete a healthcare device entry by ID.</summary>
    /// <param name="id">Entry ID.</param>
    /// <response code="200">Entry deleted and returned.</response>
    /// <response code="404">No entry with the given ID.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var device = await _db.HealthcareDevices.FindAsync(id);
        if (device is null)
            return NotFound(new { message = "Not found" });
        _db.HealthcareDevices.Remove(device);
        await _db.SaveChangesAsync();
        return Ok(device);
    }
}
