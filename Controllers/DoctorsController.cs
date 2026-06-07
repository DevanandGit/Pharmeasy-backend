using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmeasyAPI.Data;
using PharmeasyAPI.DTOs;
using PharmeasyAPI.Helpers;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.Controllers;

/// <summary>Doctor profile management and customer-facing doctor discovery.</summary>
[ApiController]
[Route("doctors")]
[Produces("application/json")]
public class DoctorsController : ControllerBase
{
    private readonly AppDbContext _db;

    public DoctorsController(AppDbContext db)
    {
        _db = db;
    }

    // ── Admin endpoints ───────────────────────────────────────────────────────

    /// <summary>Create a new doctor account and profile. (Admin only)</summary>
    /// <remarks>Creates a User with role Doctor and a linked DoctorProfile in a single call.</remarks>
    /// <response code="200">Doctor created; returns the profile DTO.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="409">A user with this email already exists.</response>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorRequest req)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return Conflict(new { message = "A user with this email already exists." });

        var user = new User
        {
            Email = req.Email,
            Name = req.Name,
            Phone = req.Phone,
            Role = UserRole.Doctor,
            IsNew = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var doctorProfile = new DoctorProfile
        {
            UserId = user.Id,
            Image = req.Image,
            Qualification = req.Qualification,
            Specialisation = req.Specialisation,
            Experience = req.Experience,
            ConsultationFee = req.ConsultationFee,
            IsActive = true
        };

        _db.DoctorProfiles.Add(doctorProfile);
        await _db.SaveChangesAsync();

        return Ok(MapDoctor(doctorProfile, user));
    }

