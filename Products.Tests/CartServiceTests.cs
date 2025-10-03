using Xunit;
using Products.Data;
using DataEntities;
using System;
using System.Linq;

namespace Products.Tests;

public class CartServiceTests
{
    private InMemoryCartService CreateCartService()
    {
        return new InMemoryCartService();
    }

    private Product CreateTestProduct(int id = 1, string name = "Test Product", decimal price = 10.00m)
    {
        return new Product
        {
            Id = id,
            Name = name,
            Description = "Test Description",
            Price = price,
            ImageUrl = "test.jpg"
        };
    }

    #region GetCart Tests

    [Fact]
    public void GetCart_WithValidUserId_ReturnsEmptyCart()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";

        // Act
        var cart = cartService.GetCart(userId);

        // Assert
        Assert.NotNull(cart);
        Assert.NotNull(cart.Items);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void GetCart_WithSameUserId_ReturnsSameCartInstance()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";

        // Act
        var cart1 = cartService.GetCart(userId);
        var cart2 = cartService.GetCart(userId);

        // Assert
        Assert.Same(cart1, cart2);
    }

    [Fact]
    public void GetCart_WithDifferentUserIds_ReturnsDifferentCarts()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId1 = "user1";
        string userId2 = "user2";

        // Act
        var cart1 = cartService.GetCart(userId1);
        var cart2 = cartService.GetCart(userId2);

        // Assert
        Assert.NotSame(cart1, cart2);
        Assert.Empty(cart1.Items);
        Assert.Empty(cart2.Items);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetCart_WithInvalidUserId_ThrowsArgumentException_ForEmptyStrings(string invalidUserId)
    {
        // Arrange
        var cartService = CreateCartService();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => cartService.GetCart(invalidUserId));
        Assert.Equal("User ID cannot be null or empty. (Parameter 'userId')", exception.Message);
        Assert.Equal("userId", exception.ParamName);
    }

    [Fact]
    public void GetCart_WithNullUserId_ThrowsArgumentException()
    {
        // Arrange
        var cartService = CreateCartService();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => cartService.GetCart(null!));
        Assert.Equal("User ID cannot be null or empty. (Parameter 'userId')", exception.Message);
        Assert.Equal("userId", exception.ParamName);
    }

    #endregion

    #region AddToCart Tests

    [Fact]
    public void AddToCart_WithNewProduct_AddsProductToCart()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";
        var product = CreateTestProduct();

        // Act
        cartService.AddToCart(userId, product);

        // Assert
        var cart = cartService.GetCart(userId);
        Assert.Single(cart.Items);
        Assert.Equal(product.Id, cart.Items.First().Product.Id);
        Assert.Equal(1, cart.Items.First().Quantity);
    }

    [Fact]
    public void AddToCart_WithExistingProduct_IncreasesQuantity()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";
        var product = CreateTestProduct();

        // Act
        cartService.AddToCart(userId, product, 2);
        cartService.AddToCart(userId, product, 3);

        // Assert
        var cart = cartService.GetCart(userId);
        Assert.Single(cart.Items);
        Assert.Equal(product.Id, cart.Items.First().Product.Id);
        Assert.Equal(5, cart.Items.First().Quantity); // 2 + 3
    }

    [Fact]
    public void AddToCart_WithSpecificQuantity_AddsCorrectQuantity()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";
        var product = CreateTestProduct();
        int quantity = 5;

        // Act
        cartService.AddToCart(userId, product, quantity);

        // Assert
        var cart = cartService.GetCart(userId);
        Assert.Single(cart.Items);
        Assert.Equal(quantity, cart.Items.First().Quantity);
    }

    [Fact]
    public void AddToCart_WithMultipleDifferentProducts_AddsAllProducts()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";
        var product1 = CreateTestProduct(1, "Product 1", 10.00m);
        var product2 = CreateTestProduct(2, "Product 2", 20.00m);

        // Act
        cartService.AddToCart(userId, product1, 2);
        cartService.AddToCart(userId, product2, 3);

        // Assert
        var cart = cartService.GetCart(userId);
        Assert.Equal(2, cart.Items.Count);
        
        var item1 = cart.Items.First(i => i.Product.Id == 1);
        var item2 = cart.Items.First(i => i.Product.Id == 2);
        
        Assert.Equal(2, item1.Quantity);
        Assert.Equal(3, item2.Quantity);
    }

    [Fact]
    public void AddToCart_WithDifferentUsers_MaintainsSeparateCarts()
    {
        // Arrange
        var cartService = CreateCartService();
        var product = CreateTestProduct();

        // Act
        cartService.AddToCart("user1", product, 2);
        cartService.AddToCart("user2", product, 3);

        // Assert
        var cart1 = cartService.GetCart("user1");
        var cart2 = cartService.GetCart("user2");
        
        Assert.Single(cart1.Items);
        Assert.Single(cart2.Items);
        Assert.Equal(2, cart1.Items.First().Quantity);
        Assert.Equal(3, cart2.Items.First().Quantity);
    }

    [Fact]
    public void AddToCart_WithDefaultQuantity_AddsOneItem()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";
        var product = CreateTestProduct();

        // Act
        cartService.AddToCart(userId, product); // No quantity specified - should default to 1

        // Assert
        var cart = cartService.GetCart(userId);
        Assert.Single(cart.Items);
        Assert.Equal(1, cart.Items.First().Quantity);
    }

    #endregion

    #region RemoveFromCart Tests

    [Fact]
    public void RemoveFromCart_WithExistingProduct_RemovesProduct()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";
        var product = CreateTestProduct();
        cartService.AddToCart(userId, product, 2);

        // Act
        cartService.RemoveFromCart(userId, product.Id);

        // Assert
        var cart = cartService.GetCart(userId);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void RemoveFromCart_WithNonExistentProduct_DoesNothing()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";
        var product = CreateTestProduct(1);
        cartService.AddToCart(userId, product);

        // Act
        cartService.RemoveFromCart(userId, 999); // Non-existent product ID

        // Assert
        var cart = cartService.GetCart(userId);
        Assert.Single(cart.Items);
        Assert.Equal(product.Id, cart.Items.First().Product.Id);
    }

    [Fact]
    public void RemoveFromCart_WithMultipleProducts_RemovesOnlySpecifiedProduct()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";
        var product1 = CreateTestProduct(1, "Product 1");
        var product2 = CreateTestProduct(2, "Product 2");
        cartService.AddToCart(userId, product1);
        cartService.AddToCart(userId, product2);

        // Act
        cartService.RemoveFromCart(userId, product1.Id);

        // Assert
        var cart = cartService.GetCart(userId);
        Assert.Single(cart.Items);
        Assert.Equal(product2.Id, cart.Items.First().Product.Id);
    }

    [Fact]
    public void RemoveFromCart_FromEmptyCart_DoesNothing()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";

        // Act
        cartService.RemoveFromCart(userId, 1);

        // Assert
        var cart = cartService.GetCart(userId);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void RemoveFromCart_WithDifferentUser_DoesNotAffectOtherUsersCarts()
    {
        // Arrange
        var cartService = CreateCartService();
        var product = CreateTestProduct();
        cartService.AddToCart("user1", product);
        cartService.AddToCart("user2", product);

        // Act
        cartService.RemoveFromCart("user1", product.Id);

        // Assert
        var cart1 = cartService.GetCart("user1");
        var cart2 = cartService.GetCart("user2");
        
        Assert.Empty(cart1.Items);
        Assert.Single(cart2.Items);
    }

    #endregion

    #region GetTotalPrice Tests

    [Fact]
    public void GetTotalPrice_WithEmptyCart_ReturnsZero()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";

        // Act
        var totalPrice = cartService.GetTotalPrice(userId);

        // Assert
        Assert.Equal(0m, totalPrice);
    }

    [Fact]
    public void GetTotalPrice_WithSingleProduct_ReturnsCorrectPrice()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";
        var product = CreateTestProduct(1, "Product", 15.50m);
        cartService.AddToCart(userId, product, 2);

        // Act
        var totalPrice = cartService.GetTotalPrice(userId);

        // Assert
        Assert.Equal(31.00m, totalPrice); // 15.50 * 2
    }

    [Fact]
    public void GetTotalPrice_WithMultipleProducts_ReturnsCorrectTotal()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";
        var product1 = CreateTestProduct(1, "Product 1", 10.00m);
        var product2 = CreateTestProduct(2, "Product 2", 25.50m);
        var product3 = CreateTestProduct(3, "Product 3", 5.25m);
        
        cartService.AddToCart(userId, product1, 2); // 10.00 * 2 = 20.00
        cartService.AddToCart(userId, product2, 1); // 25.50 * 1 = 25.50
        cartService.AddToCart(userId, product3, 4); // 5.25 * 4 = 21.00

        // Act
        var totalPrice = cartService.GetTotalPrice(userId);

        // Assert
        Assert.Equal(66.50m, totalPrice); // 20.00 + 25.50 + 21.00
    }

    [Fact]
    public void GetTotalPrice_WithDecimalPrices_HandlesDecimalsCorrectly()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";
        var product = CreateTestProduct(1, "Product", 12.99m);
        cartService.AddToCart(userId, product, 3);

        // Act
        var totalPrice = cartService.GetTotalPrice(userId);

        // Assert
        Assert.Equal(38.97m, totalPrice); // 12.99 * 3
    }

    [Fact]
    public void GetTotalPrice_AfterRemovingProduct_ReturnsUpdatedTotal()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";
        var product1 = CreateTestProduct(1, "Product 1", 10.00m);
        var product2 = CreateTestProduct(2, "Product 2", 20.00m);
        
        cartService.AddToCart(userId, product1, 2); // 20.00
        cartService.AddToCart(userId, product2, 1); // 20.00
        
        // Act
        cartService.RemoveFromCart(userId, product1.Id);
        var totalPrice = cartService.GetTotalPrice(userId);

        // Assert
        Assert.Equal(20.00m, totalPrice); // Only product2 remains
    }

    [Fact]
    public void GetTotalPrice_WithDifferentUsers_ReturnsCorrectTotalForEachUser()
    {
        // Arrange
        var cartService = CreateCartService();
        var product = CreateTestProduct(1, "Product", 10.00m);
        
        cartService.AddToCart("user1", product, 2); // 20.00
        cartService.AddToCart("user2", product, 3); // 30.00

        // Act
        var totalPrice1 = cartService.GetTotalPrice("user1");
        var totalPrice2 = cartService.GetTotalPrice("user2");

        // Assert
        Assert.Equal(20.00m, totalPrice1);
        Assert.Equal(30.00m, totalPrice2);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CartService_CompleteWorkflow_WorksCorrectly()
    {
        // Arrange
        var cartService = CreateCartService();
        string userId = "user1";
        var product1 = CreateTestProduct(1, "Product 1", 10.00m);
        var product2 = CreateTestProduct(2, "Product 2", 15.00m);

        // Act & Assert - Add products
        cartService.AddToCart(userId, product1, 2);
        cartService.AddToCart(userId, product2, 1);
        
        var cart = cartService.GetCart(userId);
        Assert.Equal(2, cart.Items.Count);
        Assert.Equal(35.00m, cartService.GetTotalPrice(userId)); // 20 + 15

        // Act & Assert - Add more of existing product
        cartService.AddToCart(userId, product1, 1);
        Assert.Equal(3, cart.Items.First(i => i.Product.Id == 1).Quantity);
        Assert.Equal(45.00m, cartService.GetTotalPrice(userId)); // 30 + 15

        // Act & Assert - Remove a product
        cartService.RemoveFromCart(userId, product2.Id);
        Assert.Single(cart.Items);
        Assert.Equal(30.00m, cartService.GetTotalPrice(userId)); // Only product1 remains

        // Act & Assert - Remove last product
        cartService.RemoveFromCart(userId, product1.Id);
        Assert.Empty(cart.Items);
        Assert.Equal(0m, cartService.GetTotalPrice(userId));
    }

    [Fact]
    public void CartService_ConcurrentUserOperations_WorksCorrectly()
    {
        // Arrange
        var cartService = CreateCartService();
        var product = CreateTestProduct(1, "Product", 10.00m);

        // Act - Simulate operations from multiple users
        cartService.AddToCart("user1", product, 1);
        cartService.AddToCart("user2", product, 2);
        cartService.AddToCart("user3", product, 3);

        cartService.RemoveFromCart("user2", product.Id);

        // Assert
        Assert.Single(cartService.GetCart("user1").Items);
        Assert.Empty(cartService.GetCart("user2").Items);
        Assert.Single(cartService.GetCart("user3").Items);

        Assert.Equal(10.00m, cartService.GetTotalPrice("user1"));
        Assert.Equal(0.00m, cartService.GetTotalPrice("user2"));
        Assert.Equal(30.00m, cartService.GetTotalPrice("user3"));
    }

    #endregion
}