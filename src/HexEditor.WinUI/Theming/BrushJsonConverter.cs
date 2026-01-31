using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.UI;

namespace HexEditor.WinUI.Theming;

internal class BrushJsonConverter(ResourceDictionary? resources) : JsonConverter<Brush>
{
	public override Brush? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var resourceKey = reader.GetString();
		if (resourceKey == null)
		{
			return null;
		}

		if (resourceKey.StartsWith('#'))
		{
			switch (resourceKey.Length)
			{
				case 4:
					{
						Span<byte> bytes =
						[
							(byte)(FromHexCharacter(resourceKey[1]) * 16),
							(byte)(FromHexCharacter(resourceKey[2]) * 16),
							(byte)(FromHexCharacter(resourceKey[3]) * 16),
						];
						return new SolidColorBrush(Color.FromArgb(255, bytes[0], bytes[1], bytes[2]));
					}

				case 7:
					{
						Span<byte> bytes = stackalloc byte[3];
						Convert.FromHexString(resourceKey.AsSpan(1), bytes, out _, out _);
						return new SolidColorBrush(Color.FromArgb(255, bytes[0], bytes[1], bytes[2]));
					}

				case 9:
					{
						Span<byte> bytes = stackalloc byte[4];
						Convert.FromHexString(resourceKey.AsSpan(1), bytes, out _, out _);
						return new SolidColorBrush(Color.FromArgb(bytes[0], bytes[1], bytes[2], bytes[3]));
					}

				default:
					return null;
			}
		}

		if (resources == null || !resources.TryGetValue(resourceKey, out var resource) || resource is not Brush brush)
		{
			return null;
		}

		if (typeof(Colors).GetProperty(resourceKey, BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase) is PropertyInfo colorProperty)
		{
			var color = (Color)colorProperty.GetValue(null)!;
			brush = new SolidColorBrush(color);
		}

		return brush;
	}

	private static byte FromHexCharacter(char ch) => (byte)(ch switch
	{
		>= '0' and <= '9' => ch - '0',
		>= 'a' and <= 'f' => ch - 'a' + 10,
		>= 'A' and <= 'F' => ch - 'A' + 10,
		_ => 0,
	});

	public override void Write(Utf8JsonWriter writer, Brush value, JsonSerializerOptions options)
	{
		throw new NotSupportedException();
	}
}