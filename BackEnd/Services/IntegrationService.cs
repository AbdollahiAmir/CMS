using BackEnd.DTOs.Integrations;
using BackEnd.Helpers;
using BackEnd.IServices;
using BackEnd.Settings;

namespace BackEnd.Services
{
    public class IntegrationService : IIntegrationService
    {
        private readonly IIntegrationSettingsService _settings;
        private readonly IMailSender _mail;
        private readonly ISmsSender _sms;

        public IntegrationService(IIntegrationSettingsService settings, IMailSender mail, ISmsSender sms)
        {
            _settings = settings;
            _mail = mail;
            _sms = sms;
        }

        private static EmailIntegrationDto Map(EmailIntegration value)
        {
            return new EmailIntegrationDto
            {
                Enabled = value.Enabled,
                Host = value.Host,
                Port = value.Port,
                EnableSsl = value.EnableSsl,
                UserName = value.UserName,
                PasswordSet = !string.IsNullOrEmpty(value.Password),
                FromAddress = value.FromAddress,
                FromName = value.FromName
            };
        }

        private static SmsIntegrationDto Map(SmsIntegration value)
        {
            return new SmsIntegrationDto
            {
                Enabled = value.Enabled,
                Provider = value.Provider,
                ApiKeySet = !string.IsNullOrEmpty(value.ApiKey),
                Sender = value.Sender,
                AvailableProviders = SmsProviders.All
            };
        }

        private static PaymentIntegrationDto Map(PaymentIntegration value)
        {
            return new PaymentIntegrationDto
            {
                Enabled = value.Enabled,
                Provider = value.Provider,
                MerchantId = value.MerchantId,
                ApiKeySet = !string.IsNullOrEmpty(value.ApiKey),
                Sandbox = value.Sandbox,
                AvailableProviders = PaymentProviders.All
            };
        }

        private static EnamadIntegrationDto Map(EnamadIntegration value)
        {
            return new EnamadIntegrationDto
            {
                Enabled = value.Enabled,
                Id = value.Id,
                Code = value.Code
            };
        }

        public async Task<EmailIntegrationDto> GetEmailAsync()
        {
            return Map(await _settings.GetAsync<EmailIntegration>(IntegrationKeys.Email));
        }

        public async Task<EmailIntegrationDto> UpdateEmailAsync(UpdateEmailIntegrationDto dto, string? userId)
        {
            var current = await _settings.GetAsync<EmailIntegration>(IntegrationKeys.Email);

            current.Enabled = dto.Enabled;
            current.Host = dto.Host.Trim();
            current.Port = dto.Port;
            current.EnableSsl = dto.EnableSsl;
            current.UserName = dto.UserName.Trim();
            current.FromAddress = dto.FromAddress.Trim();
            current.FromName = dto.FromName.Trim();

            if (dto.Password is not null)
            {
                current.Password = dto.Password;
            }

            if (current.Enabled)
            {
                if (current.Host.Length == 0)
                {
                    throw new ArgumentException("آدرس سرور ایمیل الزامی است");
                }

                if (!System.Net.Mail.MailAddress.TryCreate(current.FromAddress, out _))
                {
                    throw new ArgumentException("آدرس ایمیل فرستنده نامعتبر است");
                }
            }

            await _settings.SaveAsync(IntegrationKeys.Email, current, userId);

            return Map(current);
        }

        public async Task SendTestEmailAsync(string to)
        {
            if (!await _mail.IsConfiguredAsync())
            {
                throw new ArgumentException("ارسال ایمیل هنوز تنظیم و فعال نشده است");
            }

            try
            {
                await _mail.SendAsync(to.Trim(), "ایمیل آزمایشی", "این یک ایمیل آزمایشی برای بررسی تنظیمات است.");
            }
            catch (Exception)
            {
                throw new ArgumentException("ارسال ایمیل آزمایشی ناموفق بود، تنظیمات را بررسی کنید");
            }
        }

        public async Task<SmsIntegrationDto> GetSmsAsync()
        {
            return Map(await _settings.GetAsync<SmsIntegration>(IntegrationKeys.Sms));
        }

        public async Task<SmsIntegrationDto> UpdateSmsAsync(UpdateSmsIntegrationDto dto, string? userId)
        {
            var provider = dto.Provider.Trim().ToLowerInvariant();

            if (!SmsProviders.All.Contains(provider))
            {
                throw new ArgumentException("ارائه‌دهنده‌ی پیامک نامعتبر است");
            }

            var current = await _settings.GetAsync<SmsIntegration>(IntegrationKeys.Sms);

            current.Enabled = dto.Enabled;
            current.Provider = provider;
            current.Sender = dto.Sender.Trim();

            if (dto.ApiKey is not null)
            {
                current.ApiKey = dto.ApiKey.Trim();
            }

            if (current.Enabled)
            {
                if (current.ApiKey.Length == 0)
                {
                    throw new ArgumentException("کلید API الزامی است");
                }

                if (provider == SmsProviders.SmsIr && !long.TryParse(current.Sender, out _))
                {
                    throw new ArgumentException("برای sms.ir شماره‌ی خط ارسال‌کننده باید عدد باشد");
                }
            }

            await _settings.SaveAsync(IntegrationKeys.Sms, current, userId);

            return Map(current);
        }

