using BackEnd.Data;
using BackEnd.DTOs.Common;
using BackEnd.DTOs.Orders;
using BackEnd.Helpers;
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
                var number = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.NextInt64(100_000_000_000, 1_000_000_000_000)}";

                if (!await _context.Orders.AnyAsync(o => o.OrderNumber == number))
                {
                    return number;
                }
            }
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

        private async Task CancelAsync(Order order, string? reason)
        {
            var expectedStatus = order.Status;
            var orderId = order.Id;
            var cleanReason = Clean(reason);
            var now = DateTime.UtcNow;

            await _context.ExecuteInTransactionAsync(async () =>
            {
                var updated = await _context.Orders
                    .Where(o => o.Id == orderId && o.Status == expectedStatus)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(o => o.Status, OrderStatus.Cancelled)
                        .SetProperty(o => o.CancelReason, cleanReason)
                        .SetProperty(o => o.UpdatedAt, now));

                if (updated == 0)
                {
                    throw new ArgumentException("وضعیت سفارش هم‌زمان تغییر کرد، دوباره تلاش کنید");
                }

                foreach (var item in order.Items.Where(i => i.ProductId.HasValue).OrderBy(i => i.ProductId))
                {
                    var productId = item.ProductId!.Value;
                    var quantity = item.Quantity;

                    await _context.Products
                        .Where(p => p.Id == productId)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity + quantity));
                }
            });
        }

        public async Task<OrderDto> CreateAsync(string userId, CreateOrderDto dto)
        {
            var cart = await _context.Carts
                .AsNoTracking()
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

            foreach (var cartItem in cart.Items.OrderBy(i => i.ProductId))
            {
                var product = cartItem.Product;

                if (product is null || !product.IsActive)
                {
                    throw new ArgumentException("یکی از محصولات سبد خرید دیگر در دسترس نیست، سبد خرید را بررسی کنید");
                }

                var unitPrice = product.SalePrice ?? product.Price;

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

            var cartId = cart.Id;
            var itemCount = cart.Items.Count;

            await _context.ExecuteInTransactionAsync(async () =>
            {
                _context.ChangeTracker.Clear();

                foreach (var item in order.Items.OrderBy(i => i.ProductId))
                {
                    var productId = item.ProductId!.Value;
                    var quantity = item.Quantity;

                    var updated = await _context.Products
                        .Where(p => p.Id == productId && p.IsActive && p.StockQuantity >= quantity)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.StockQuantity, p => p.StockQuantity - quantity));

                    if (updated == 0)
                    {
                        throw new ArgumentException($"موجودی محصول «{item.ProductName}» کافی نیست");
                    }
                }

                var removed = await _context.CartItems.Where(i => i.CartId == cartId).ExecuteDeleteAsync();

                if (removed != itemCount)
                {
                    throw new ArgumentException("سبد خرید هم‌زمان تغییر کرد، سبد را بررسی و دوباره تلاش کنید");
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
            });

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
                .AsNoTracking()
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order is null)
            {
                return null;
            }

            if (order.Status != OrderStatus.Pending)
            {
                throw new ArgumentException("فقط سفارش‌های در انتظار تأیید قابل لغو هستند");
            }

            await CancelAsync(order, "لغو توسط مشتری");

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
                .AsNoTracking()
                .Include(o => o.Items)
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
                await CancelAsync(order, dto.Reason);
            }
            else
            {
                var expectedStatus = order.Status;
                var trackingCode = newStatus == OrderStatus.Shipped ? Clean(dto.TrackingCode) : order.TrackingCode;
                var now = DateTime.UtcNow;

                var updated = await _context.Orders
                    .Where(o => o.Id == id && o.Status == expectedStatus)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(o => o.Status, newStatus)
                        .SetProperty(o => o.TrackingCode, trackingCode)
                        .SetProperty(o => o.UpdatedAt, now));

                if (updated == 0)
                {
                    throw new ArgumentException("وضعیت سفارش هم‌زمان تغییر کرد، دوباره تلاش کنید");
                }
            }

            return await GetByIdAsync(id);
        }
    }
}