using Microsoft.UI.Xaml.Media;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HexEditor.WinUI.Theming;

internal class FontFamilyJsonConverter : JsonConverter<FontFamily>
{
	public override FontFamily? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var fontName = reader.GetString();
		if (fontName == null)
		{
			return null;
		}

		return new FontFamily("Consolas");
	}

	public override void Write(Utf8JsonWriter writer, FontFamily value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.Source);
	}
}
