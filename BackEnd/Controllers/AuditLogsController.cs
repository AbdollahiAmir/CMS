using BackEnd.Data;
using BackEnd.DTOs.AuditLogs;
using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _service;

        public AuditLogsController(IAuditLogService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.AuditView)]
        public async Task<IActionResult> GetAll([FromQuery] AuditLogQueryDto query)
        {
            return Ok(await _service.GetAllAsync(query));
        }
    }
}