    /// <summary>Partially update a doctor profile. (Admin only)</summary>
    /// <param name="id">DoctorProfile ID.</param>
    /// <response code="200">Profile updated and returned.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="404">No doctor profile with the given ID.</response>
    /// <response code="409">Another user already uses the supplied email.</response>
    [HttpPatch("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PatchDoctor(int id, [FromBody] PatchDoctorRequest req)
    {
        var profile = await _db.DoctorProfiles
            .Include(dp => dp.User)
            .FirstOrDefaultAsync(dp => dp.Id == id);

        if (profile is null)
            return NotFound(new { message = "Doctor profile not found." });

        if (!string.IsNullOrWhiteSpace(req.Email) && req.Email != profile.User.Email)
        {
            if (await _db.Users.AnyAsync(u => u.Email == req.Email && u.Id != profile.UserId))
                return Conflict(new { message = "Another user already uses this email." });
            profile.User.Email = req.Email;
        }

        if (!string.IsNullOrWhiteSpace(req.Name)) profile.User.Name = req.Name;
        if (!string.IsNullOrWhiteSpace(req.Phone)) profile.User.Phone = req.Phone;
        if (!string.IsNullOrWhiteSpace(req.Image)) profile.Image = req.Image;
        if (!string.IsNullOrWhiteSpace(req.Qualification)) profile.Qualification = req.Qualification;
        if (!string.IsNullOrWhiteSpace(req.Specialisation)) profile.Specialisation = req.Specialisation;
        if (req.Experience.HasValue) profile.Experience = req.Experience.Value;
        if (req.ConsultationFee.HasValue) profile.ConsultationFee = req.ConsultationFee.Value;
        if (req.IsActive.HasValue) profile.IsActive = req.IsActive.Value;

        profile.UpdatedAt = DateTime.UtcNow;
        profile.User.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(MapDoctor(profile, profile.User));
    }

    /// <summary>Delete a doctor profile. (Admin only)</summary>
    /// <remarks>The underlying user is demoted to Customer role to preserve booking history.</remarks>
    /// <param name="id">DoctorProfile ID.</param>
    /// <response code="200">Doctor profile removed.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="404">No doctor profile with the given ID.</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDoctor(int id)
    {
        var profile = await _db.DoctorProfiles
            .Include(dp => dp.User)
            .FirstOrDefaultAsync(dp => dp.Id == id);

        if (profile is null)
            return NotFound(new { message = "Doctor profile not found." });

        profile.User.Role = UserRole.Customer;
        _db.DoctorProfiles.Remove(profile);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Doctor removed." });
    }

    /// <summary>Retrieve a single doctor profile by ID. (Admin only)</summary>
    /// <param name="id">DoctorProfile ID.</param>
    /// <response code="200">Profile found and returned.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    /// <response code="404">No doctor profile with the given ID.</response>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDoctorById(int id)
    {
        var profile = await _db.DoctorProfiles
            .Include(dp => dp.User)
            .FirstOrDefaultAsync(dp => dp.Id == id);

        if (profile is null)
            return NotFound(new { message = "Doctor profile not found." });

        return Ok(MapDoctor(profile, profile.User));
    }

    /// <summary>List all doctor profiles. (Admin only)</summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Items per page, 1–100 (default 20).</param>
    /// <response code="200">Paginated list of all profiles, newest first.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="403">Caller is not an Admin.</response>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllDoctors([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var paged = await _db.DoctorProfiles
            .Include(dp => dp.User)
            .OrderByDescending(dp => dp.CreatedAt)
            .PaginateAsync(page, limit);

        return Ok(paged.MapData(dp => MapDoctor(dp, dp.User)));
    }

    // ── Customer-facing endpoints ─────────────────────────────────────────────

    /// <summary>List all active doctors (summary view for customers). No authentication required.</summary>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="limit">Items per page, 1–100 (default 20).</param>
    /// <response code="200">Paginated, alphabetically ordered list of active doctors with consultation fee.</response>
    [HttpGet("customer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDoctorsForCustomer([FromQuery] int page = 1, [FromQuery] int limit = 20)
    {
        var paged = await _db.DoctorProfiles
            .Include(dp => dp.User)
            .Where(dp => dp.IsActive)
            .OrderBy(dp => dp.User.Name)
            .PaginateAsync(page, limit);

        return Ok(paged.MapData(dp => new DoctorCustomerSummaryDto
        {
            DoctorProfileId = dp.Id,
            Name = dp.User.Name ?? string.Empty,
            Image = dp.Image,
            Qualification = dp.Qualification,
            Specialisation = dp.Specialisation,
            Experience = dp.Experience,
            ConsultationFee = dp.ConsultationFee
        }));
    }

    /// <summary>Get a doctor's full profile with per-weekday available 30-minute slots. No authentication required.</summary>
    /// <remarks>
    /// Available slots are computed by subtracting the doctor's unavailability blocks from the full 24-hour day,
    /// then splitting each resulting window into 30-minute intervals (e.g. <c>09:00–09:30</c>, <c>09:30–10:00</c>).
    /// </remarks>
    /// <param name="id">DoctorProfile ID.</param>
    /// <response code="200">Doctor detail with weekly availability slots.</response>
    /// <response code="404">No active doctor with the given ID.</response>
    [HttpGet("customer/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDoctorForCustomer(int id)
    {
        var profile = await _db.DoctorProfiles
            .Include(dp => dp.User)
            .Include(dp => dp.Unavailabilities)
            .FirstOrDefaultAsync(dp => dp.Id == id && dp.IsActive);

        if (profile is null)
            return NotFound(new { message = "Doctor not found." });

        var detail = new DoctorCustomerDetailDto
        {
            DoctorProfileId = profile.Id,
            Name = profile.User.Name ?? string.Empty,
            Image = profile.Image,
            Qualification = profile.Qualification,
            Specialisation = profile.Specialisation,
            Experience = profile.Experience,
            ConsultationFee = profile.ConsultationFee,
            WeeklyAvailability = ComputeWeeklyAvailability(profile.Unavailabilities)
        };

        return Ok(detail);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<DoctorWeekdayAvailabilityDto> ComputeWeeklyAvailability(
        ICollection<DoctorUnAvailability> unavailabilities)
    {
        return Enum.GetValues<Weekday>().Select(day =>
        {
            var blocks = unavailabilities
                .Where(u => u.Weekday == day)
                .OrderBy(u => u.StartTime)
                .Select(u => (u.StartTime, u.EndTime))
                .ToList();

            return new DoctorWeekdayAvailabilityDto
            {
                Weekday = day.ToString(),
                AvailableSlots = ComputeAvailableSlots(blocks)
            };
        }).ToList();
    }

    private static List<DoctorSlotDto> ComputeAvailableSlots(
        List<(TimeSpan Start, TimeSpan End)> unavailabilities)
    {
        var slots = new List<DoctorSlotDto>();
        var dayEnd = TimeSpan.FromHours(24);
        var duration = TimeSpan.FromMinutes(30);
        var cursor = TimeSpan.Zero;

        foreach (var (uStart, uEnd) in unavailabilities)
        {
            if (uStart > cursor)
                GenerateSlots(slots, cursor, uStart, duration);
            if (uEnd > cursor)
                cursor = uEnd;
        }

        if (cursor < dayEnd)
            GenerateSlots(slots, cursor, dayEnd, duration);

        return slots;
    }

    private static void GenerateSlots(List<DoctorSlotDto> slots, TimeSpan from, TimeSpan to, TimeSpan duration)
    {
        var start = from;
        while (start + duration <= to)
        {
            slots.Add(new DoctorSlotDto
            {
                Start = FormatTime(start),
                End = FormatTime(start + duration)
            });
            start += duration;
        }
    }

    private static string FormatTime(TimeSpan ts) =>
        $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}";

    private static DoctorResponseDto MapDoctor(DoctorProfile profile, User user) => new()
    {
        DoctorProfileId = profile.Id,
        UserId = user.Id,
        Email = user.Email,
        Name = user.Name,
        Phone = user.Phone,
        IsActive = profile.IsActive,
        Image = profile.Image,
        Qualification = profile.Qualification,
        Specialisation = profile.Specialisation,
        Experience = profile.Experience,
        ConsultationFee = profile.ConsultationFee,
        CreatedAt = profile.CreatedAt,
        UpdatedAt = profile.UpdatedAt
    };
}
