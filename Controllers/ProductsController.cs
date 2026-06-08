using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmeasyAPI.Data;
using PharmeasyAPI.DTOs;
using PharmeasyAPI.Helpers;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.Controllers;

/// <summary>CRUD management for pharmacy products.</summary>
[ApiController]
[Route("products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Create a new product. (Admin only)</summary>
    /// <response code="200">Product created and returned with its category.</response>
    /// <response code="400">Referenced category does not exist.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest req)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == req.CategoryId);
        if (!categoryExists)
            return BadRequest(new { message = "Category not found." });

        var product = new Product
        {
            Name = req.Name,
            Price = req.Price,
            DiscountedPrice = req.DiscountedPrice,
            Summary = req.Summary,
            Description = req.Description,
            Uses = req.Uses,
            SideEffects = req.SideEffects,
            DirectionForUse = req.DirectionForUse,
            QuickTips = req.QuickTips,
            StorageDisposal = req.StorageDisposal,
            Dosage = req.Dosage,
            ModeOfAction = req.ModeOfAction,
            Image = req.Image,
            CategoryId = req.CategoryId,
            UserId = userId
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        await _db.Entry(product).Reference(p => p.Category).LoadAsync();

        return Ok(product);
    }

    /// <summary>List all products, with optional keyword search and category filter.</summary>
    /// <param name="search">Optional keyword matched against name, summary, and description.</param>
    /// <param name="categoryId">Optional category ID to filter results.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Items per page, 1–100 (default 20).</param>
    /// <response code="200">Paginated, alphabetically ordered list of matching products.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search, [FromQuery] int? categoryId,
        [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Summary.ToLower().Contains(term) ||
                p.Description.ToLower().Contains(term));
        }

        return Ok(await query.OrderBy(p => p.Name).PaginateAsync(page, limit));
    }

    /// <summary>Retrieve a single product by ID.</summary>
    /// <param name="id">Product ID.</param>
    /// <response code="200">Product found and returned.</response>
    /// <response code="404">No product with the given ID.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound(new { message = "Product not found." });

        return Ok(product);
    }

    /// <summary>Replace all fields of a product. (Admin only)</summary>
    /// <param name="id">Product ID.</param>
    /// <response code="200">Product updated and returned.</response>
    /// <response code="400">Referenced category does not exist.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="404">No product with the given ID.</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest req)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null)
            return NotFound(new { message = "Product not found." });

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == req.CategoryId);
        if (!categoryExists)
            return BadRequest(new { message = "Category not found." });

        product.Name = req.Name;
        product.Price = req.Price;
        product.DiscountedPrice = req.DiscountedPrice;
        product.Summary = req.Summary;
        product.Description = req.Description;
        product.Uses = req.Uses;
        product.SideEffects = req.SideEffects;
        product.DirectionForUse = req.DirectionForUse;
        product.QuickTips = req.QuickTips;
        product.StorageDisposal = req.StorageDisposal;
        product.Dosage = req.Dosage;
        product.ModeOfAction = req.ModeOfAction;
        product.Image = req.Image;
        product.CategoryId = req.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _db.Entry(product).Reference(p => p.Category).LoadAsync();

        return Ok(product);
    }

    /// <summary>Partially update a product (only supplied fields are changed). (Admin only)</summary>
    /// <param name="id">Product ID.</param>
    /// <response code="200">Product updated and returned.</response>
    /// <response code="400">Referenced category does not exist.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="404">No product with the given ID.</response>
    [HttpPatch("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(int id, [FromBody] PatchProductRequest req)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null)
            return NotFound(new { message = "Product not found." });

        if (req.CategoryId.HasValue)
        {
            var categoryExists = await _db.Categories.AnyAsync(c => c.Id == req.CategoryId.Value);
            if (!categoryExists)
                return BadRequest(new { message = "Category not found." });
            product.CategoryId = req.CategoryId.Value;
        }

        if (!string.IsNullOrWhiteSpace(req.Name)) product.Name = req.Name;
        if (req.Price.HasValue) product.Price = req.Price.Value;
        if (req.DiscountedPrice.HasValue) product.DiscountedPrice = req.DiscountedPrice.Value;
        if (!string.IsNullOrWhiteSpace(req.Summary)) product.Summary = req.Summary;
        if (!string.IsNullOrWhiteSpace(req.Description)) product.Description = req.Description;
        if (!string.IsNullOrWhiteSpace(req.Uses)) product.Uses = req.Uses;
        if (!string.IsNullOrWhiteSpace(req.SideEffects)) product.SideEffects = req.SideEffects;
        if (!string.IsNullOrWhiteSpace(req.DirectionForUse)) product.DirectionForUse = req.DirectionForUse;
        if (!string.IsNullOrWhiteSpace(req.QuickTips)) product.QuickTips = req.QuickTips;
        if (!string.IsNullOrWhiteSpace(req.StorageDisposal)) product.StorageDisposal = req.StorageDisposal;
        if (!string.IsNullOrWhiteSpace(req.Dosage)) product.Dosage = req.Dosage;
        if (!string.IsNullOrWhiteSpace(req.ModeOfAction)) product.ModeOfAction = req.ModeOfAction;
        if (req.Image is not null) product.Image = req.Image;

        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _db.Entry(product).Reference(p => p.Category).LoadAsync();

        return Ok(product);
    }

    /// <summary>Delete a product by ID. (Admin only)</summary>
    /// <param name="id">Product ID.</param>
    /// <response code="200">Product deleted.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="404">No product with the given ID.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null)
            return NotFound(new { message = "Product not found." });

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Product deleted." });
    }
}
