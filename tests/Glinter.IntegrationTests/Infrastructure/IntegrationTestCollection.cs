namespace Glinter.IntegrationTests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IntegrationTestCollection : ICollectionFixture<GlinterApiFactory>
{
    public const string Name = "Glinter integration tests";
}
