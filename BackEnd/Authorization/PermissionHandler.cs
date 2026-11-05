using BackEnd.Data;
using Microsoft.AspNetCore.Authorization;

namespace BackEnd.Authorization
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.User.IsInRole(AppRoles.Admin) || context.User.HasClaim(AppPermissions.ClaimType, requirement.Permission))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}