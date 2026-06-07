using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmeasyAPI.Data;
using PharmeasyAPI.DTOs;
using PharmeasyAPI.Models;

namespace PharmeasyAPI.Controllers;

/// <summary>Product cart checkout and Razorpay webhook handling.</summary>
[ApiController]
[Route("checkout")]
[Produces("application/json")]
public class ProductCheckoutController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public ProductCheckoutController(AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    /// <summary>Initiate checkout for the authenticated customer's cart and generate a Razorpay payment link.</summary>
    /// <remarks>
    /// **Flow:**
    /// 1. Resolves the cart and optionally validates/applies a coupon.
    /// 2. Creates a <c>CheckoutSession</c> with a snapshot of the cart items.
    /// 3. Calls the Razorpay API and returns the payment link URL.
    ///
    /// Orders are only created after Razorpay fires the <c>payment_link.paid</c> webhook.
    /// </remarks>
    /// <response code="200">Checkout initiated; returns cart summary and Razorpay payment URL.</response>
    /// <response code="400">Cart is empty, coupon is invalid, or customer profile missing.</response>
    /// <response code="401">Missing or invalid JWT.</response>
    /// <response code="500">Razorpay payment link creation failed.</response>
    [HttpPost("products")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckoutProducts([FromBody] ProductCheckoutRequest request)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return Unauthorized();

        var cart = await _db.Carts
            .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null || !cart.CartItems.Any())
            return BadRequest(new { message = "Cart is empty or not found." });

        var products = cart.CartItems.Select(ci => new CheckoutProductDto
        {
            ProductId = ci.ProductId,
            Name = ci.Product.Name,
            Quantity = ci.Quantity,
            UnitPrice = ci.Product.DiscountedPrice,
            TotalPrice = ci.Product.DiscountedPrice * ci.Quantity
        }).ToList();

        var subtotal = products.Sum(p => p.TotalPrice);
        decimal discountAmount = 0m;
        Coupon? coupon = null;

        // Ensure customer profile exists
        var customerProfile = await _db.CustomerProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
        if (customerProfile is null)
        {
            customerProfile = new CustomerProfile { UserId = userId };
            _db.CustomerProfiles.Add(customerProfile);
            await _db.SaveChangesAsync();
        }

        if (!string.IsNullOrWhiteSpace(request.CouponName))
        {
            coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.CouponName == request.CouponName);
            if (coupon is null)
                return BadRequest(new { message = "coupon cannot be applicable" });

            var usageCount = await _db.CouponUsages.CountAsync(cu => cu.CouponId == coupon.Id && cu.CustomerProfileId == customerProfile.Id);
            if (coupon.UsageLimit > 0 && usageCount >= coupon.UsageLimit)
                return BadRequest(new { message = "coupon cannot be applicable" });

            discountAmount = coupon.CouponType == CouponType.Percentage
                ? Math.Round(subtotal * (coupon.Value / 100m), 2)
                : coupon.Value;

            if (discountAmount > subtotal) discountAmount = subtotal;

            _db.CouponUsages.Add(new CouponUsage
            {
                CouponId = coupon.Id,
                CustomerProfileId = customerProfile.Id,
                UsedAt = DateTime.UtcNow
            });
        }

        var totalAmount = Math.Max(subtotal - discountAmount, 0m);

        var session = new CheckoutSession
        {
            UserId = userId,
            CartId = cart.Id,
            CouponId = coupon?.Id,
            Subtotal = subtotal,
            DiscountAmount = discountAmount,
            PayableAmount = totalAmount,
            CartItemsSnapshot = JsonSerializer.Serialize(products),
            Status = "Created"
        };

        _db.Add(session);
        await _db.SaveChangesAsync();

        var razorpayResponse = await CreateRazorpayPaymentLink(session, user, products, totalAmount);
        if (razorpayResponse is null)
            return StatusCode(500, new { message = "Unable to create payment link." });

        session.PaymentLinkId = razorpayResponse.LinkId;
        session.PaymentLinkUrl = razorpayResponse.ShortUrl;
        session.Status = "LinkCreated";
        session.UpdatedAt = DateTime.UtcNow;

        foreach (var item in products)
        {
            _db.Orders.Add(new Order
            {
                CustomerProfileId = customerProfile.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                PurchasePrice = item.UnitPrice,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        _db.CartItems.RemoveRange(cart.CartItems);
        _db.Carts.Remove(cart);

        await _db.SaveChangesAsync();

        return Ok(new ProductCheckoutResponse
        {
            Products = products,
            Subtotal = subtotal,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount,
            PaymentLinkUrl = razorpayResponse.ShortUrl,
            PaymentLinkId = razorpayResponse.LinkId,
            Message = "Checkout initiated. Use the payment link to complete payment."
        });
    }

    /// <summary>Razorpay webhook endpoint — confirms payment for product orders and doctor bookings.</summary>
    /// <remarks>
    /// Razorpay calls this URL after a payment link is paid. The signature in the
    /// <c>X-Razorpay-Signature</c> header is verified with HMAC-SHA256 before any action is taken.
    ///
    /// - For **product** payments: creates Order records and clears the cart.
    /// - For **booking** payments: creates the Booking record linked to customer and doctor.
    ///
    /// Configure this URL in the Razorpay dashboard under **Webhooks**.
    /// </remarks>
    /// <response code="200">Webhook processed (always returns 200 to Razorpay, even for no-ops).</response>
    /// <response code="400">Missing or invalid signature, or malformed payload.</response>
    [HttpPost("razorpay/webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RazorpayWebhook()
    {
        var secret = _configuration["Razorpay:KeySecret"];
        if (string.IsNullOrWhiteSpace(secret)) return BadRequest();

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var signature = Request.Headers["X-Razorpay-Signature"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(signature)) return BadRequest();
        if (!VerifyRazorpaySignature(body, secret, signature)) return BadRequest();

        using var document = JsonDocument.Parse(body);
        var eventType = document.RootElement.GetProperty("event").GetString();
        if (string.IsNullOrWhiteSpace(eventType)) return BadRequest();

        var paymentLinkEntity = document.RootElement
            .GetProperty("payload")
            .GetProperty("payment_link")
            .GetProperty("entity");

        var paymentLinkId = paymentLinkEntity.GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(paymentLinkId)) return BadRequest();

        if (eventType != "payment_link.paid" && eventType != "payment_link.completed")
            return Ok();

        string? paymentId = null;
        if (document.RootElement.GetProperty("payload").TryGetProperty("payment", out var paymentProp) &&
            paymentProp.GetProperty("entity").TryGetProperty("id", out var paymentIdProp))
        {
            paymentId = paymentIdProp.GetString();
        }

        // ── Booking payment ───────────────────────────────────────────────────
        var bookingSession = await _db.BookingSessions.FirstOrDefaultAsync(s => s.PaymentLinkId == paymentLinkId);
        if (bookingSession is not null)
        {
            if (bookingSession.Status == "Paid") return Ok();

            var snapshot = JsonSerializer.Deserialize<BookingSnapshotDto>(bookingSession.BookingSnapshot);
            if (snapshot is null) return BadRequest();

            var (startTime, endTime, _) = ParseTimeSlot(snapshot.TimeSlot);

            _db.Bookings.Add(new Booking
            {
                CustomerProfileId = bookingSession.CustomerProfileId,
                DoctorProfileId = bookingSession.DoctorProfileId,
                AppointmentDate = snapshot.AppointmentDate,
                TimeSlot = snapshot.TimeSlot,
                StartTime = startTime,
                EndTime = endTime,
                PatientName = snapshot.PatientName,
                PatientNumber = snapshot.PatientNumber,
                Age = snapshot.Age,
                Gender = snapshot.Gender,
                PrescriptionUpload = snapshot.PrescriptionUpload,
                Description = snapshot.Description,
                ModeOfConsult = snapshot.ModeOfConsult,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            bookingSession.Status = "Paid";
            bookingSession.RazorpayPaymentId = paymentId;
            bookingSession.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok();
        }

        // ── Product payment ───────────────────────────────────────────────────
        var session = await _db.Set<CheckoutSession>().FirstOrDefaultAsync(s => s.PaymentLinkId == paymentLinkId);
        if (session is null) return Ok();
        if (session.Status == "Paid") return Ok();

        var cart = await _db.Carts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.Id == session.CartId);
        if (cart is not null)
        {
            _db.CartItems.RemoveRange(cart.CartItems);
            _db.Carts.Remove(cart);
        }

        session.Status = "Paid";
        session.RazorpayPaymentId = paymentId;
        session.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok();
    }

    private async Task<RazorpayLinkResponse?> CreateRazorpayPaymentLink(
        CheckoutSession session, User user, IReadOnlyList<CheckoutProductDto> products, decimal payableAmount)
    {
        var keyId = _configuration["Razorpay:KeyId"];
        var keySecret = _configuration["Razorpay:KeySecret"];
        var currency = _configuration["Razorpay:Currency"] ?? "INR";
        if (string.IsNullOrWhiteSpace(keyId) || string.IsNullOrWhiteSpace(keySecret)) return null;

        var client = _httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var requestPayload = new Dictionary<string, object>
        {
            ["amount"] = (int)Math.Round(payableAmount * 100m),
            ["currency"] = currency,
            ["reference_id"] = session.Id.ToString(),
            ["description"] = "Pharmeasy cart checkout",
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

        var callbackUrl = _configuration["Razorpay:CallbackUrl"];
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
        if (parts.Length != 2) return (default, default, "invalid");
        TimeSpan.TryParse(parts[0].Trim(), out var start);
        TimeSpan.TryParse(parts[1].Trim(), out var end);
        return (start, end, null);
    }

    private static bool VerifyRazorpaySignature(string body, string secret, string signature)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        var computedSignature = Convert.ToHexString(hash).ToLowerInvariant();
        return computedSignature == signature.ToLowerInvariant();
    }

    private sealed class RazorpayLinkResponse
    {
        public string LinkId { get; set; } = string.Empty;
        public string ShortUrl { get; set; } = string.Empty;
    }
}
