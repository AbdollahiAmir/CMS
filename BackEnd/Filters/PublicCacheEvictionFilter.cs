using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.OutputCaching;

namespace BackEnd.Filters
{
    public class PublicCacheEvictionFilter : IAsyncActionFilter
    {
        public const string Tag = "public";

        private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
        {
            "GET", "HEAD", "OPTIONS", "TRACE"
        };

        private static readonly HashSet<string> ContentControllers = new(StringComparer.Ordinal)
        {
            "Posts",
            "ContentPages",
            "PostCategories",
            "Tags",
            "Products",
            "ProductCategories",
            "ProductAttributes",
            "Brands",
            "Menus",
            "SiteSettings",
            "PostComments",
            "Integrations"
        };

        private readonly IOutputCacheStore _store;

        public PublicCacheEvictionFilter(IOutputCacheStore store)
        {
            _store = store;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executed = await next();

            if (SafeMethods.Contains(context.HttpContext.Request.Method))
            {
                return;
            }

            if (context.ActionDescriptor is not ControllerActionDescriptor descriptor
                || !ContentControllers.Contains(descriptor.ControllerName))
            {
                return;
            }

            if (executed.Result is IStatusCodeActionResult { StatusCode: >= 200 and < 300 })
            {
                await _store.EvictByTagAsync(Tag, context.HttpContext.RequestAborted);
            }
        }
    }
}
