namespace HexEditor.Core.ContentType;

public interface IContentTypeRegistry
{
	bool TryGetDefinition(string type, out ContentTypeDefinition? definition);

	IEnumerable<ContentTypeDefinition> GetAllDefinitions();

	IEnumerable<ContentTypeDefinition> GetBaseTypesAndSelf(string type);
}
