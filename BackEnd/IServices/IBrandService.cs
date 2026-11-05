using BackEnd.DTOs.Brands;
using BackEnd.Models;

namespace BackEnd.IServices
{
    public interface IBrandService
    {
        Task<List<Brand>> GetAllAsync(bool includeInactive);
        Task<Brand?> GetByIdAsync(int id);
        Task<Brand> CreateAsync(CreateBrandDto dto);
        Task<Brand?> PatchAsync(int id, PatchBrandDto dto);
        Task<bool> DeleteAsync(int id);
        Task<(Stream Stream, string ContentType)?> GetBrandLogoAsync(int id, bool onlyActive);
    }
}