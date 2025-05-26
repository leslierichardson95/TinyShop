using DataEntities;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Products.Data;

namespace Products.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Product");

        // Get all products
        group.MapGet("/", [OutputCache] async (ProductDataContext db) =>
        {
            return await db.Product.ToListAsync();
        })
        .WithName("GetAllProducts")
        .Produces<List<Product>>(StatusCodes.Status200OK);

        // Get product by id
        group.MapGet("/{id:int}", async (int id, ProductDataContext db) =>
        {
            var product = await db.Product.FindAsync(id);
            return product is not null ? Results.Ok(product) : Results.NotFound();
        })
        .WithName("GetProductById")
        .Produces<Product>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Create product
        group.MapPost("/", async (Product product, ProductDataContext db) =>
        {
            db.Product.Add(product);
            await db.SaveChangesAsync();
            return Results.Created($"/api/Product/{product.Id}", product);
        })
        .WithName("CreateProduct")
        .Produces<Product>(StatusCodes.Status201Created);

        // Update product
        group.MapPut("/{id:int}", async (int id, Product updatedProduct, ProductDataContext db) =>
        {
            var product = await db.Product.FindAsync(id);
            if (product is null) return Results.NotFound();
            product.Name = updatedProduct.Name;
            product.Description = updatedProduct.Description;
            product.Price = updatedProduct.Price;
            product.ImageUrl = updatedProduct.ImageUrl;
            await db.SaveChangesAsync();
            return Results.Ok(product);
        })
        .WithName("UpdateProduct")
        .Produces<Product>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // Delete product
        group.MapDelete("/{id:int}", async (int id, ProductDataContext db) =>
        {
            var product = await db.Product.FindAsync(id);
            if (product is null) return Results.NotFound();
            db.Product.Remove(product);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .WithName("DeleteProduct")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
