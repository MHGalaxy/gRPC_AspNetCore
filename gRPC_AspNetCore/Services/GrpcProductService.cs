using Grpc.Core;
using gRPC_AspNetCore.Protos;

namespace gRPC_AspNetCore.Services;

public class GrpcProductService : ProductService.ProductServiceBase
{
    public override Task<CreateProductReply> CreateProduct(IAsyncStreamReader<CreateProductRequest> requestStream, ServerCallContext context)
    {
        return base.CreateProduct(requestStream, context);
    }
}
