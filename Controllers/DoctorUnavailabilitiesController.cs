using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmeasyAPI.Data;
using PharmeasyAPI.DTOs;
using PharmeasyAPI.Helpers;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.Controllers;

/// <summary>Manage doctor unavailability blocks used to compute available appointment slots.</summary>
[ApiController]
[Route("doctor-unavailabilities")]
[Produces("application/json")]
public class DoctorUnavailabilitiesController : ControllerBase
{
    private readonly AppDbContext _db;

    public DoctorUnavailabilitiesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Add an unavailability block for the authenticated doctor's own schedule.</summary>
    /// <remarks>Time format: <c>HH:mm-HH:mm</c> (e.g. <c>18:00-21:00</c>). End time must be after start time.</remarks>
    /// <response code="200">Unavailability block created and returned.</response>
    /// <response code="400">Invalid time format or end time not after start time.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not a Doctor.</response>
    /// <response code="404">No doctor profile linked to the authenticated user.</response>
    [HttpPost("doctor")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateForDoctor([FromBody] CreateDoctorUnavailabilityRequest request)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var profile = await _db.DoctorProfiles.FirstOrDefaultAsync(dp => dp.UserId == userId);
        if (profile is null)
            return NotFound(new { message = "Doctor profile not found." });

        var (startTime, endTime, error) = ParseTime(request.Time);
        if (error is not null)
            return BadRequest(new { message = error });

        var entity = new DoctorUnAvailability
        {
            DoctorProfileId = profile.Id,
            Weekday = request.Weekday,
            StartTime = startTime,
            EndTime = endTime
        };

        _db.DoctorUnAvailabilities.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(MapResponse(entity));
    }

