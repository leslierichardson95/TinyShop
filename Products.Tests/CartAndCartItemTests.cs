using Xunit;
using Products.Data;
using DataEntities;
using System.Linq;

namespace Products.Tests;

public class CartItemTests
{
    [Fact]
    public void CartItem_DefaultConstructor_InitializesCorrectly()
    {
        // Act
        var cartItem = new CartItem();

        // Assert
        Assert.Null(cartItem.Product);
        Assert.Equal(0, cartItem.Quantity);
    }

    [Fact]
    public void CartItem_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Description = "Test Description",
            Price = 10.00m,
            ImageUrl = "test.jpg"
        };
        var quantity = 5;

        // Act
        var cartItem = new CartItem
        {
            Product = product,
            Quantity = quantity
        };

        // Assert
        Assert.Equal(product, cartItem.Product);
        Assert.Equal(quantity, cartItem.Quantity);
    }
}

public class CartTests
{
    [Fact]
    public void Cart_DefaultConstructor_InitializesEmptyItemsList()
    {
        // Act
        var cart = new Cart();

        // Assert
        Assert.NotNull(cart.Items);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void Cart_Items_CanAddAndRetrieveItems()
    {
        // Arrange
        var cart = new Cart();
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Description = "Test Description",
            Price = 10.00m,
            ImageUrl = "test.jpg"
        };
        var cartItem = new CartItem
        {
            Product = product,
            Quantity = 2
        };

        // Act
        cart.Items.Add(cartItem);

        // Assert
        Assert.Single(cart.Items);
        Assert.Equal(cartItem, cart.Items.First());
        Assert.Equal(product, cart.Items.First().Product);
        Assert.Equal(2, cart.Items.First().Quantity);
    }

    [Fact]
    public void Cart_Items_CanHoldMultipleItems()
    {
        // Arrange
        var cart = new Cart();
        var product1 = new Product { Id = 1, Name = "Product 1", Price = 10.00m };
        var product2 = new Product { Id = 2, Name = "Product 2", Price = 20.00m };
        
        var cartItem1 = new CartItem { Product = product1, Quantity = 1 };
        var cartItem2 = new CartItem { Product = product2, Quantity = 3 };

        // Act
        cart.Items.Add(cartItem1);
        cart.Items.Add(cartItem2);

        // Assert
        Assert.Equal(2, cart.Items.Count);
        Assert.Contains(cartItem1, cart.Items);
        Assert.Contains(cartItem2, cart.Items);
    }

    [Fact]
    public void Cart_Items_CanRemoveItems()
    {
        // Arrange
        var cart = new Cart();
        var product = new Product { Id = 1, Name = "Product", Price = 10.00m };
        var cartItem = new CartItem { Product = product, Quantity = 1 };
        cart.Items.Add(cartItem);

        // Act
        cart.Items.Remove(cartItem);

        // Assert
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void Cart_Items_CanClearAllItems()
    {
        // Arrange
        var cart = new Cart();
        var product1 = new Product { Id = 1, Name = "Product 1", Price = 10.00m };
        var product2 = new Product { Id = 2, Name = "Product 2", Price = 20.00m };
        
        cart.Items.Add(new CartItem { Product = product1, Quantity = 1 });
        cart.Items.Add(new CartItem { Product = product2, Quantity = 2 });

        // Act
        cart.Items.Clear();

        // Assert
        Assert.Empty(cart.Items);
    }
}