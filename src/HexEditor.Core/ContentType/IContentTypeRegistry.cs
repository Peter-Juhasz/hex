using HexEditor.Model;

namespace HexEditor.Core.ContentType;

public interface IContentTypeRegistry
{
	bool TryGetDefinition(string type, out ContentTypeDefinition? definition);

	IEnumerable<ContentTypeDefinition> GetAllDefinitions();

	IEnumerable<ContentTypeDefinition> GetBaseTypesAndSelf(ContentTypeDefinition? type);

	Task<ContentTypeDefinition?> MatchAsync(string? filePath, IBinarySnapshot snapshot, CancellationToken cancellationToken);
}
