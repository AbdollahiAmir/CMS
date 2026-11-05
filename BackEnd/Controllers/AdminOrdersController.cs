using BackEnd.Data;
using BackEnd.DTOs.Orders;
using BackEnd.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminOrdersController : ControllerBase
    {
        private readonly IOrderService _service;

        public AdminOrdersController(IOrderService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Policy = AppPermissions.OrdersView)]
        public async Task<IActionResult> GetAllOrders([FromQuery] OrderQueryDto query)
        {
            return Ok(await _service.GetAllAsync(query));
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = AppPermissions.OrdersView)]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _service.GetByIdAsync(id);
            if (order is null)
            {
                return NotFound();
            }

            return Ok(order);
        }

        [HttpPatch("{id:int}/status")]
        [Authorize(Policy = AppPermissions.OrdersManage)]
        public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusDto dto)
        {
            var order = await _service.UpdateStatusAsync(id, dto);
            if (order is null)
            {
                return NotFound();
            }

            return Ok(order);
        }
    }
}