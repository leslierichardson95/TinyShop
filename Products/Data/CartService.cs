using System.Collections.Concurrent;
using DataEntities;

namespace Products.Data;

public class CartItem
{
    public Product Product { get; set; } = default!;
    public int Quantity { get; set; }
}

public class Cart
{
    public List<CartItem> Items { get; set; } = new();
}

public interface ICartService
{
    Cart GetCart(string userId);
    void AddToCart(string userId, Product product, int quantity = 1);
    void RemoveFromCart(string userId, int productId);
    void ClearCart(string userId);
    decimal GetTotalPrice(string userId);
}

public class InMemoryCartService : ICartService
{
    private readonly ConcurrentDictionary<string, Cart> _carts = new();

    //DEMO: Implement with Copilot - GetCart
    public Cart GetCart(string userId)
    {
        return _carts.GetOrAdd(userId, _ => new Cart());
    }

    public void AddToCart(string userId, Product product, int quantity = 1)
    {
        var cart = GetCart(userId);
        var item = cart.Items.FirstOrDefault(i => i.Product.Id == product.Id);
        if (item == null)
        {
            cart.Items.Add(new CartItem { Product = product, Quantity = quantity });
        }
        else
        {
            item.Quantity += quantity;
        }
    }

    public void RemoveFromCart(string userId, int productId)
    {
        var cart = GetCart(userId);
        cart.Items.RemoveAll(i => i.Product.Id == productId);
    }

    public void ClearCart(string userId)
    {
        var cart = GetCart(userId);
        cart.Items.Clear();
    }

    public decimal GetTotalPrice(string userId)
    {
        var cart = GetCart(userId);
        return cart.Items.Sum(i => i.Product.Price * i.Quantity);
    }
}
