using Microsoft.AspNetCore.Mvc;
using PharmeasyAPI.Data;
using PharmeasyAPI.DTOs;
using PharmeasyAPI.Helpers;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.Controllers;

/// <summary>Health food and drinks product listings.</summary>
[ApiController]
[Route("HealthFoodandDrinks")]
[Produces("application/json")]
public class HealthFoodAndDrinksController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthFoodAndDrinksController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Create a health food or drink entry.</summary>
    /// <response code="201">Entry created and returned.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryItemRequest req)
    {
        var item = new HealthFoodDrink { Image = req.Image, Title = req.Title, Price = req.Price };
        _db.HealthFoodDrinks.Add(item);
        await _db.SaveChangesAsync();
        return StatusCode(201, item);
    }

    /// <summary>List all health food and drink entries.</summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Items per page, 1–100 (default 20).</param>
    /// <response code="200">Paginated list of entries.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        return Ok(await _db.HealthFoodDrinks.PaginateAsync(page, limit));
    }

    /// <summary>Delete a health food or drink entry by ID.</summary>
    /// <param name="id">Entry ID.</param>
    /// <response code="200">Entry deleted and returned.</response>
    /// <response code="404">No entry with the given ID.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.HealthFoodDrinks.FindAsync(id);
        if (item is null)
            return NotFound(new { message = "Not found" });
        _db.HealthFoodDrinks.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(item);
    }
}
