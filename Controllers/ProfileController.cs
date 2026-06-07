using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmeasyAPI.Data;
using PharmeasyAPI.DTOs;

namespace PharmeasyAPI.Controllers;

/// <summary>Manages the authenticated user's own profile.</summary>
[ApiController]
[Route("profile")]
[Authorize]
[Produces("application/json")]
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProfileController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Get the authenticated user's profile, including their Customer or Doctor sub-profile.</summary>
    /// <remarks>Pass the JWT in the <c>Authorization: Bearer &lt;token&gt;</c> header.</remarks>
    /// <response code="200">Returns user info and role-specific profile data.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Unauthorized();

        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            IsNew = user.IsNew,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        CustomerProfileDto? customerProfileDto = null;
        DoctorProfileSummaryDto? doctorProfileDto = null;

        if (user.Role == PharmeasyAPI.Models.UserRole.Customer)
        {
            var cp = await _db.CustomerProfiles
                .Include(c => c.Bookings)
                    .ThenInclude(b => b.DoctorProfile).ThenInclude(dp => dp.User)
                .Include(c => c.Orders)
                    .ThenInclude(o => o.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cp is not null)
                customerProfileDto = new CustomerProfileDto
                {
                    CustomerProfileId = cp.Id,
                    ProfileImage = cp.ProfileImage,
                    Bookings = cp.Bookings
                        .OrderByDescending(b => b.AppointmentDate)
                        .Select(b => new ProfileBookingDto
                        {
                            Id = b.Id,
                            AppointmentDate = b.AppointmentDate,
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
                            PatientProfileName = user.Name ?? string.Empty,
                            CreatedAt = b.CreatedAt
                        }).ToList(),
                    Orders = cp.Orders
                        .OrderByDescending(o => o.CreatedAt)
                        .Select(o => new ProfileOrderDto
                        {
                            Id = o.Id,
                            ProductId = o.ProductId,
                            ProductName = o.Product.Name,
                            Quantity = o.Quantity,
                            PurchasePrice = o.PurchasePrice,
                            TotalPrice = o.PurchasePrice * o.Quantity,
                            CreatedAt = o.CreatedAt
                        }).ToList(),
                    CreatedAt = cp.CreatedAt,
                    UpdatedAt = cp.UpdatedAt
                };
        }
        else if (user.Role == PharmeasyAPI.Models.UserRole.Doctor)
        {
            var dp = await _db.DoctorProfiles
                .Include(d => d.Bookings)
                    .ThenInclude(b => b.CustomerProfile).ThenInclude(cp => cp.User)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (dp is not null)
                doctorProfileDto = new DoctorProfileSummaryDto
                {
                    DoctorProfileId = dp.Id,
                    Image = dp.Image,
                    Qualification = dp.Qualification,
                    Specialisation = dp.Specialisation,
                    Experience = dp.Experience,
                    ConsultationFee = dp.ConsultationFee,
                    IsActive = dp.IsActive,
                    Bookings = dp.Bookings
                        .OrderByDescending(b => b.AppointmentDate)
                        .Select(b => new ProfileBookingDto
                        {
                            Id = b.Id,
                            AppointmentDate = b.AppointmentDate,
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
                            DoctorName = user.Name ?? string.Empty,
                            CustomerProfileId = b.CustomerProfileId,
                            PatientProfileName = b.CustomerProfile.User.Name ?? string.Empty,
                            CreatedAt = b.CreatedAt
                        }).ToList(),
                    CreatedAt = dp.CreatedAt,
                    UpdatedAt = dp.UpdatedAt
                };
        }

        return Ok(new ProfileResponse
        {
            User = userDto,
            CustomerProfile = customerProfileDto,
            DoctorProfile = doctorProfileDto
        });
    }

    /// <summary>Complete the profile of the currently logged-in user.</summary>
    /// <remarks>
    /// Sets the user's name and phone number and marks the account as no longer new.
    /// Should be called once after the first login.
    /// </remarks>
    /// <response code="200">Profile updated; returns the full user object.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    [HttpPost("complete-profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CompleteProfile([FromBody] CompleteProfileRequest req)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Unauthorized();

        user.Name = req.Name;
        user.Phone = req.Phone;
        user.IsNew = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            IsNew = user.IsNew,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        });
    }
}
