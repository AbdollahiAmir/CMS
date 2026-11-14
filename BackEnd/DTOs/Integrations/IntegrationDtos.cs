using System.ComponentModel.DataAnnotations;

namespace BackEnd.DTOs.Integrations
{
    public class EmailIntegrationDto
    {
        public bool Enabled { get; set; }
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string UserName { get; set; } = string.Empty;
        public bool PasswordSet { get; set; }
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }

    public class UpdateEmailIntegrationDto
    {
        public bool Enabled { get; set; }

        [MaxLength(255)]
        public string Host { get; set; } = string.Empty;

        [Range(1, 65535)]
        public int Port { get; set; } = 587;

        public bool EnableSsl { get; set; } = true;

        [MaxLength(255)]
        public string UserName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Password { get; set; }

        [MaxLength(256)]
        public string FromAddress { get; set; } = string.Empty;

        [MaxLength(100)]
        public string FromName { get; set; } = string.Empty;
    }

    public class SmsIntegrationDto
    {
        public bool Enabled { get; set; }
        public string Provider { get; set; } = string.Empty;
        public bool ApiKeySet { get; set; }
        public string Sender { get; set; } = string.Empty;
        public IReadOnlyList<string> AvailableProviders { get; set; } = [];
    }

    public class UpdateSmsIntegrationDto
    {
        public bool Enabled { get; set; }

        [Required, MaxLength(30)]
        public string Provider { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? ApiKey { get; set; }

        [MaxLength(30)]
        public string Sender { get; set; } = string.Empty;
    }

    public class PaymentIntegrationDto
    {
        public bool Enabled { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string MerchantId { get; set; } = string.Empty;
        public bool ApiKeySet { get; set; }
        public bool Sandbox { get; set; }
        public IReadOnlyList<string> AvailableProviders { get; set; } = [];
    }

    public class UpdatePaymentIntegrationDto
    {
        public bool Enabled { get; set; }

        [Required, MaxLength(30)]
        public string Provider { get; set; } = string.Empty;

        [MaxLength(255)]
        public string MerchantId { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? ApiKey { get; set; }

        public bool Sandbox { get; set; }
    }

    public class EnamadIntegrationDto
    {
        public bool Enabled { get; set; }
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class UpdateEnamadIntegrationDto
    {
        public bool Enabled { get; set; }

        [MaxLength(2000)]
        public string? Input { get; set; }
    }

    public class TestEmailDto
    {
        [Required, EmailAddress, MaxLength(256)]
        public string To { get; set; } = string.Empty;
    }

    public class TestSmsDto
    {
        [Required, MaxLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class PublicEnamadDto
    {
        public string MetaName { get; set; } = "enamad";
        public string Code { get; set; } = string.Empty;
        public string? SealUrl { get; set; }
        public string? LogoUrl { get; set; }
    }

    public class PublicTrustBadgesDto
    {
        public PublicEnamadDto? Enamad { get; set; }
    }
}
