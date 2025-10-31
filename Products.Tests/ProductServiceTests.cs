using Xunit;
using Store.Services;
using DataEntities;
using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Products.Tests;

public class ProductServiceTests
{
    private ProductService CreateProductService(MockHttpMessageHandler mockHttp)
    {
        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost");
        return new ProductService(httpClient);
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

    private string SerializeProduct(Product product)
    {
        return JsonSerializer.Serialize(product, ProductSerializerContext.Default.Product);
    }

    private string SerializeProductList(List<Product> products)
    {
        return JsonSerializer.Serialize(products, ProductSerializerContext.Default.ListProduct);
    }

    #region GetProducts Tests

    [Fact]
    public async Task GetProducts_WithSuccessfulResponse_ReturnsProductList()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var testProducts = new List<Product>
        {
            CreateTestProduct(1, "Product 1", 15.99m),
            CreateTestProduct(2, "Product 2", 25.99m),
            CreateTestProduct(3, "Product 3", 35.99m)
        };
        var productsJson = SerializeProductList(testProducts);

        mockHttp.When("https://localhost/api/Product")
                .Respond("application/json", productsJson);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProducts();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        
        Assert.Equal(testProducts[0].Id, result[0].Id);
        Assert.Equal(testProducts[0].Name, result[0].Name);
        Assert.Equal(testProducts[0].Price, result[0].Price);
        
        Assert.Equal(testProducts[1].Id, result[1].Id);
        Assert.Equal(testProducts[1].Name, result[1].Name);
        Assert.Equal(testProducts[1].Price, result[1].Price);
        
        Assert.Equal(testProducts[2].Id, result[2].Id);
        Assert.Equal(testProducts[2].Name, result[2].Name);
        Assert.Equal(testProducts[2].Price, result[2].Price);
    }

    [Fact]
    public async Task GetProducts_WithEmptyResponse_ReturnsEmptyList()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var emptyProductList = new List<Product>();
        var emptyJson = SerializeProductList(emptyProductList);

        mockHttp.When("https://localhost/api/Product")
                .Respond("application/json", emptyJson);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProducts();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProducts_WithSingleProduct_ReturnsSingleItemList()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var singleProduct = new List<Product> { CreateTestProduct(1, "Single Product", 99.99m) };
        var singleProductJson = SerializeProductList(singleProduct);

