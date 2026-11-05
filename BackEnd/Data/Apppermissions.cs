namespace BackEnd.Data
{
    public static class AppPermissions
    {
        public const string ClaimType = "permission";

        public const string CategoriesManage = "Categories.Manage";
        public const string BrandsManage = "Brands.Manage";
        public const string ProductsView = "Products.View";
        public const string ProductsManage = "Products.Manage";
        public const string ProductsToggleActive = "Products.ToggleActive";
        public const string OrdersView = "Orders.View";
        public const string OrdersManage = "Orders.Manage";
        public const string UsersManage = "Users.Manage";
        public const string RolesManage = "Roles.Manage";

        public static readonly IReadOnlyList<string> All =
        [
            CategoriesManage,
            BrandsManage,
            ProductsView,
            ProductsManage,
            ProductsToggleActive,
            OrdersView,
            OrdersManage,
            UsersManage,
            RolesManage
        ];
    }
}