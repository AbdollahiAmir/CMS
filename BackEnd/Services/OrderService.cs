using BackEnd.Data;
using BackEnd.DTOs.Common;
using BackEnd.DTOs.Orders;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BackEnd.Services
{
    public class OrderService : IOrderService
    {
        private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
        {
            [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
            [OrderStatus.Confirmed] = [OrderStatus.Shipped, OrderStatus.Cancelled],
            [OrderStatus.Shipped] = [OrderStatus.Delivered],
            [OrderStatus.Delivered] = [],
            [OrderStatus.Cancelled] = []
        };

        private static readonly Expression<Func<Order, OrderDto>> OrderProjection = o => new OrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            UserId = o.UserId,
            UserName = o.User.UserName ?? string.Empty,
            Status = o.Status,
            TotalAmount = o.TotalAmount,
            RecipientName = o.RecipientName,
            Phone = o.Phone,
            Address = o.Address,
            PostalCode = o.PostalCode,
            CustomerNote = o.CustomerNote,
            TrackingCode = o.TrackingCode,
            CancelReason = o.CancelReason,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            Items = o.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Sku = i.Sku,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                LineTotal = i.LineTotal
            }).ToList()
        };

        private static readonly Expression<Func<Order, OrderListItemDto>> ListProjection = o => new OrderListItemDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            UserName = o.User.UserName ?? string.Empty,
            Status = o.Status,
            TotalAmount = o.TotalAmount,
            ItemCount = o.Items.Count,
            CreatedAt = o.CreatedAt
        };

        private readonly AppDbContext _context;

        public OrderService(AppDbContext context)
        {
            _context = context;
        }

        private static string? Clean(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private async Task<string> GenerateOrderNumberAsync()
        {
            while (true)
            {
                var number = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100000, 1000000)}";

                if (!await _context.Orders.AnyAsync(o => o.OrderNumber == number))
                {
                    return number;
                }
            }
        }

        private static void ApplyCancellation(Order order, string? reason)
        {
            foreach (var item in order.Items)
            {
                if (item.Product is not null)
                {
                    item.Product.StockQuantity += item.Quantity;
                }
            }

            order.Status = OrderStatus.Cancelled;
            order.CancelReason = Clean(reason);
            order.UpdatedAt = DateTime.UtcNow;
        }

        private static async Task<PagedResult<OrderListItemDto>> ToPagedAsync(IQueryable<Order> query, OrderQueryDto q)
        {
            if (q.Status.HasValue)
            {
                query = query.Where(o => o.Status == q.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                var search = q.Search.Trim();
                query = query.Where(o => o.OrderNumber.Contains(search) || o.User.UserName!.Contains(search));
            }

            var page = Math.Max(q.Page, 1);
            var pageSize = Math.Clamp(q.PageSize, 1, 100);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ListProjection)
                .ToListAsync();

            return new PagedResult<OrderListItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = total
            };
        }

        public async Task<OrderDto> CreateAsync(string userId, CreateOrderDto dto)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart is null || cart.Items.Count == 0)
            {
                throw new ArgumentException("سبد خرید خالی است");
            }

            var order = new Order
            {
                OrderNumber = await GenerateOrderNumberAsync(),
                UserId = userId,
                Status = OrderStatus.Pending,
                RecipientName = dto.RecipientName.Trim(),
                Phone = dto.Phone.Trim(),
                Address = dto.Address.Trim(),
                PostalCode = Clean(dto.PostalCode),
                CustomerNote = Clean(dto.CustomerNote)
            };

            foreach (var cartItem in cart.Items)
            {
                var product = cartItem.Product;

                if (product is null || !product.IsActive)
                {
                    throw new ArgumentException("یکی از محصولات سبد خرید دیگر در دسترس نیست، سبد خرید را بررسی کنید");
                }

                if (cartItem.Quantity > product.StockQuantity)
                {
                    throw new ArgumentException($"موجودی محصول «{product.Name}» کافی نیست");
                }

                var unitPrice = product.SalePrice ?? product.Price;

                product.StockQuantity -= cartItem.Quantity;

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Sku = product.Sku,
                    UnitPrice = unitPrice,
                    Quantity = cartItem.Quantity,
                    LineTotal = unitPrice * cartItem.Quantity
                });
            }

            order.TotalAmount = order.Items.Sum(i => i.LineTotal);

            _context.Orders.Add(order);
            _context.CartItems.RemoveRange(cart.Items);

            await _context.SaveChangesAsync();

            return (await GetMyOrderAsync(userId, order.Id))!;
        }

        public async Task<PagedResult<OrderListItemDto>> GetMyOrdersAsync(string userId, OrderQueryDto query)
        {
            return await ToPagedAsync(_context.Orders.AsNoTracking().Where(o => o.UserId == userId), query);
        }

        public async Task<OrderDto?> GetMyOrderAsync(string userId, int id)
        {
            return await _context.Orders.AsNoTracking()
                .Where(o => o.Id == id && o.UserId == userId)
                .Select(OrderProjection)
                .FirstOrDefaultAsync();
        }

        public async Task<OrderDto?> CancelMyOrderAsync(string userId, int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order is null)
            {
                return null;
            }

            if (order.Status != OrderStatus.Pending)
            {
                throw new ArgumentException("فقط سفارش‌های در انتظار تأیید قابل لغو هستند");
            }

            ApplyCancellation(order, "لغو توسط مشتری");

            await _context.SaveChangesAsync();

            return await GetMyOrderAsync(userId, id);
        }

        public async Task<PagedResult<OrderListItemDto>> GetAllAsync(OrderQueryDto query)
        {
            return await ToPagedAsync(_context.Orders.AsNoTracking(), query);
        }

        public async Task<OrderDto?> GetByIdAsync(int id)
        {
            return await _context.Orders.AsNoTracking()
                .Where(o => o.Id == id)
                .Select(OrderProjection)
                .FirstOrDefaultAsync();
        }

        public async Task<OrderDto?> UpdateStatusAsync(int id, UpdateOrderStatusDto dto)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order is null)
            {
                return null;
            }

            var newStatus = dto.Status!.Value;

            if (!AllowedTransitions[order.Status].Contains(newStatus))
            {
                throw new ArgumentException("تغییر وضعیت از حالت فعلی سفارش به وضعیت انتخاب‌شده مجاز نیست");
            }

            if (newStatus == OrderStatus.Cancelled)
            {
                ApplyCancellation(order, dto.Reason);
            }
            else
            {
                order.Status = newStatus;
                order.UpdatedAt = DateTime.UtcNow;

                if (newStatus == OrderStatus.Shipped)
                {
                    order.TrackingCode = Clean(dto.TrackingCode);
                }
            }

            await _context.SaveChangesAsync();

            return await GetByIdAsync(id);
        }
    }
}