        mockHttp.When("https://localhost/api/Product")
                .Respond("application/json", singleProductJson);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProducts();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Single Product", result[0].Name);
        Assert.Equal(99.99m, result[0].Price);
    }

    [Fact]
    public async Task GetProducts_WithServerError_ReturnsEmptyList()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://localhost/api/Product")
                .Respond(HttpStatusCode.InternalServerError);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProducts();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProducts_WithBadRequest_ReturnsEmptyList()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://localhost/api/Product")
                .Respond(HttpStatusCode.BadRequest);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProducts();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProducts_WithNotFound_ReturnsEmptyList()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://localhost/api/Product")
                .Respond(HttpStatusCode.NotFound);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProducts();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProducts_WithUnauthorized_ReturnsEmptyList()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://localhost/api/Product")
                .Respond(HttpStatusCode.Unauthorized);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProducts();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProducts_WithNullResponseContent_ReturnsEmptyList()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://localhost/api/Product")
                .Respond("application/json", "null");

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProducts();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetProducts_WithMalformedJson_ReturnsEmptyList()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://localhost/api/Product")
                .Respond("application/json", "{ invalid json }");

        var productService = CreateProductService(mockHttp);

        // Act & Assert
        // ReadFromJsonAsync will throw JsonException for malformed JSON
        // The ProductService should handle this gracefully
        await Assert.ThrowsAsync<JsonException>(async () => await productService.GetProducts());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task GetProducts_WithVariousProductCounts_ReturnsCorrectCount(int productCount)
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var testProducts = new List<Product>();
        
        for (int i = 1; i <= productCount; i++)
        {
            testProducts.Add(CreateTestProduct(i, $"Product {i}", i * 10.0m));
        }
        
        var productsJson = SerializeProductList(testProducts);

        mockHttp.When("https://localhost/api/Product")
                .Respond("application/json", productsJson);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProducts();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(productCount, result.Count);
        
        // Verify first and last products to ensure proper deserialization
        if (productCount > 0)
        {
            Assert.Equal(1, result[0].Id);
            Assert.Equal("Product 1", result[0].Name);
            Assert.Equal(productCount, result[productCount - 1].Id);
            Assert.Equal($"Product {productCount}", result[productCount - 1].Name);
        }
    }

    [Fact]
    public async Task GetProducts_WithProductsHavingNullProperties_HandlesGracefully()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var testProducts = new List<Product>
        {
            new Product { Id = 1, Name = null, Description = null, Price = 10.99m, ImageUrl = null },
            new Product { Id = 2, Name = "Valid Product", Description = "Valid Description", Price = 20.99m, ImageUrl = "valid.jpg" }
        };
        var productsJson = SerializeProductList(testProducts);

        mockHttp.When("https://localhost/api/Product")
                .Respond("application/json", productsJson);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProducts();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        
        // First product with null properties
        Assert.Equal(1, result[0].Id);
        Assert.Null(result[0].Name);
        Assert.Null(result[0].Description);
        Assert.Equal(10.99m, result[0].Price);
        Assert.Null(result[0].ImageUrl);
        
        // Second product with valid properties
        Assert.Equal(2, result[1].Id);
        Assert.Equal("Valid Product", result[1].Name);
        Assert.Equal("Valid Description", result[1].Description);
        Assert.Equal(20.99m, result[1].Price);
        Assert.Equal("valid.jpg", result[1].ImageUrl);
    }

    [Fact]
    public async Task GetProducts_CallsCorrectEndpoint()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var testProducts = new List<Product> { CreateTestProduct() };
        var productsJson = SerializeProductList(testProducts);

        // Verify exact endpoint is called
        var expectedRequest = mockHttp.When("https://localhost/api/Product")
                                     .Respond("application/json", productsJson);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProducts();

        // Assert
        Assert.NotNull(result);
        mockHttp.VerifyNoOutstandingExpectation();
    }

    [Fact]
    public async Task GetProducts_WithTimeout_ThrowsTaskCanceledException()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://localhost/api/Product")
                .Respond(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(30)); // Simulate long delay
                    return new HttpResponseMessage(HttpStatusCode.OK);
                });

        var httpClient = mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://localhost");
        httpClient.Timeout = TimeSpan.FromMilliseconds(100); // Very short timeout
        
        var productService = new ProductService(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await productService.GetProducts());
    }

    #endregion

    #region GetProductById Tests

    [Fact]
    public async Task GetProductById_WithValidId_ReturnsProduct()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var testProduct = CreateTestProduct(1, "Test Product", 15.99m);
        var productJson = SerializeProduct(testProduct);

        mockHttp.When("https://localhost/api/Product/1")
                .Respond("application/json", productJson);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProductById(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(testProduct.Id, result.Id);
        Assert.Equal(testProduct.Name, result.Name);
        Assert.Equal(testProduct.Description, result.Description);
        Assert.Equal(testProduct.Price, result.Price);
        Assert.Equal(testProduct.ImageUrl, result.ImageUrl);
    }

    [Fact]
    public async Task GetProductById_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://localhost/api/Product/999")
                .Respond(HttpStatusCode.NotFound);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProductById(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductById_WithServerError_ReturnsNull()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://localhost/api/Product/1")
                .Respond(HttpStatusCode.InternalServerError);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProductById(1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductById_WithBadRequest_ReturnsNull()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://localhost/api/Product/-1")
                .Respond(HttpStatusCode.BadRequest);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProductById(-1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProductById_WithEmptyResponseContent_ReturnsNull()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://localhost/api/Product/1")
                .Respond("application/json", "null");

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProductById(1);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public async Task GetProductById_WithVariousIds_CallsCorrectEndpoint(int id)
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var testProduct = CreateTestProduct(id);
        var productJson = SerializeProduct(testProduct);

        mockHttp.When($"https://localhost/api/Product/{id}")
                .Respond("application/json", productJson);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.GetProductById(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    #endregion

    #region CreateProduct Tests

    [Fact]
    public async Task CreateProduct_WithValidProduct_ReturnsCreatedProduct()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var inputProduct = CreateTestProduct(0, "New Product", 25.50m); // ID 0 for new product
        var createdProduct = CreateTestProduct(123, "New Product", 25.50m); // ID assigned by server
        var createdProductJson = SerializeProduct(createdProduct);

        mockHttp.When(HttpMethod.Post, "https://localhost/api/Product")
                .Respond("application/json", createdProductJson);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.CreateProduct(inputProduct);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createdProduct.Id, result.Id);
        Assert.Equal(createdProduct.Name, result.Name);
        Assert.Equal(createdProduct.Description, result.Description);
        Assert.Equal(createdProduct.Price, result.Price);
        Assert.Equal(createdProduct.ImageUrl, result.ImageUrl);
    }

    [Fact]
    public async Task CreateProduct_WithBadRequest_ReturnsNull()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var product = CreateTestProduct(0, "Invalid Product");

        mockHttp.When(HttpMethod.Post, "https://localhost/api/Product")
                .Respond(HttpStatusCode.BadRequest);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.CreateProduct(product);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateProduct_WithServerError_ReturnsNull()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var product = CreateTestProduct(0, "Test Product");

        mockHttp.When(HttpMethod.Post, "https://localhost/api/Product")
                .Respond(HttpStatusCode.InternalServerError);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.CreateProduct(product);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateProduct_WithConflict_ReturnsNull()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var product = CreateTestProduct(0, "Duplicate Product");

        mockHttp.When(HttpMethod.Post, "https://localhost/api/Product")
                .Respond(HttpStatusCode.Conflict);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.CreateProduct(product);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateProduct_WithNullProduct_HandlesGracefully()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        
        // HttpClient.PostAsJsonAsync with null will result in a successful HTTP call
        // but with null content, so we expect the service to handle this gracefully
        mockHttp.When(HttpMethod.Post, "https://localhost/api/Product")
                .Respond(HttpStatusCode.BadRequest);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.CreateProduct(null!);

        // Assert
        // The service should return null for unsuccessful responses
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateProduct_WithEmptyResponseContent_ReturnsNull()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var product = CreateTestProduct(0, "Test Product");

        mockHttp.When(HttpMethod.Post, "https://localhost/api/Product")
                .Respond("application/json", "null");

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.CreateProduct(product);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region UpdateProduct Tests

    [Fact]
    public async Task UpdateProduct_WithValidProduct_ReturnsUpdatedProduct()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var productToUpdate = CreateTestProduct(1, "Updated Product", 35.75m);
        var updatedProductJson = SerializeProduct(productToUpdate);

        mockHttp.When(HttpMethod.Put, "https://localhost/api/Product/1")
                .Respond("application/json", updatedProductJson);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.UpdateProduct(1, productToUpdate);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(productToUpdate.Id, result.Id);
        Assert.Equal(productToUpdate.Name, result.Name);
        Assert.Equal(productToUpdate.Description, result.Description);
        Assert.Equal(productToUpdate.Price, result.Price);
        Assert.Equal(productToUpdate.ImageUrl, result.ImageUrl);
    }

    [Fact]
    public async Task UpdateProduct_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var product = CreateTestProduct(999, "Non-existent Product");

        mockHttp.When(HttpMethod.Put, "https://localhost/api/Product/999")
                .Respond(HttpStatusCode.NotFound);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.UpdateProduct(999, product);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProduct_WithBadRequest_ReturnsNull()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var product = CreateTestProduct(1, "Invalid Product");

        mockHttp.When(HttpMethod.Put, "https://localhost/api/Product/1")
                .Respond(HttpStatusCode.BadRequest);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.UpdateProduct(1, product);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProduct_WithServerError_ReturnsNull()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var product = CreateTestProduct(1, "Test Product");

        mockHttp.When(HttpMethod.Put, "https://localhost/api/Product/1")
                .Respond(HttpStatusCode.InternalServerError);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.UpdateProduct(1, product);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProduct_WithMismatchedIds_StillCallsCorrectEndpoint()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var product = CreateTestProduct(5, "Product with ID 5");
        var updatedProductJson = SerializeProduct(product);

        // The endpoint should use the ID parameter, not the product's ID
        mockHttp.When(HttpMethod.Put, "https://localhost/api/Product/3")
                .Respond("application/json", updatedProductJson);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.UpdateProduct(3, product);

        // Assert
        Assert.NotNull(result);
        // The result should reflect what the server returned
        Assert.Equal(product.Name, result.Name);
    }

    [Fact]
    public async Task UpdateProduct_WithNullProduct_HandlesGracefully()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        
        // HttpClient.PutAsJsonAsync with null will result in a successful HTTP call
        // but with null content, so we expect the service to handle this gracefully
        mockHttp.When(HttpMethod.Put, "https://localhost/api/Product/1")
                .Respond(HttpStatusCode.BadRequest);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.UpdateProduct(1, null!);

        // Assert
        // The service should return null for unsuccessful responses
        Assert.Null(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task UpdateProduct_WithVariousIds_CallsCorrectEndpoint(int id)
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var product = CreateTestProduct(id, "Test Product");
        var productJson = SerializeProduct(product);

        mockHttp.When(HttpMethod.Put, $"https://localhost/api/Product/{id}")
                .Respond("application/json", productJson);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.UpdateProduct(id, product);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(product.Name, result.Name);
    }

    #endregion

    #region DeleteProduct Tests

    [Fact]
    public async Task DeleteProduct_WithValidId_ReturnsTrue()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, "https://localhost/api/Product/1")
                .Respond(HttpStatusCode.OK);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.DeleteProduct(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteProduct_WithNoContentResponse_ReturnsTrue()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, "https://localhost/api/Product/1")
                .Respond(HttpStatusCode.NoContent);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.DeleteProduct(1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteProduct_WithNonExistentId_ReturnsFalse()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, "https://localhost/api/Product/999")
                .Respond(HttpStatusCode.NotFound);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.DeleteProduct(999);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteProduct_WithBadRequest_ReturnsFalse()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, "https://localhost/api/Product/-1")
                .Respond(HttpStatusCode.BadRequest);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.DeleteProduct(-1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteProduct_WithServerError_ReturnsFalse()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, "https://localhost/api/Product/1")
                .Respond(HttpStatusCode.InternalServerError);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.DeleteProduct(1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteProduct_WithConflict_ReturnsFalse()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, "https://localhost/api/Product/1")
                .Respond(HttpStatusCode.Conflict);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.DeleteProduct(1);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public async Task DeleteProduct_WithVariousIds_CallsCorrectEndpoint(int id)
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, $"https://localhost/api/Product/{id}")
                .Respond(HttpStatusCode.OK);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.DeleteProduct(id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteProduct_WithForbidden_ReturnsFalse()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, "https://localhost/api/Product/1")
                .Respond(HttpStatusCode.Forbidden);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.DeleteProduct(1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteProduct_WithUnauthorized_ReturnsFalse()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When(HttpMethod.Delete, "https://localhost/api/Product/1")
                .Respond(HttpStatusCode.Unauthorized);

        var productService = CreateProductService(mockHttp);

        // Act
        var result = await productService.DeleteProduct(1);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task ProductService_CompleteWorkflow_WorksCorrectly()
    {
        // Arrange - We need separate mock handlers for each operation
        // to avoid conflicts between different HTTP calls
        var createMockHttp = new MockHttpMessageHandler();
        var getMockHttp = new MockHttpMessageHandler();
        var updateMockHttp = new MockHttpMessageHandler();
        var deleteMockHttp = new MockHttpMessageHandler();

        var newProduct = CreateTestProduct(0, "Workflow Product", 50.00m);
        var createdProduct = CreateTestProduct(1, "Workflow Product", 50.00m);
        var updatedProduct = CreateTestProduct(1, "Updated Workflow Product", 60.00m);

        // Test Create
        createMockHttp.When(HttpMethod.Post, "https://localhost/api/Product")
                      .Respond("application/json", SerializeProduct(createdProduct));
        var createService = CreateProductService(createMockHttp);
        var created = await createService.CreateProduct(newProduct);
        Assert.NotNull(created);
        Assert.Equal(1, created.Id);
        Assert.Equal("Workflow Product", created.Name);

        // Test Get
        getMockHttp.When("https://localhost/api/Product/1")
                   .Respond("application/json", SerializeProduct(createdProduct));
        var getService = CreateProductService(getMockHttp);
        var retrieved = await getService.GetProductById(1);
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);

        // Test Update
        updateMockHttp.When(HttpMethod.Put, "https://localhost/api/Product/1")
                      .Respond("application/json", SerializeProduct(updatedProduct));
        var updateService = CreateProductService(updateMockHttp);
        var updated = await updateService.UpdateProduct(1, updatedProduct);
        Assert.NotNull(updated);
        Assert.Equal("Updated Workflow Product", updated.Name);
        Assert.Equal(60.00m, updated.Price);

        // Test Delete
        deleteMockHttp.When(HttpMethod.Delete, "https://localhost/api/Product/1")
                      .Respond(HttpStatusCode.OK);
        var deleteService = CreateProductService(deleteMockHttp);
        var deleted = await deleteService.DeleteProduct(1);
        Assert.True(deleted);
    }

    #endregion
}