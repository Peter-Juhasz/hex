using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace HexEditor.WinUI.Theming;

public record class VisualTheme(
	FontFamily FontFamily,
	int Columns = 16,
	double FontSize = 14,
	double FontWidth = 8.25d,
	double RowHeight = 20,
	IReadOnlyDictionary<string, TextRunStyle>? ClassificationMap = null,
	TextRunStyle? HyperlinkStyle = null,
	Brush? Background = null,
	Brush? Foreground = null,
	HexViewStyle? HexView = null,
	AsciiViewStyle? AsciiView = null,
	AddressMarginStyle? AddressMargin = null,
	ShapeStyle? RowHighlight = null,
	ShapeStyle? SelectionHighlight = null,
	ShapeStyle? ColumnHighlight = null,
	ShapeStyle? ReferenceHighlight = null,
	ShapeStyle? OutliningRegionHighlight = null
)
{
	public static double FontSizeToWidth(FontFamily fontFamily, double fontSize)
	{
		var tb = new TextBlock
		{
			Text = "MMMMMMMMMM",
			FontFamily = fontFamily,
			FontSize = fontSize,
		};
		tb.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
		double charWidth = tb.DesiredSize.Width / 10;
		return charWidth;
	}
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
	int? SecondaryGrouping = null,
	ShapeStyle? SelectionHighlight = null,
	ShapeStyle? ColumnHighlight = null,
	ShapeStyle? ReferenceHighlight = null,
	ShapeStyle? OutliningRegionHighlight = null
);

public record class AsciiViewStyle(
	FontFamily? FontFamily = null,
	Brush? Background = null,
	Brush? Foreground = null,
	int? PrimaryGrouping = null,
	int? SecondaryGrouping = null,
	ShapeStyle? SelectionHighlight = null,
	ShapeStyle? ColumnHighlight = null,
	ShapeStyle? ReferenceHighlight = null,
	ShapeStyle? OutliningRegionHighlight = null
);

public record class ShapeStyle(
	Brush? Fill = null,
	Brush? Stroke = null,
	double? StrokeThickness = null,
	double? Opacity = null
);