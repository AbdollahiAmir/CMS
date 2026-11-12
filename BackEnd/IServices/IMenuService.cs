using BackEnd.DTOs.Menus;

namespace BackEnd.IServices
{
    public interface IMenuService
    {
        Task<List<MenuListItemDto>> GetAllAsync();
        Task<MenuDto?> GetByIdAsync(int id);
        Task<MenuDto?> GetByKeyAsync(string key);
        Task<MenuDto> CreateAsync(CreateMenuDto dto);
        Task<MenuDto?> PatchAsync(int id, PatchMenuDto dto);
        Task<MenuDto?> ReplaceItemsAsync(int id, SaveMenuItemsDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
