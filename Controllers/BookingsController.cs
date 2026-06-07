using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmeasyAPI.Data;
using PharmeasyAPI.DTOs;
using PharmeasyAPI.Helpers;

namespace PharmeasyAPI.Controllers;

/// <summary>View confirmed appointment bookings.</summary>
[ApiController]
[Route("bookings")]
[Authorize]
[Produces("application/json")]
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public BookingsController(AppDbContext db) => _db = db;

    /// <summary>List the authenticated customer's bookings, newest appointment first.</summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Items per page, 1–100 (default 20).</param>
    /// <response code="200">Paginated list of the customer's bookings.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not a Customer.</response>
    /// <response code="404">Customer profile not found.</response>
    [HttpGet("customer")]
    [Authorize(Roles = "Customer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerBookings(
        [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var customerProfile = await _db.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
        if (customerProfile is null)
            return NotFound(new { message = "Customer profile not found." });

        var query = _db.Bookings
            .Where(b => b.CustomerProfileId == customerProfile.Id)
            .Include(b => b.DoctorProfile).ThenInclude(dp => dp.User)
            .Include(b => b.CustomerProfile).ThenInclude(cp => cp.User)
            .OrderByDescending(b => b.AppointmentDate);

        var paged = await query.PaginateAsync(page, limit);
        return Ok(paged.MapData(MapBooking));
    }

    /// <summary>List the authenticated doctor's bookings, newest appointment first.</summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Items per page, 1–100 (default 20).</param>
    /// <response code="200">Paginated list of the doctor's bookings.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not a Doctor.</response>
    /// <response code="404">Doctor profile not found.</response>
    [HttpGet("doctor")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDoctorBookings(
        [FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var doctorProfile = await _db.DoctorProfiles.FirstOrDefaultAsync(dp => dp.UserId == userId);
        if (doctorProfile is null)
            return NotFound(new { message = "Doctor profile not found." });

        var query = _db.Bookings
            .Where(b => b.DoctorProfileId == doctorProfile.Id)
            .Include(b => b.DoctorProfile).ThenInclude(dp => dp.User)
            .Include(b => b.CustomerProfile).ThenInclude(cp => cp.User)
            .OrderByDescending(b => b.AppointmentDate);

        var paged = await query.PaginateAsync(page, limit);
        return Ok(paged.MapData(MapBooking));
    }

    private static BookingResponseDto MapBooking(PharmeasyAPI.Models.Booking b) => new()
    {
        Id = b.Id,
        AppointmentDate = DateOnly.FromDateTime(b.AppointmentDate),
        TimeSlot = b.TimeSlot,
        PatientName = b.PatientName,
        PatientNumber = b.PatientNumber,
        Age = b.Age,
        Gender = b.Gender,
        ModeOfConsult = b.ModeOfConsult,
        Description = b.Description,
        PrescriptionUpload = b.PrescriptionUpload,
        Notes = b.Notes,
        DoctorProfileId = b.DoctorProfileId,
        DoctorName = b.DoctorProfile.User.Name ?? string.Empty,
        CustomerProfileId = b.CustomerProfileId,
        CustomerName = b.CustomerProfile.User.Name ?? string.Empty,
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt
    };
}
