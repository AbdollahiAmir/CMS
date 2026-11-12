namespace BackEnd.Data
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Customer = "Customer";

        public static readonly List<string> All = new() { Admin, Customer};
    }
}