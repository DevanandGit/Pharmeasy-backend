using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmeasyAPI.Data;
using PharmeasyAPI.DTOs;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.Controllers;

/// <summary>Initiate a doctor-appointment booking and collect payment via Razorpay.</summary>
[ApiController]
[Route("checkout")]
[Produces("application/json")]
public class BookingCheckoutController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public BookingCheckoutController(AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    /// <summary>Initiate a doctor-appointment booking and create a Razorpay payment link.</summary>
    /// <remarks>
    /// **Flow:**
    /// 1. Validates the doctor exists and is active.
    /// 2. Parses the time slot string (format: <c>HH:mm-HH:mm</c>, e.g. <c>09:00-09:30</c>).
    /// 3. Creates a pending <c>BookingSession</c> with a JSON snapshot of the booking data.
    /// 4. Calls the Razorpay API to generate a payment link for the consultation fee.
    /// 5. Returns all booking details plus the Razorpay <c>paymentLinkUrl</c>.
    ///
    /// The actual <c>Booking</c> record is only created after Razorpay fires the
    /// <c>payment_link.paid</c> webhook to <c>POST /checkout/razorpay/webhook</c>.
    /// </remarks>
    /// <response code="200">Booking session created; returns booking details and Razorpay payment URL.</response>
    /// <response code="400">Invalid time slot format, or customer profile not found.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="404">Doctor not found or inactive.</response>
    /// <response code="500">Razorpay payment link creation failed.</response>
    [HttpPost("booking")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckoutBooking([FromBody] BookingCheckoutRequest request)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Unauthorized();

        var customerProfile = await _db.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
        if (customerProfile is null)
            return BadRequest(new { message = "Customer profile not found. Please complete your profile." });

        var doctor = await _db.DoctorProfiles
            .Include(dp => dp.User)
            .FirstOrDefaultAsync(dp => dp.Id == request.DoctorProfileId && dp.IsActive);
        if (doctor is null)
            return NotFound(new { message = "Doctor not found." });

        var (startTime, endTime, slotError) = ParseTimeSlot(request.TimeSlot);
        if (slotError is not null)
            return BadRequest(new { message = slotError });

        var snapshot = new BookingSnapshotDto
        {
            DoctorProfileId = doctor.Id,
            CustomerProfileId = customerProfile.Id,
            TimeSlot = request.TimeSlot,
            AppointmentDate = request.AppointmentDate.Date,
            PatientName = request.PatientName,
            PatientNumber = request.PatientNumber,
            Age = request.Age,
            Gender = request.Gender.ToString(),
            PrescriptionUpload = request.PrescriptionUpload,
            Description = request.Description,
            ModeOfConsult = request.ModeOfConsult.ToString()
        };

        var session = new BookingSession
        {
            UserId = userId,
            CustomerProfileId = customerProfile.Id,
            DoctorProfileId = doctor.Id,
            BookingSnapshot = JsonSerializer.Serialize(snapshot),
            ConsultationFee = doctor.ConsultationFee,
            Status = "Created"
        };

        _db.BookingSessions.Add(session);
        await _db.SaveChangesAsync();

        var razorpayResponse = await CreateRazorpayPaymentLink(session, user, doctor);
        if (razorpayResponse is null)
            return StatusCode(500, new { message = "Unable to create payment link." });

        session.PaymentLinkId = razorpayResponse.LinkId;
        session.PaymentLinkUrl = razorpayResponse.ShortUrl;
        session.Status = "LinkCreated";
        session.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new BookingCheckoutResponse
        {
            BookingSessionId = session.Id,
            DoctorProfileId = doctor.Id,
            DoctorName = doctor.User.Name ?? string.Empty,
            TimeSlot = request.TimeSlot,
            AppointmentDate = DateOnly.FromDateTime(request.AppointmentDate),
            PatientName = request.PatientName,
            PatientNumber = request.PatientNumber,
            Age = request.Age,
            Gender = request.Gender.ToString(),
            PrescriptionUpload = request.PrescriptionUpload,
            Description = request.Description,
            ModeOfConsult = request.ModeOfConsult.ToString(),
            ConsultationFee = doctor.ConsultationFee,
            PaymentLinkUrl = razorpayResponse.ShortUrl,
            PaymentLinkId = razorpayResponse.LinkId,
            Message = "Booking initiated. Complete payment via the link to confirm your appointment."
        });
    }

    private async Task<RazorpayLinkResponse?> CreateRazorpayPaymentLink(BookingSession session, User user, DoctorProfile doctor)
    {
        var keyId = _configuration["Razorpay:KeyId"];
        var keySecret = _configuration["Razorpay:KeySecret"];
        var currency = _configuration["Razorpay:Currency"] ?? "INR";
        if (string.IsNullOrWhiteSpace(keyId) || string.IsNullOrWhiteSpace(keySecret))
            return null;

        var client = _httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var requestPayload = new Dictionary<string, object>
        {
            ["amount"] = (int)Math.Round(session.ConsultationFee * 100m),
            ["currency"] = currency,
            ["reference_id"] = $"booking-{session.Id}",
            ["description"] = $"Consultation with {doctor.User.Name ?? "Doctor"} ({doctor.Specialisation})",
            ["customer"] = new Dictionary<string, object>
            {
                ["name"] = user.Name ?? user.Email,
                ["email"] = user.Email,
                ["contact"] = user.Phone ?? string.Empty
            },
            ["notify"] = new Dictionary<string, object>
            {
                ["sms"] = false,
                ["email"] = false
            }
        };

        var callbackUrl = _configuration["Razorpay:BookingCallbackUrl"] ?? _configuration["Razorpay:CallbackUrl"];
        if (!string.IsNullOrWhiteSpace(callbackUrl))
        {
            requestPayload["callback_url"] = callbackUrl;
            requestPayload["callback_method"] = "get";
        }

        var json = JsonSerializer.Serialize(requestPayload);
        var response = await client.PostAsync("https://api.razorpay.com/v1/payment_links",
            new StringContent(json, Encoding.UTF8, "application/json"));
        if (!response.IsSuccessStatusCode) return null;

        var responseBody = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(responseBody);
        var entity = document.RootElement;
        return new RazorpayLinkResponse
        {
            LinkId = entity.GetProperty("id").GetString() ?? string.Empty,
            ShortUrl = entity.GetProperty("short_url").GetString() ?? string.Empty
        };
    }

    private static (TimeSpan start, TimeSpan end, string? error) ParseTimeSlot(string slot)
    {
        var parts = slot.Split('-');
        if (parts.Length != 2)
            return (default, default, "TimeSlot must be in format 'HH:mm-HH:mm' (e.g. '09:00-09:30').");
        if (!TimeSpan.TryParse(parts[0].Trim(), out var start))
            return (default, default, "Invalid start time in TimeSlot.");
        if (!TimeSpan.TryParse(parts[1].Trim(), out var end))
            return (default, default, "Invalid end time in TimeSlot.");
        if (end <= start)
            return (default, default, "End time must be after start time in TimeSlot.");
        return (start, end, null);
    }

    private sealed class RazorpayLinkResponse
    {
        public string LinkId { get; set; } = string.Empty;
        public string ShortUrl { get; set; } = string.Empty;
    }
}
