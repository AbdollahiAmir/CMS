namespace BackEnd.Settings
{
    public class EmailOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public string FrontendBaseUrl { get; set; } = string.Empty;
        public string ConfirmEmailPath { get; set; } = "/confirm-email";
        public string ResetPasswordPath { get; set; } = "/reset-password";
    }
}
