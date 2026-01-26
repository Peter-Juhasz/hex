using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace HexEditor.WinUI.Theming;

public record class VisualTheme(
	FontFamily FontFamily,
	int Columns = 16,
	double FontSize = 14,
	double FontWidth = 8.25d,
	double RowHeight = 24,
	IReadOnlyDictionary<string, WinUITextRunStyle>? ClassificationStyleMap = null,
	WinUITextRunStyle? HyperlinkStyle = null,
	Brush? Background = null,
	Brush? Foreground = null,
	HexViewStyle? HexViewStyle = null,
	AsciiViewStyle? AsciiViewStyle = null,
	AddressMarginStyle? AddressMarginStyle = null
)
{
	public static double FontSizeToWidth(double fontSize) => fontSize * (8.25d / 14d);
}

public record class AddressMarginStyle(
	FontFamily? FontFamily = null,
	Brush? Background = null,
	Brush? Foreground = null
);

public record class HexViewStyle(
	FontFamily? FontFamily = null,
	Brush? Background = null,
	Brush? Foreground = null,
	int? PrimaryGrouping = null,
	int? SecondaryGrouping = null
);

public record class AsciiViewStyle(
	FontFamily? FontFamily = null,
	Brush? Background = null,
	Brush? Foreground = null,
	int? PrimaryGrouping = null,
	int? SecondaryGrouping = null
);