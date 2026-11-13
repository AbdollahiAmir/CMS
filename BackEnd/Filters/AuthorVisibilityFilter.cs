using BackEnd.Data;
using BackEnd.DTOs.Common;
using BackEnd.DTOs.Pages;
using BackEnd.DTOs.Posts;
using BackEnd.DTOs.Revisions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Filters
{
    public class AuthorVisibilityFilter : IAsyncResultFilter
    {
        private static readonly HashSet<string> Controllers = new(StringComparer.Ordinal)
        {
            "Posts",
            "ContentPages"
        };

        private readonly IAuthorizationService _authorization;
        private readonly AppDbContext _context;

        public AuthorVisibilityFilter(IAuthorizationService authorization, AppDbContext context)
        {
            _authorization = authorization;
            _context = context;
        }

        private sealed class Collected
        {
            public List<PostDto> Posts { get; } = new();
            public List<PostListItemDto> PostItems { get; } = new();
            public List<ContentPageDto> Pages { get; } = new();
            public List<PageListItemDto> PageItems { get; } = new();
            public List<RevisionListItemDto> Revisions { get; } = new();

            public bool IsEmpty =>
                Posts.Count == 0 && PostItems.Count == 0 && Pages.Count == 0 && PageItems.Count == 0 && Revisions.Count == 0;
        }

        private static void Collect(object? value, Collected target)
        {
            switch (value)
            {
                case PostDto post:
                    target.Posts.Add(post);
                    break;
                case ContentPageDto page:
                    target.Pages.Add(page);
                    break;
                case RevisionListItemDto revision:
                    target.Revisions.Add(revision);
                    break;
                case PagedResult<PostListItemDto> postPage:
                    target.PostItems.AddRange(postPage.Items);
                    break;
                case PagedResult<PageListItemDto> pagePage:
                    target.PageItems.AddRange(pagePage.Items);
                    break;
                case IEnumerable<RevisionListItemDto> revisions:
                    target.Revisions.AddRange(revisions);
                    break;
                case IEnumerable<PageListItemDto> pageItems:
                    target.PageItems.AddRange(pageItems);
                    break;
                case IEnumerable<PostListItemDto> postItems:
                    target.PostItems.AddRange(postItems);
                    break;
            }
        }

        private static string? Name(IReadOnlyDictionary<string, string> names, string? id)
        {
            return id is not null && names.TryGetValue(id, out var name) ? name : null;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.ActionDescriptor is ControllerActionDescriptor descriptor
                && Controllers.Contains(descriptor.ControllerName)
                && context.Result is ObjectResult { Value: not null } result)
            {
                var collected = new Collected();
                Collect(result.Value, collected);

                if (!collected.IsEmpty)
                {
                    var allowed = (await _authorization.AuthorizeAsync(context.HttpContext.User, AppPermissions.AuthorsView)).Succeeded;
                    await Apply(collected, allowed);
                }
            }

            await next();
        }

        private async Task Apply(Collected c, bool allowed)
        {
            IReadOnlyDictionary<string, string> names = new Dictionary<string, string>();

            if (allowed)
            {
                var ids = c.Posts.SelectMany(p => new[] { p.AuthorId, p.UpdatedByUserId })
                    .Concat(c.PostItems.Select(p => p.AuthorId))
                    .Concat(c.Pages.SelectMany(p => new[] { p.CreatedByUserId, p.UpdatedByUserId }))
                    .Concat(c.PageItems.SelectMany(p => new[] { p.CreatedByUserId, p.UpdatedByUserId }))
                    .Concat(c.Revisions.Select(r => r.CreatedByUserId))
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Select(id => id!)
                    .Distinct()
                    .ToList();

                if (ids.Count > 0)
                {
                    names = await _context.Users
                        .AsNoTracking()
                        .Where(u => ids.Contains(u.Id))
                        .Select(u => new { u.Id, Name = u.UserName ?? u.Email ?? u.Id })
                        .ToDictionaryAsync(u => u.Id, u => u.Name);
                }
            }

            foreach (var post in c.Posts)
            {
                post.AuthorId = allowed ? post.AuthorId : null;
                post.UpdatedByUserId = allowed ? post.UpdatedByUserId : null;
                post.AuthorName = Name(names, post.AuthorId);
                post.UpdatedByName = Name(names, post.UpdatedByUserId);
            }

            foreach (var item in c.PostItems)
            {
                item.AuthorId = allowed ? item.AuthorId : null;
                item.AuthorName = Name(names, item.AuthorId);
            }

            foreach (var page in c.Pages)
            {
                page.CreatedByUserId = allowed ? page.CreatedByUserId : null;
                page.UpdatedByUserId = allowed ? page.UpdatedByUserId : null;
                page.CreatedByName = Name(names, page.CreatedByUserId);
                page.UpdatedByName = Name(names, page.UpdatedByUserId);
            }

            foreach (var item in c.PageItems)
            {
                item.CreatedByUserId = allowed ? item.CreatedByUserId : null;
                item.UpdatedByUserId = allowed ? item.UpdatedByUserId : null;
                item.CreatedByName = Name(names, item.CreatedByUserId);
                item.UpdatedByName = Name(names, item.UpdatedByUserId);
            }

            foreach (var revision in c.Revisions)
            {
                revision.CreatedByUserId = allowed ? revision.CreatedByUserId : null;
                revision.CreatedByName = Name(names, revision.CreatedByUserId);
            }
        }
    }
}
