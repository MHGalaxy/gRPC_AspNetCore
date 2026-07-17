using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using gRPC_AspNetCore.Contexts;
using gRPC_AspNetCore.Models;
using gRPC_AspNetCore.Protos;
using Microsoft.EntityFrameworkCore;

namespace gRPC_AspNetCore.Services;

public class GrpcProductService(GrpcContext grpcContext)
    : ProductService.ProductServiceBase
{
    public override async Task GetAllProducts(GetAllProductsRequest request, IServerStreamWriter<GetAllProductsReply> responseStream, ServerCallContext context)
    {
        int skip = (request.Page - 1) * request.Take;

        var products = await grpcContext.Products
            .Skip(skip)
            .Take(request.Take)
            .ToListAsync();

        foreach (var product in products)
        {
            await responseStream.WriteAsync(new GetAllProductsReply
            {
                CreateDate = Timestamp.FromDateTime(DateTime.SpecifyKind(product.CreateDate, DateTimeKind.Utc)),
                Description = product.Description,
                Id = product.Id,
                Price = product.Price,
                Tilte = product.Title
            });
        }
    }

    public override async Task<GetProductByIdReply> GetProductById(GetProductByIdRequest request, ServerCallContext context)
    {
        Product? product = await grpcContext.Products.FirstOrDefaultAsync(p => p.Id == request.Id);

        if (product == null)
            return new GetProductByIdReply();

        #region Add Response Headers (before response body sent)

        Metadata headers = new Metadata()
        {
            { "fName" , "Mohamad Hosein" },
            { "lName" , "Kahkeshan" },
            { "age" , "25"},
        };

        await context.WriteResponseHeadersAsync(headers);

        #endregion

        #region Add Response Trailers (ater response body sent)

        context.ResponseTrailers.Add("FirstName", "Mohamad Hosein");
        context.ResponseTrailers.Add("LastName", "Kahkeshan");
        context.ResponseTrailers.Add("SuccessMessage", "GetProdutById Successfully Done.");

        #endregion

        return new GetProductByIdReply
        {
            CreateDate = Timestamp.FromDateTime(DateTime.SpecifyKind(product.CreateDate, DateTimeKind.Utc)),
            Description = product.Description,
            Id = product.Id,
            Price = product.Price,
            Tilte = product.Title
        };
    }

    public override async Task CreateProduct(IAsyncStreamReader<CreateProductRequest> requestStream, IServerStreamWriter<CreateProductReply> responseStream, ServerCallContext context)
    {
        var createdProductsCount = 0;
        while (await requestStream.MoveNext())
        {
            grpcContext.Products.Add(new Models.Product()
            {
                Title = requestStream.Current.Title,
                Description = requestStream.Current.Description,
                Price = requestStream.Current.Price,
                CreateDate = DateTime.UtcNow,
            });
            createdProductsCount++;
        }

        await grpcContext.SaveChangesAsync();

        await responseStream.WriteAsync(new CreateProductReply
        {
            Status = 200,
            CreatedItemsCount = createdProductsCount,
            Message = $"{createdProductsCount} Products created.",
        });
    }

    public override async Task<UpdateProductReply> UpdateProduct(UpdateProductRequest request, ServerCallContext context)
    {
        Product? product = await grpcContext.Products.FirstOrDefaultAsync(p => p.Id == request.Id);

        if (product == null)
            return new UpdateProductReply();

        product.Title = request.Title;
        product.Description = request.Description;
        product.Price = request.Price;

        grpcContext.Products.Update(product);
        await grpcContext.SaveChangesAsync();

        return new UpdateProductReply()
        {
            Message = "Updated product successfully done",
            Status = 200,
            UpdatedItemsCount = 1
        };
    }

    public override async Task<RemoveProductByIdReply> RemoveProductById(IAsyncStreamReader<RemoveProductByIdRequest> requestStream, ServerCallContext context)
    {
        int removedItemsCount = 0;

        while (await requestStream.MoveNext())
        {
            Product? product = await grpcContext.Products.FirstOrDefaultAsync(p => p.Id == requestStream.Current.Id);

            if (product == null)
                continue;

            grpcContext.Products.Remove(product);

            removedItemsCount++;
        }

        await grpcContext.SaveChangesAsync();

        return new RemoveProductByIdReply()
        {
            Message = "Remoed products successfully done",
            RemovedItemsCount = removedItemsCount,
            Status = 200
        };
    }
}
