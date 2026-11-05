using BackEnd.Data;
using System.Security.Claims;

namespace BackEnd.Authorization
{
    public class Actor
    {
        public string Id { get; }
        public bool IsAdmin { get; }
        public IReadOnlySet<string> Permissions { get; }

        private Actor(string id, bool isAdmin, IReadOnlySet<string> permissions)
        {
            Id = id;
            IsAdmin = isAdmin;
            Permissions = permissions;
        }

        public static Actor From(ClaimsPrincipal user)
        {
            var id = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var isAdmin = user.IsInRole(AppRoles.Admin);
            var permissions = user.FindAll(AppPermissions.ClaimType).Select(c => c.Value).ToHashSet();

            return new Actor(id, isAdmin, permissions);
        }

        public bool HasAll(IEnumerable<string> permissions)
        {
            return IsAdmin || permissions.All(Permissions.Contains);
        }
    }
}