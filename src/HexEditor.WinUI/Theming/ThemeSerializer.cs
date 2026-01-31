using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using System.Collections.Frozen;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace HexEditor.WinUI.Theming;

public class ThemeSerializer(string themesFolderPath)
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1869:Cache and reuse 'JsonSerializerOptions' instances", Justification = "<Pending>")]
	public async Task<VisualTheme> DeserializeAsync(string name, CancellationToken cancellationToken)
	{
		// read resources
		var xamlFilePath = Path.Combine(themesFolderPath, $"{name}.xaml");
		ResourceDictionary? resources = null;
		try
		{
			var xaml = await File.ReadAllTextAsync(xamlFilePath, cancellationToken);
			resources = (ResourceDictionary)XamlReader.Load(xaml);
		}
		catch (FileNotFoundException)
		{ }

		// read theme
		var jsonFilePath = Path.Combine(themesFolderPath, $"{name}.json");
		var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
		{
			Converters =
			{
				new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
				new FontFamilyJsonConverter(),
				new BrushJsonConverter(resources),
			},
			AllowTrailingCommas = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
			NumberHandling = JsonNumberHandling.AllowReadingFromString,
		};
		var bytes = await File.ReadAllBytesAsync(jsonFilePath, cancellationToken);
		var theme = JsonSerializer.Deserialize<VisualTheme>(bytes, jsonSerializerOptions) ?? // this work needs to be done on UI thread
			throw new InvalidDataException($"Failed to deserialize theme from '{jsonFilePath}'.");

		// optimize
		theme = theme with
		{
			ClassificationMap = theme.ClassificationMap?.ToFrozenDictionary(),
			FontWidth = VisualTheme.FontSizeToWidth(theme.FontFamily, theme.FontSize),
		};

		return theme;
	}
}
