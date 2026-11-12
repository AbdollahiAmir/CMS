
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

        public const string PagesView = "Pages.View";
        public const string PagesManage = "Pages.Manage";
        public const string PagesPublish = "Pages.Publish";

        public const string PostsView = "Posts.View";
        public const string PostsManage = "Posts.Manage";
        public const string PostsPublish = "Posts.Publish";

        public const string PostCategoriesManage = "PostCategories.Manage";
        public const string TagsManage = "Tags.Manage";

        public const string MediaView = "Media.View";
        public const string MediaManage = "Media.Manage";

        public const string MenusManage = "Menus.Manage";
        public const string SettingsManage = "Settings.Manage";
        public const string AuditView = "Audit.View";

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
            RolesManage,
            PagesView,
            PagesManage,
            PagesPublish,
            PostsView,
            PostsManage,
            PostsPublish,
            PostCategoriesManage,
            TagsManage,
            MediaView,
            MediaManage,
            MenusManage,
            SettingsManage,
            AuditView
        ];
    }
}
