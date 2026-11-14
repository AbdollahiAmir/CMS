using BackEnd.DTOs.Account;
using BackEnd.DTOs.Auth;
using BackEnd.Helpers;
using BackEnd.IServices;
using BackEnd.Models;
using BackEnd.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BackEnd.Services
{
    public class AccountService : IAccountService
    {
        private const string InvalidLink = "لینک نامعتبر یا منقضی شده است";

        private readonly UserManager<AppUser> _userManager;
        private readonly IRefreshTokenService _refreshTokens;
        private readonly IMailSender _mail;
        private readonly ISmsSender _sms;
        private readonly EmailOptions _email;
        private readonly SiteOptions _site;
        private readonly ILogger<AccountService> _logger;

        public AccountService(
            UserManager<AppUser> userManager,
            IRefreshTokenService refreshTokens,
            IMailSender mail,
            ISmsSender sms,
            IOptions<EmailOptions> email,
            IOptions<SiteOptions> site,
            ILogger<AccountService> logger)
        {
            _userManager = userManager;
            _refreshTokens = refreshTokens;
            _mail = mail;
            _sms = sms;
            _email = email.Value;
            _site = site.Value;
            _logger = logger;
        }

        private static AuthResult Fail(params string[] errors)
        {
            return new AuthResult { IsSuccess = false, Errors = errors };
        }

        private static AuthResult Ok()
        {
            return new AuthResult { IsSuccess = true };
        }

        private static AuthResult FromIdentity(IdentityResult result)
        {
            return new AuthResult
            {
                IsSuccess = false,
                Errors = result.Errors.Select(e => e.Description)
            };
        }

        private static ProfileDto Map(AppUser user)
        {
            return new ProfileDto
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed
            };
        }

        private string? BuildLink(string path, params (string Key, string Value)[] query)
        {
            var baseUrl = string.IsNullOrWhiteSpace(_email.FrontendBaseUrl)
                ? _site.BaseUrl
                : _email.FrontendBaseUrl;

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return null;
            }

            var queryString = string.Join(
                "&",
                query.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

            return $"{baseUrl.TrimEnd('/')}{path}?{queryString}";
        }

        private async Task<bool> TrySendMailAsync(string to, string subject, string body)
        {
            try
            {
                await _mail.SendAsync(to, subject, body);
                return true;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send email to {To}", to);
                return false;
            }
        }

        private async Task<bool> IsPhoneTakenAsync(string phoneNumber, string userId)
        {
            return await _userManager.Users.AnyAsync(u =>
                u.PhoneNumber == phoneNumber &&
                u.PhoneNumberConfirmed &&
                u.Id != userId);
        }

        public async Task<ProfileDto?> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            return user is null ? null : Map(user);
        }

        public async Task<ProfileDto?> UpdateProfileAsync(string userId, UpdateProfileDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return null;
            }

            user.FullName = string.IsNullOrWhiteSpace(dto.FullName) ? null : dto.FullName.Trim();

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                throw new ArgumentException(string.Join(" ", result.Errors.Select(e => e.Description)));
            }

            return Map(user);
        }

        public async Task<AuthResult> RequestEmailChangeAsync(string userId, string email)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return Fail("کاربر یافت نشد");
            }

            var normalized = email.Trim();

            if (string.Equals(user.Email, normalized, StringComparison.OrdinalIgnoreCase) && user.EmailConfirmed)
            {
                return Fail("این ایمیل قبلا تایید شده است");
            }

            var existing = await _userManager.FindByEmailAsync(normalized);

            if (existing is not null && existing.Id != user.Id)
            {
                return Fail("این ایمیل قبلا ثبت شده است");
            }

            var result = await _userManager.SetEmailAsync(user, normalized);

            if (!result.Succeeded)
            {
                return FromIdentity(result);
            }

            await SendEmailConfirmationAsync(user);

            return Ok();
        }

        public async Task SendEmailConfirmationAsync(AppUser user)
        {
            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return;
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var link = BuildLink(
                _email.ConfirmEmailPath,
                ("userId", user.Id),
                ("token", token));

            var body = link is null
                ? $"برای تایید ایمیل خود، شناسه‌ی کاربر و کد زیر را وارد کنید.\n\nشناسه: {user.Id}\nکد: {token}"
                : $"برای تایید ایمیل خود روی لینک زیر بزنید:\n\n{link}";

            await TrySendMailAsync(user.Email, "تایید ایمیل", body);
        }

        public async Task<AuthResult> ResendEmailConfirmationAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return Fail("کاربر یافت نشد");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                return Fail("ایمیلی برای این حساب ثبت نشده است");
            }

            if (user.EmailConfirmed)
            {
                return Fail("ایمیل قبلا تایید شده است");
            }

            await SendEmailConfirmationAsync(user);

            return Ok();
        }

        public async Task<AuthResult> ConfirmEmailAsync(ConfirmEmailDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);

            if (user is null)
            {
                return Fail(InvalidLink);
            }

            var result = await _userManager.ConfirmEmailAsync(user, dto.Token);

            return result.Succeeded ? Ok() : Fail(InvalidLink);
        }

        public async Task<AuthResult> RequestPhoneCodeAsync(string userId, string phoneNumber)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return Fail("کاربر یافت نشد");
            }

            var phone = PhoneNumberHelper.Normalize(phoneNumber);

            if (phone is null)
            {
                return Fail("شماره موبایل نامعتبر است");
            }

            if (user.PhoneNumber == phone && user.PhoneNumberConfirmed)
            {
                return Fail("این شماره قبلا تایید شده است");
            }

            if (await IsPhoneTakenAsync(phone, user.Id))
            {
                return Fail("این شماره قبلا ثبت شده است");
            }

            var code = await _userManager.GenerateChangePhoneNumberTokenAsync(user, phone);

            try
            {
                await _sms.SendAsync(phone, $"کد تایید شما: {code}");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to send verification SMS");

                return Fail("ارسال پیامک ناموفق بود، دوباره تلاش کنید");
            }

            return Ok();
        }

        public async Task<AuthResult> VerifyPhoneAsync(string userId, VerifyPhoneDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return Fail("کاربر یافت نشد");
            }

            var phone = PhoneNumberHelper.Normalize(dto.PhoneNumber);

            if (phone is null)
            {
                return Fail("شماره موبایل نامعتبر است");
            }

            if (await IsPhoneTakenAsync(phone, user.Id))
            {
                return Fail("این شماره قبلا ثبت شده است");
            }

            var result = await _userManager.ChangePhoneNumberAsync(user, phone, dto.Code.Trim());

            return result.Succeeded ? Ok() : Fail("کد تایید نامعتبر یا منقضی شده است");
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email.Trim());

            if (user is null || !user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email))
            {
                return;
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var link = BuildLink(
                _email.ResetPasswordPath,
                ("email", user.Email),
                ("token", token));

            var body = link is null
                ? $"برای تعیین رمز عبور جدید، ایمیل و کد زیر را وارد کنید.\n\nایمیل: {user.Email}\nکد: {token}\n\nاگر این درخواست از طرف شما نبوده، این پیام را نادیده بگیرید."
                : $"برای تعیین رمز عبور جدید روی لینک زیر بزنید:\n\n{link}\n\nاگر این درخواست از طرف شما نبوده، این پیام را نادیده بگیرید.";

            var recipient = user.Email;

            _ = Task.Run(() => TrySendMailAsync(recipient, "بازیابی رمز عبور", body));
        }

        public async Task<AuthResult> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email.Trim());

            if (user is null || !user.EmailConfirmed)
            {
                return Fail(InvalidLink);
            }

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

            if (!result.Succeeded)
            {
                var onlyTokenError = result.Errors.All(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken));

                return onlyTokenError ? Fail(InvalidLink) : FromIdentity(result);
            }

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            await _refreshTokens.RevokeAllAsync(user.Id);

            return Ok();
        }
    }
}
