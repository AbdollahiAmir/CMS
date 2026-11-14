namespace BackEnd.Settings
{
    public static class IntegrationKeys
    {
        public const string Email = "email";
        public const string Sms = "sms";
        public const string Payment = "payment";
        public const string Enamad = "enamad";
    }

    public static class SmsProviders
    {
        public const string Kavenegar = "kavenegar";
        public const string SmsIr = "smsir";

        public static readonly IReadOnlyList<string> All = [Kavenegar, SmsIr];
    }

    public static class PaymentProviders
    {
        public const string Zarinpal = "zarinpal";
        public const string Zibal = "zibal";
        public const string IdPay = "idpay";
        public const string NextPay = "nextpay";

        public static readonly IReadOnlyList<string> All = [Zarinpal, Zibal, IdPay, NextPay];

        public static bool NeedsMerchantId(string provider)
        {
            return provider is Zarinpal or Zibal;
        }
    }

    public class EmailIntegration
    {
        public bool Enabled { get; set; }
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }

    public class SmsIntegration
    {
        public bool Enabled { get; set; }
        public string Provider { get; set; } = SmsProviders.Kavenegar;
        public string ApiKey { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
    }

    public class PaymentIntegration
    {
        public bool Enabled { get; set; }
        public string Provider { get; set; } = PaymentProviders.Zarinpal;
        public string MerchantId { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public bool Sandbox { get; set; } = true;
    }

    public class EnamadIntegration
    {
        public bool Enabled { get; set; }
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }
}
