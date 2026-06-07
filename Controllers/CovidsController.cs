using Microsoft.AspNetCore.Mvc;
using PharmeasyAPI.Data;
using PharmeasyAPI.DTOs;
using PharmeasyAPI.Helpers;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.Controllers;

/// <summary>COVID-related product/test listings.</summary>
[ApiController]
[Route("covids")]
[Produces("application/json")]
public class CovidsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CovidsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Create a COVID product/test entry.</summary>
    /// <response code="201">Entry created and returned.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryItemRequest req)
    {
        var covid = new Covid { Image = req.Image, Title = req.Title, Price = req.Price };
        _db.Covids.Add(covid);
        await _db.SaveChangesAsync();
        return StatusCode(201, covid);
    }

    /// <summary>List all COVID product/test entries.</summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Items per page, 1–100 (default 20).</param>
    /// <response code="200">Paginated list of COVID entries.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        return Ok(await _db.Covids.PaginateAsync(page, limit));
    }

    /// <summary>Delete a COVID entry by ID.</summary>
    /// <param name="id">Entry ID.</param>
    /// <response code="200">Entry deleted and returned.</response>
    /// <response code="404">No entry with the given ID.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var covid = await _db.Covids.FindAsync(id);
        if (covid is null)
            return NotFound(new { message = "Not found" });
        _db.Covids.Remove(covid);
        await _db.SaveChangesAsync();
        return Ok(covid);
    }
}
