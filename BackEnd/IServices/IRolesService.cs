using BackEnd.Authorization;
using BackEnd.DTOs.Roles;

namespace BackEnd.IServices
{
    public interface IRolesService
    {
        IReadOnlyList<string> GetPermissions();
        Task<IReadOnlyList<RoleDto>> GetAllAsync();
        Task<RoleDto?> GetByIdAsync(string id);
        Task<RoleResult> CreateAsync(SaveRoleDto dto, Actor actor);
        Task<RoleResult> UpdateAsync(string id, SaveRoleDto dto, Actor actor);
        Task<RoleResult> DeleteAsync(string id, Actor actor);
    }
}