        public async Task SendTestSmsAsync(string phoneNumber)
        {
            var phone = PhoneNumberHelper.Normalize(phoneNumber)
                ?? throw new ArgumentException("شماره موبایل نامعتبر است");

            if (!await _sms.IsConfiguredAsync())
            {
                throw new ArgumentException("ارسال پیامک هنوز تنظیم و فعال نشده است");
            }

            try
            {
                await _sms.SendAsync(phone, "پیامک آزمایشی برای بررسی تنظیمات");
            }
            catch (Exception)
            {
                throw new ArgumentException("ارسال پیامک آزمایشی ناموفق بود، تنظیمات را بررسی کنید");
            }
        }

        public async Task<PaymentIntegrationDto> GetPaymentAsync()
        {
            return Map(await _settings.GetAsync<PaymentIntegration>(IntegrationKeys.Payment));
        }

        public async Task<PaymentIntegrationDto> UpdatePaymentAsync(UpdatePaymentIntegrationDto dto, string? userId)
        {
            var provider = dto.Provider.Trim().ToLowerInvariant();

            if (!PaymentProviders.All.Contains(provider))
            {
                throw new ArgumentException("درگاه پرداخت نامعتبر است");
            }

            var current = await _settings.GetAsync<PaymentIntegration>(IntegrationKeys.Payment);

            current.Enabled = dto.Enabled;
            current.Provider = provider;
            current.MerchantId = dto.MerchantId.Trim();
            current.Sandbox = dto.Sandbox;

            if (dto.ApiKey is not null)
            {
                current.ApiKey = dto.ApiKey.Trim();
            }

            if (current.Enabled)
            {
                if (PaymentProviders.NeedsMerchantId(provider) && current.MerchantId.Length == 0)
                {
                    throw new ArgumentException("کد مرچنت الزامی است");
                }

                if (!PaymentProviders.NeedsMerchantId(provider) && current.ApiKey.Length == 0)
                {
                    throw new ArgumentException("کلید API الزامی است");
                }
            }

            await _settings.SaveAsync(IntegrationKeys.Payment, current, userId);

            return Map(current);
        }

        public async Task<EnamadIntegrationDto> GetEnamadAsync()
        {
            return Map(await _settings.GetAsync<EnamadIntegration>(IntegrationKeys.Enamad));
        }

        public async Task<EnamadIntegrationDto> UpdateEnamadAsync(UpdateEnamadIntegrationDto dto, string? userId)
        {
            var current = await _settings.GetAsync<EnamadIntegration>(IntegrationKeys.Enamad);

            if (!string.IsNullOrWhiteSpace(dto.Input))
            {
                var (id, code) = EnamadParser.Parse(dto.Input);

                if (code is null)
                {
                    throw new ArgumentException("کد اینماد نامعتبر است. فقط کد یا کل کد HTML دریافتی از اینماد را وارد کنید");
                }

                current.Code = code;
                current.Id = id ?? string.Empty;
            }

            current.Enabled = dto.Enabled;

            if (current.Enabled && current.Code.Length == 0)
            {
                throw new ArgumentException("برای فعال‌سازی ابتدا کد اینماد را وارد کنید");
            }

            await _settings.SaveAsync(IntegrationKeys.Enamad, current, userId);

            return Map(current);
        }

        public async Task<PublicTrustBadgesDto> GetPublicTrustBadgesAsync()
        {
            var enamad = await _settings.GetAsync<EnamadIntegration>(IntegrationKeys.Enamad);

            if (!enamad.Enabled || enamad.Code.Length == 0)
            {
                return new PublicTrustBadgesDto();
            }

            var hasId = enamad.Id.Length > 0;

            return new PublicTrustBadgesDto
            {
                Enamad = new PublicEnamadDto
                {
                    Code = enamad.Code,
                    SealUrl = hasId ? $"https://trustseal.enamad.ir/?id={enamad.Id}&Code={enamad.Code}" : null,
                    LogoUrl = hasId ? $"https://trustseal.enamad.ir/logo.aspx?id={enamad.Id}&Code={enamad.Code}" : null
                }
            };
        }
    }
}
