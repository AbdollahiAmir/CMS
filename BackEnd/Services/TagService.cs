using BackEnd.Data;
using BackEnd.DTOs.Tags;
using BackEnd.Helpers;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services
{
    public class TagService : ITagService
    {
        private readonly AppDbContext _context;

        public TagService(AppDbContext context)
        {
            _context = context;
        }

        private static TagDto Map(Tag tag)
        {
            return new TagDto
            {
                Id = tag.Id,
                Name = tag.Name,
                Slug = tag.Slug,
                PostsCount = tag.Posts.Count
            };
        }

        public async Task<List<TagDto>> GetAllAsync()
        {
            return await _context.Tags
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(t => new TagDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Slug = t.Slug,
                    PostsCount = t.Posts.Count
                })
                .ToListAsync();
        }

        public async Task<TagDto?> GetByIdAsync(int id)
        {
            var tag = await _context.Tags
                .AsNoTracking()
                .Include(t => t.Posts)
                .FirstOrDefaultAsync(t => t.Id == id);

            return tag is null ? null : Map(tag);
        }

        public async Task<TagDto> CreateAsync(CreateTagDto dto)
        {
            var name = dto.Name.Trim();

            if (name.Length == 0)
            {
                throw new ArgumentException("نام تگ الزامی است");
            }

            var tag = new Tag
            {
                Name = name,
                Slug = await SlugHelper.ResolveAsync(
                    dto.Slug,
                    name,
                    slug => _context.Tags.AnyAsync(t => t.Slug == slug))
            };

            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();

            return Map(tag);
        }

        public async Task<TagDto?> PatchAsync(int id, PatchTagDto dto)
        {
            var tag = await _context.Tags
                .Include(t => t.Posts)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tag is null)
            {
                return null;
            }

            if (dto.Name is not null)
            {
                var name = dto.Name.Trim();

                if (name.Length == 0)
                {
                    throw new ArgumentException("نام تگ نمی‌تواند خالی باشد");
                }

                tag.Name = name;
            }

            if (dto.Slug is not null && SlugHelper.Generate(dto.Slug) != tag.Slug)
            {
                tag.Slug = await SlugHelper.ResolveAsync(
                    dto.Slug,
                    tag.Name,
                    slug => _context.Tags.AnyAsync(t => t.Slug == slug && t.Id != id));
            }

            await _context.SaveChangesAsync();

            return Map(tag);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var tag = await _context.Tags
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tag is null)
            {
                return false;
            }

            _context.Tags.Remove(tag);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}