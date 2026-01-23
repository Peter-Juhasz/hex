using System.Collections.Frozen;

namespace HexEditor.Core.ContentType;

public class ContentTypeRegistry : IContentTypeRegistry
{
	public ContentTypeRegistry(IEnumerable<ContentTypeDefinition> definitions)
	{
		_definitions = definitions.ToFrozenDictionary(d => d.Type);
	}

	private readonly FrozenDictionary<string, ContentTypeDefinition> _definitions;

	public bool TryGetDefinition(string type, out ContentTypeDefinition? definition) => _definitions.TryGetValue(type, out definition);

	public IEnumerable<ContentTypeDefinition> GetAllDefinitions() => _definitions.Values;

	public IEnumerable<ContentTypeDefinition> GetBaseTypesAndSelf(string type)
	{
		var set = new HashSet<string>();

		var currentType = type;
		set.Add(currentType);
		yield return _definitions[currentType];

		while (_definitions.TryGetValue(currentType, out var definition) && definition.BaseType != null)
		{
			if (_definitions.TryGetValue(definition.BaseType, out var baseDefinition))
			{
				if (!set.Add(baseDefinition.Type))
				{
					throw new Exception($"Cyclic dependency detected in content type definitions involving type '{baseDefinition.Type}'.");
				}

				yield return baseDefinition;
				currentType = baseDefinition.Type;
			}
			else
			{
				break;
			}
		}
	}
}