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