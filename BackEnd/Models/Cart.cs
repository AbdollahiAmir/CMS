namespace BackEnd.Models
{
    public class Cart
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public AppUser User { get; set; } = null;

        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}