using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace PharmeasyAPI.Services;

public class OtpService
{
    private readonly IConfiguration _config;

    public OtpService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateOtp() =>
        RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

    public string HashOtp(string otp)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(otp));
        return Convert.ToHexString(bytes);
    }

    public bool VerifyOtp(string otp, string hash) =>
        HashOtp(otp) == hash;

    public async Task SendOtpEmailAsync(string toEmail, string otp)
    {
        var smtp = _config.GetSection("Smtp");
        var host = smtp["Host"] ?? throw new InvalidOperationException("Smtp:Host not configured");
        var port = int.Parse(smtp["Port"] ?? "587");
        var username = smtp["Username"] ?? throw new InvalidOperationException("Smtp:Username not configured");
        var password = smtp["Password"] ?? throw new InvalidOperationException("Smtp:Password not configured");
        var from = smtp["From"] ?? username;

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true
        };

        var mail = new MailMessage(from, toEmail)
        {
            Subject = "Your Pharmeasy OTP",
            Body = $"Your OTP is: {otp}\n\nThis code expires in 10 minutes. Do not share it with anyone."
        };

        await client.SendMailAsync(mail);
    }
}
