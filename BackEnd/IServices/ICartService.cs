using BackEnd.DTOs.Cart;

namespace BackEnd.IServices
{
    public interface ICartService
    {
        Task<CartDto> GetAsync(string userId);

        Task<CartDto> AddItemAsync(string userId, AddCartItemDto dto);

        Task<CartDto?> PatchItemAsync(string userId, int productId, PatchCartItemDto dto);

        Task<CartDto?> RemoveItemAsync(string userId, int productId);

        Task<MergeCartResultDto> MergeAsync(string userId, MergeCartDto dto);
    }
}