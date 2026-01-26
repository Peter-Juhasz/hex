using HexEditor.Model;
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

	public IEnumerable<ContentTypeDefinition> GetBaseTypesAndSelf(ContentTypeDefinition? type)
	{
		if (type == null)
		{
			yield break;
		}

		var set = new HashSet<string>();

		var currentType = type;
		set.Add(currentType.Type);
		yield return currentType;

		while (currentType.BaseType != null && _definitions.TryGetValue(currentType.BaseType, out var baseDefinition))
		{
			if (!set.Add(baseDefinition.Type))
			{
				throw new Exception($"Cyclic dependency detected in content type definitions involving type '{baseDefinition.Type}'.");
			}

			yield return baseDefinition;

			currentType = baseDefinition;
		}
	}

	public async Task<ContentTypeDefinition?> MatchAsync(string? filePath, IBinarySnapshot snapshot, CancellationToken cancellationToken)
	{
		foreach (var definition in _definitions.Values)
		{
			try
			{
				if (await definition.MatchesAsync(filePath, snapshot, default))
				{
					return definition;
				}
			}
			catch (Exception)
			{
				// TODO: log
			}
		}

		return null;
	}
}