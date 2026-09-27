using Azure.Search.Documents.Indexes.Models;

namespace Copilot.Api.Services.Search;

public interface ISearchIndexDefinitionService
{
    SearchIndex CreateDefinition();
}
