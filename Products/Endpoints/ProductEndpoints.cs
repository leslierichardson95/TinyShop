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

        group.MapGet("/", [OutputCache] async (ProductDataContext db) =>
        {
            return await db.Product.ToListAsync();
        })
        .WithName("GetAllProducts")
        .Produces<List<Product>>(StatusCodes.Status200OK);
    }
}