    /// <summary>Add an unavailability block for any doctor by profile ID. (Admin only)</summary>
    /// <remarks>Time format: <c>HH:mm-HH:mm</c> (e.g. <c>18:00-21:00</c>).</remarks>
    /// <response code="200">Unavailability block created and returned.</response>
    /// <response code="400">Invalid time format.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="404">No doctor profile with the given ID.</response>
    [HttpPost("admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateForDoctorAdmin([FromBody] CreateDoctorUnavailabilityAdminRequest request)
    {
        var profile = await _db.DoctorProfiles
            .Include(dp => dp.User)
            .FirstOrDefaultAsync(dp => dp.Id == request.DoctorProfileId);
        if (profile is null)
            return NotFound(new { message = "Doctor profile not found." });

        var (startTime, endTime, error) = ParseTime(request.Time);
        if (error is not null)
            return BadRequest(new { message = error });

        var entity = new DoctorUnAvailability
        {
            DoctorProfileId = profile.Id,
            Weekday = request.Weekday,
            StartTime = startTime,
            EndTime = endTime
        };

        _db.DoctorUnAvailabilities.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(MapResponse(entity));
    }

    /// <summary>List all unavailability blocks for the authenticated doctor.</summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Items per page, 1–100 (default 20).</param>
    /// <response code="200">Paginated unavailability blocks ordered by weekday and start time.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not a Doctor.</response>
    /// <response code="404">No doctor profile linked to the authenticated user.</response>
    [HttpGet("doctor")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDoctorUnavailabilities([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var profile = await _db.DoctorProfiles.FirstOrDefaultAsync(dp => dp.UserId == userId);
        if (profile is null)
            return NotFound(new { message = "Doctor profile not found." });

        var paged = await _db.DoctorUnAvailabilities
            .Where(d => d.DoctorProfileId == profile.Id)
            .Include(d => d.DoctorProfile).ThenInclude(dp => dp.User)
            .OrderBy(d => d.Weekday).ThenBy(d => d.StartTime)
            .PaginateAsync(page, limit);

        return Ok(paged.MapData(MapResponse));
    }

    /// <summary>List all unavailability blocks for a specific doctor by profile ID. (Admin only)</summary>
    /// <param name="doctorProfileId">DoctorProfile ID.</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Items per page, 1–100 (default 20).</param>
    /// <response code="200">Paginated unavailability blocks ordered by weekday and start time.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    [HttpGet("admin/{doctorProfileId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDoctorUnavailabilitiesAdmin(
        int doctorProfileId, [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var paged = await _db.DoctorUnAvailabilities
            .Where(d => d.DoctorProfileId == doctorProfileId)
            .Include(d => d.DoctorProfile).ThenInclude(dp => dp.User)
            .OrderBy(d => d.Weekday).ThenBy(d => d.StartTime)
            .PaginateAsync(page, limit);

        return Ok(paged.MapData(MapResponse));
    }

    /// <summary>Retrieve a single unavailability block by ID. Accessible by the owning doctor or an Admin.</summary>
    /// <param name="id">Unavailability block ID.</param>
    /// <response code="200">Block found and returned.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Block does not belong to the authenticated doctor.</response>
    /// <response code="404">No block with the given ID.</response>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var entity = await _db.DoctorUnAvailabilities
            .Include(d => d.DoctorProfile).ThenInclude(dp => dp.User)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (entity is null)
            return NotFound(new { message = "Unavailability not found." });

        if (!await CanAccess(entity))
            return Forbid();

        return Ok(MapResponse(entity));
    }

    /// <summary>Partially update an unavailability block. Accessible by the owning doctor or an Admin.</summary>
    /// <param name="id">Unavailability block ID.</param>
    /// <response code="200">Block updated and returned.</response>
    /// <response code="400">Invalid time format.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Block does not belong to the authenticated doctor.</response>
    /// <response code="404">No block with the given ID.</response>
    [HttpPatch("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Patch(int id, [FromBody] PatchDoctorUnavailabilityRequest request)
    {
        var entity = await _db.DoctorUnAvailabilities
            .Include(d => d.DoctorProfile).ThenInclude(dp => dp.User)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (entity is null)
            return NotFound(new { message = "Unavailability not found." });

        if (!await CanAccess(entity))
            return Forbid();

        if (request.Weekday.HasValue)
            entity.Weekday = request.Weekday.Value;

        if (!string.IsNullOrWhiteSpace(request.Time))
        {
            var (startTime, endTime, error) = ParseTime(request.Time);
            if (error is not null)
                return BadRequest(new { message = error });
            entity.StartTime = startTime;
            entity.EndTime = endTime;
        }

        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(MapResponse(entity));
    }

    /// <summary>Delete an unavailability block. Accessible by the owning doctor or an Admin.</summary>
    /// <param name="id">Unavailability block ID.</param>
    /// <response code="200">Block deleted.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Block does not belong to the authenticated doctor.</response>
    /// <response code="404">No block with the given ID.</response>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var entity = await _db.DoctorUnAvailabilities
            .Include(d => d.DoctorProfile).ThenInclude(dp => dp.User)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (entity is null)
            return NotFound(new { message = "Unavailability not found." });

        if (!await CanAccess(entity))
            return Forbid();

        _db.DoctorUnAvailabilities.Remove(entity);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Unavailability deleted." });
    }

    private async Task<bool> CanAccess(DoctorUnAvailability entity)
    {
        if (User.IsInRole("Admin")) return true;
        if (!User.IsInRole("Doctor")) return false;

        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return false;

        var profile = await _db.DoctorProfiles.FirstOrDefaultAsync(dp => dp.UserId == userId);
        return profile is not null && profile.Id == entity.DoctorProfileId;
    }

    private static (TimeSpan startTime, TimeSpan endTime, string? error) ParseTime(string time)
    {
        var parts = time.Split('-');
        if (parts.Length != 2)
            return (default, default, "Time must be in format 'HH:mm-HH:mm'.");

        if (!TimeSpan.TryParse(parts[0], out var startTime))
            return (default, default, "Invalid start time.");

        if (!TimeSpan.TryParse(parts[1], out var endTime))
            return (default, default, "Invalid end time.");

        if (endTime <= startTime)
            return (default, default, "End time must be after start time.");

        return (startTime, endTime, null);
    }

    private DoctorUnavailabilityResponseDto MapResponse(DoctorUnAvailability entity)
    {
        var user = entity.DoctorProfile.User;
        return new DoctorUnavailabilityResponseDto
        {
            Id = entity.Id,
            DoctorProfileId = entity.DoctorProfileId,
            DoctorName = user.Name ?? string.Empty,
            DoctorEmail = user.Email,
            Weekday = entity.Weekday.ToString(),
            Time = $"{entity.StartTime:hh\\:mm}-{entity.EndTime:hh\\:mm}",
            StartTime = entity.StartTime.ToString(@"hh\:mm"),
            EndTime = entity.EndTime.ToString(@"hh\:mm"),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}
