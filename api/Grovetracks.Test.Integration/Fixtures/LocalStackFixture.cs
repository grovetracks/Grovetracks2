using Amazon.S3;
using Amazon.S3.Model;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace Grovetracks.Test.Integration.Fixtures;

public class LocalStackFixture : IAsyncLifetime
{
    public const string TestBucketName = "grovetracks-test-drawings";

    private readonly IContainer _container = new ContainerBuilder("localstack/localstack:latest")
        .WithEnvironment("SERVICES", "s3")
        .WithPortBinding(4566, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(4566).ForPath("/_localstack/health")))
        .Build();

    public IAmazonS3 S3Client { get; private set; } = null!;
    public string ServiceUrl => $"http://{_container.Hostname}:{_container.GetMappedPublicPort(4566)}";

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        S3Client = new AmazonS3Client(
            "test",
            "test",
            new AmazonS3Config
            {
                ServiceURL = ServiceUrl,
                ForcePathStyle = true
            });

        await S3Client.PutBucketAsync(new PutBucketRequest { BucketName = TestBucketName });
    }

    public async Task DisposeAsync()
    {
        S3Client.Dispose();
        await _container.DisposeAsync();
    }
}
