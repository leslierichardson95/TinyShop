using DataEntities;
using System.Text.Json;
using System.Net.Http.Json;

namespace Store.Services;

public class ProductService
{
    HttpClient httpClient;
    public ProductService(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<List<Product>> GetProducts()
    {
        var response = await httpClient.GetAsync("/api/Product");
        if (response.IsSuccessStatusCode)
        {
            var products = await response.Content.ReadFromJsonAsync(ProductSerializerContext.Default.ListProduct);
            return products ?? new List<Product>();
        }
        return new List<Product>();
    }

    public async Task<Product?> GetProductById(int id)
    {
        var response = await httpClient.GetAsync($"/api/Product/{id}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync(ProductSerializerContext.Default.Product);
        }
        return null;
    }

    public async Task<Product?> CreateProduct(Product product)
    {
        var response = await httpClient.PostAsJsonAsync("/api/Product", product, ProductSerializerContext.Default.Product);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync(ProductSerializerContext.Default.Product);
        }
        return null;
    }

    public async Task<Product?> UpdateProduct(int id, Product product)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/Product/{id}", product, ProductSerializerContext.Default.Product);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync(ProductSerializerContext.Default.Product);
        }
        return null;
    }

    public async Task<bool> DeleteProduct(int id)
    {
        var response = await httpClient.DeleteAsync($"/api/Product/{id}");
        return response.IsSuccessStatusCode;
    }
}