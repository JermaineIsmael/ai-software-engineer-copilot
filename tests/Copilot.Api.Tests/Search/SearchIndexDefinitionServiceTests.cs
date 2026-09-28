using Azure.Search.Documents.Indexes.Models;
using Copilot.Api.Configuration;
using Copilot.Api.Services.Search;
using Microsoft.Extensions.Options;

namespace Copilot.Api.Tests.Search;

public sealed class SearchIndexDefinitionServiceTests
{
    [Fact]
    public void CreateDefinition_ShouldCreateExpectedIndexSchema()
    {
        var options = Options.Create(
            new AzureSearchOptions
            {
                IndexName = "code-chunks",
                VectorDimensions = 1536
            });

        var service = new SearchIndexDefinitionService(options);

        var definition = service.CreateDefinition();

        Assert.Equal("code-chunks", definition.Name);
        Assert.Equal(10, definition.Fields.Count);

        var id = definition.Fields.Single(x => x.Name == "id");
        Assert.True(id.IsKey);
        Assert.True(id.IsFilterable);
        Assert.Equal(SearchFieldDataType.String, id.Type);

        var repository = definition.Fields.Single(x => x.Name == "repository");
        Assert.True(repository.IsFilterable);

        var branch = definition.Fields.Single(x => x.Name == "branch");
        Assert.True(branch.IsFilterable);

        var path = definition.Fields.Single(x => x.Name == "path");
        Assert.True(path.IsSearchable);
        Assert.True(path.IsFilterable);

        var language = definition.Fields.Single(x => x.Name == "language");
        Assert.True(language.IsFilterable);
        Assert.True(language.IsFacetable);

        var chunkIndex = definition.Fields.Single(x => x.Name == "chunkIndex");
        Assert.True(chunkIndex.IsFilterable);
        Assert.Equal(SearchFieldDataType.Int32, chunkIndex.Type);

        var startLine = definition.Fields.Single(x => x.Name == "startLine");
        Assert.Equal(SearchFieldDataType.Int32, startLine.Type);

        var endLine = definition.Fields.Single(x => x.Name == "endLine");
        Assert.Equal(SearchFieldDataType.Int32, endLine.Type);

        var content = definition.Fields.Single(x => x.Name == "content");
        Assert.True(content.IsSearchable);
        Assert.Equal(SearchFieldDataType.String, content.Type);

        var contentVector = definition.Fields.Single(
            x => x.Name == "contentVector");

        Assert.True(contentVector.IsSearchable);
        Assert.Equal(
            SearchFieldDataType.Collection(SearchFieldDataType.Single),
            contentVector.Type);
        Assert.Equal(1536, contentVector.VectorSearchDimensions);
        Assert.Equal(
            "code-vector-profile",
            contentVector.VectorSearchProfileName);

        Assert.NotNull(definition.VectorSearch);

        var profile = Assert.Single(
            definition.VectorSearch.Profiles);

        Assert.Equal("code-vector-profile", profile.Name);
        Assert.Equal(
            "code-hnsw",
            profile.AlgorithmConfigurationName);

        var algorithm = Assert.Single(
            definition.VectorSearch.Algorithms);

        Assert.Equal("code-hnsw", algorithm.Name);
    }

    [Fact]
    public void CreateDefinition_ShouldUseConfiguredVectorDimensions()
    {
        var options = Options.Create(
            new AzureSearchOptions
            {
                IndexName = "custom-code-index",
                VectorDimensions = 3072
            });

        var service = new SearchIndexDefinitionService(options);

        var definition = service.CreateDefinition();

        var contentVector = definition.Fields.Single(
            x => x.Name == "contentVector");

        Assert.Equal(3072, contentVector.VectorSearchDimensions);
        Assert.Equal("custom-code-index", definition.Name);
    }
}
