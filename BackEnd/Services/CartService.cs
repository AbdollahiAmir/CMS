using BackEnd.Data;
using BackEnd.DTOs.Cart;
using BackEnd.IServices;
using BackEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace BackEnd.Services
{
    public class CartService : ICartService
    {
        private const int MaxItemQuantity = 100;

        private readonly AppDbContext _context;

        public CartService(AppDbContext context)
        {
            _context = context;
        }

        private async Task<Cart> GetOrCreateCartAsync(string userId)
        {
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart is not null)
            {
                return cart;
            }

            cart = new Cart
            {
                UserId = userId
            };

            _context.Carts.Add(cart);

            try
            {
                await _context.SaveChangesAsync();

                return cart;
            }
            catch (DbUpdateException)
            {
                _context.Entry(cart).State = EntityState.Detached;

                return await _context.Carts.FirstAsync(c => c.UserId == userId);
            }
        }

        private async Task RemoveUnavailableItemsAsync(int cartId)
        {
            var unavailableItems = await _context.CartItems
                .Where(i => i.CartId == cartId && (i.Product == null || !i.Product.IsActive))
                .ToListAsync();

            if (unavailableItems.Count == 0)
            {
                return;
            }

            _context.CartItems.RemoveRange(unavailableItems);
            await _context.SaveChangesAsync();
        }

        private async Task<CartDto> BuildCartDtoAsync(int cartId)
        {
            var cart = await _context.Carts.AsNoTracking()
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstAsync(c => c.Id == cartId);

            var items = cart.Items
                .Where(i => i.Product != null && i.Product.IsActive)
                .Select(i =>
                {
                    var unitPrice = i.Product.SalePrice ?? i.Product.Price;

                    return new CartItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.Product.Name,
                        ImgPath = i.Product.ImgPath,
                        Quantity = i.Quantity,
                        UnitPrice = unitPrice,
                        LineTotal = i.Quantity * unitPrice
                    };
                }).ToList();

            return new CartDto
            {
                Id = cart.Id,
                Items = items,
                TotalAmount = items.Sum(i => i.LineTotal)
            };
        }

        public async Task<CartDto> GetAsync(string userId)
        {
            var cart = await GetOrCreateCartAsync(userId);

            await RemoveUnavailableItemsAsync(cart.Id);

            return await BuildCartDtoAsync(cart.Id);
        }

        public async Task<CartDto> AddItemAsync(string userId, AddCartItemDto dto)
        {
            if (dto.Quantity < 1 || dto.Quantity > MaxItemQuantity)
            {
                throw new ArgumentException("تعداد محصول باید بین 1 تا 100 عدد باشد");
            }

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == dto.ProductId);
            if (product is null)
            {
                throw new KeyNotFoundException("محصول یافت نشد");
            }

            if (!product.IsActive)
            {
                throw new InvalidOperationException("این محصول برای خرید در دسترس نیست");
            }

            var cart = await GetOrCreateCartAsync(userId);
            await RemoveUnavailableItemsAsync(cart.Id);

            var cartItem = await _context.CartItems.FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == dto.ProductId);

            var newQuantity = (cartItem?.Quantity ?? 0) + dto.Quantity;
            if (newQuantity > MaxItemQuantity)
            {
                throw new ArgumentException("تعداد کل محصولات نمیتواند بیشتر از 100 عدد باشد.");
            }

            if (newQuantity > product.StockQuantity)
            {
                throw new InvalidOperationException("موجودی این محصول کافی نیست");
            }

            if (cartItem is null)
            {
                _context.CartItems.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                });
            }
            else
            {
                cartItem.Quantity = newQuantity;
            }

            await _context.SaveChangesAsync();

            return await BuildCartDtoAsync(cart.Id);
        }

        public async Task<CartDto?> PatchItemAsync(string userId, int productId, PatchCartItemDto dto)
        {
            if (dto.Quantity < 1 || dto.Quantity > MaxItemQuantity)
            {
                throw new ArgumentException("تعداد محصول باید بین 1 تا 100 عدد باشد");
            }

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart is null)
            {
                return null;
            }

            await RemoveUnavailableItemsAsync(cart.Id);

            var item = await _context.CartItems.Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == productId);
            if (item is null)
            {
                return null;
            }

            if (dto.Quantity > item.Product.StockQuantity)
            {
                throw new InvalidOperationException("موجودی این محصول کافی نیست");
            }

            item.Quantity = dto.Quantity;

            await _context.SaveChangesAsync();

            return await BuildCartDtoAsync(cart.Id);
        }

        public async Task<MergeCartResultDto> MergeAsync(string userId, MergeCartDto dto)
        {
            var cart = await GetOrCreateCartAsync(userId);
            await RemoveUnavailableItemsAsync(cart.Id);

            var requested = dto.Items
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                .ToList();

            var productIds = requested.Select(r => r.ProductId).ToList();

            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var existingItems = await _context.CartItems
                .Where(i => i.CartId == cart.Id && productIds.Contains(i.ProductId))
                .ToDictionaryAsync(i => i.ProductId);

            var skipped = new List<SkippedCartItemDto>();

            foreach (var request in requested)
            {
                if (!products.TryGetValue(request.ProductId, out var product) || !product.IsActive)
                {
                    skipped.Add(new SkippedCartItemDto { ProductId = request.ProductId, Reason = "NotAvailable" });
                    continue;
                }

                if (product.StockQuantity < 1)
                {
                    skipped.Add(new SkippedCartItemDto { ProductId = request.ProductId, Reason = "OutOfStock" });
                    continue;
                }

                existingItems.TryGetValue(request.ProductId, out var cartItem);

                var wanted = (cartItem?.Quantity ?? 0) + request.Quantity;
                var allowed = Math.Min(Math.Min(wanted, MaxItemQuantity), product.StockQuantity);

                if (cartItem is null)
                {
                    _context.CartItems.Add(new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = request.ProductId,
                        Quantity = allowed
                    });
                }
                else
                {
                    cartItem.Quantity = allowed;
                }

                if (allowed < wanted)
                {
                    skipped.Add(new SkippedCartItemDto
                    {
                        ProductId = request.ProductId,
                        Reason = "QuantityReduced",
                        AvailableQuantity = allowed
                    });
                }
            }

            await _context.SaveChangesAsync();

            return new MergeCartResultDto
            {
                Cart = await BuildCartDtoAsync(cart.Id),
                Skipped = skipped
            };
        }

        public async Task<CartDto?> RemoveItemAsync(string userId, int productId)
        {
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart is null)
            {
                return null;
            }

            var item = await _context.CartItems.FirstOrDefaultAsync(i => i.CartId == cart.Id && i.ProductId == productId);
            if (item is null)
            {
                return null;
            }

            _context.CartItems.Remove(item);

            await _context.SaveChangesAsync();

            await RemoveUnavailableItemsAsync(cart.Id);

            return await BuildCartDtoAsync(cart.Id);
        }
    }
}