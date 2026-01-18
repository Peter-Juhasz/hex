using HexEditor.Model;

namespace HexEditor.Core.Model;

public abstract class ContentTypeDefinition(
	string type,
	string? baseType = null
)
{
	public string Type { get; } = type;
	public string? BaseType { get; } = baseType;

	public abstract ValueTask<bool> MatchesAsync(string? filePath, IBinaryDataSource source, CancellationToken cancellationToken);

	protected static bool MatchByFileName(string? filePath, string fileName, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
	{
		if (filePath == null)
		{
			return false;
		}

		var actualFileName = Path.GetFileName(filePath);
		return string.Equals(actualFileName, fileName, comparison);
	}

	protected static bool MatchByExtension(string? filePath, params ReadOnlySpan<string> extensions)
	{
		if (filePath == null)
		{
			return false;
		}

		var fileExtension = Path.GetExtension(filePath);
		if (fileExtension == null)
		{
			return false;
		}

		foreach (var extension in extensions)
		{
			if (string.Equals(fileExtension, extension, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}
}
