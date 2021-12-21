using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace GrpcGreeter
{
    public class GreeterService : Greeter.GreeterBase
    {
        private readonly ILogger<GreeterService> _logger;
        public GreeterService(ILogger<GreeterService> logger)
        {
            _logger = logger;
        }

        public override Task<ExampleResponse> UnaryCall(ExampleRequest request, ServerCallContext context)
        {
            return Task.FromResult(new ExampleResponse
            {
                Message = "Hello " + request.Message
            });
        }

        public override async Task StreamingFromServer(ExampleRequest request, IServerStreamWriter<ExampleResponse> responseStream, ServerCallContext context)
        {
            for (var i = 0; i < 5; i++)
            {
                await responseStream.WriteAsync(new ExampleResponse() { Message = request.Message + i.ToString()});
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        public override async Task<ExampleResponse> StreamingFromClient(IAsyncStreamReader<ExampleRequest> requestStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                var message = requestStream.Current;
                Console.WriteLine(message.Message);
            }
            return new ExampleResponse() { Message = "Client stream completed."};
        }

        public override async Task StreamingBothWays(IAsyncStreamReader<ExampleRequest> requestStream, IServerStreamWriter<ExampleResponse> responseStream, ServerCallContext context)
        {
            int i=0;
            await foreach (var message in requestStream.ReadAllAsync())
            {
                Console.WriteLine(message.Message);
                await responseStream.WriteAsync(new ExampleResponse() { Message = "StreamingBothWays " + (++i).ToString() });
            }
        }

        public override async Task StreamingBothWaysSimultaneously(IAsyncStreamReader<ExampleRequest> requestStream, IServerStreamWriter<ExampleResponse> responseStream, ServerCallContext context)
        {
            // Read requests in a background task.
            var readTask = Task.Run(async () =>
            {
                await foreach (var message in requestStream.ReadAllAsync())
                {
                    Console.WriteLine(message.Message);
                }
            });

            int i = 0;
            // Send responses until the client signals that it is complete.
            while (!readTask.IsCompleted)
            {
                await responseStream.WriteAsync(new ExampleResponse() { Message = "StreamingBothWaysSimultaneously " + (++i).ToString() });
                await Task.Delay(TimeSpan.FromSeconds(1), context.CancellationToken);
            }
        }
    }
}
