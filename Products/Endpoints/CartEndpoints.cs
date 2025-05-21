using Products.Data;
using Microsoft.AspNetCore.Builder;
using DataEntities;
using Microsoft.EntityFrameworkCore;

namespace Products.Endpoints;

public static class CartEndpoints
{
    public static void MapCartEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Product");
        // For demo, use a fake user id
        const string demoUserId = "demo-user";

        group.MapPost("/cart/add", async (int productId, int quantity, ICartService cartService, ProductDataContext db) =>
        {
            var product = await db.Product.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null)
                return Results.NotFound();
            cartService.AddToCart(demoUserId, product, quantity);
            return Results.Ok();
        })
        .WithName("AddToCart");

        group.MapGet("/cart", (ICartService cartService) =>
        {
            var cart = cartService.GetCart(demoUserId);
            return Results.Ok(cart);
        })
        .WithName("GetCart");

        group.MapDelete("/cart/remove/{productId:int}", (int productId, ICartService cartService) =>
        {
            cartService.RemoveFromCart(demoUserId, productId);
            return Results.Ok();
        })
        .WithName("RemoveFromCart");

        group.MapPost("/cart/clear", (ICartService cartService) =>
        {
            cartService.ClearCart(demoUserId);
            return Results.Ok();
        })
        .WithName("ClearCart");
    }
}
