using BackEnd.Authorization;
using BackEnd.DTOs.Common;
using BackEnd.DTOs.Users;

namespace BackEnd.IServices
{
    public interface IUsersService
    {
        Task<PagedResult<UserDto>> GetAllAsync(UserQueryDto query);
        Task<UserDto?> GetByIdAsync(string id);
        Task<UserResult> EditAsync(string id, EditUserDto dto, Actor actor);
        Task<UserResult> DeleteAsync(string id, Actor actor);
    }
}