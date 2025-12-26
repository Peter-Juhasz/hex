using System.Text.Json.Serialization;

namespace HexEditor.ViewModel;

internal record class ConsoleTheme(
	IReadOnlyCollection<ValueFormattingRule> FormattingRules,
	AddressMarginStyle? AddressMargin = null,
	HexViewStyle? HexView = null,
	AsciiViewStyle? AsciiView = null,
	ConsoleStyle? DefaultStyle = null,
	int? Columns = null,
	int? Rows = null,
	ScrollbarStyle? Scrollbar = null,
	Spacing? Padding = null,
	int? RowGroupingSize = null
);

internal record class ValueFormattingRule(
	ConsoleStyle Style,
	byte? MinimumValue = null,
	byte? MaximumValue = null,
	long? MinimumOffset = null,
	long? MaximumOffset = null,
	int? MinimumColumn = null,
	int? MaximumColumn = null,
	int? MinimumRow = null,
	int? MaximumRow = null
)
{
	public bool IsMatch(byte value, Context context)
	{
		if (MinimumValue != null)
		{
			if (value < MinimumValue)
			{
				return false;
			}
		}

		if (MaximumValue != null)
		{
			if (value > MaximumValue)
			{
				return false;
			}
		}

		if (MinimumOffset != null)
		{
			if (context.Offset < MinimumOffset)
			{
				return false;
			}
		}

		if (MaximumOffset != null)
		{
			if (context.Offset > MaximumOffset)
			{
				return false;
			}
		}

		if (MinimumColumn != null)
		{
			if (context.Column < MinimumColumn)
			{
				return false;
			}
		}

		if (MaximumColumn != null)
		{
			if (context.Column > MaximumColumn)
			{
				return false;
			}
		}

		if (MinimumRow != null)
		{
			if (context.Row < MinimumRow)
			{
				return false;
			}
		}

		if (MaximumRow != null)
		{
			if (context.Row > MaximumRow)
			{
				return false;
			}
		}

		return true;
	}

	public record struct Context(
		long Offset,
		long Row,
		int Column
	);
}

internal record class ConsoleStyle(
	ConsoleColor? ForegroundColor = null,
	ConsoleColor? BackgroundColor = null
);

internal enum BorderPattern
{
	Dot,
	DotDot,
	Dashes,
	Solid,
	Double,
	Full,
}

internal record class BorderStyle(
	ConsoleColor? ForegroundColor = null,
	ConsoleColor? BackgroundColor = null,
	BorderPattern? Pattern = null
) : ConsoleStyle(ForegroundColor, BackgroundColor);

internal record class AddressMarginStyle(
	ConsoleStyle? TextStyle = null,
	FullBorderStyle? Border = null,
	HorizontalSpacing? Padding = null,
	HorizontalSpacing? Margin = null,
	bool Visible = true,
	int? MinimumWidth = 8,
	bool ShowSuffix = false,
	LetterCasing LetterCasing = LetterCasing.Upper
);

internal record class HexViewStyle(
	ConsoleStyle? TextStyle = null,
	FullBorderStyle? Border = null,
	HorizontalSpacing? Padding = null,
	HorizontalSpacing? Margin = null,
	int? ColumnGroupingSize = null,
	bool Visible = true,
	HexColumnHeaderStyle? Header = null,
    LetterCasing LetterCasing = LetterCasing.Upper
);

internal record class HexColumnHeaderStyle(
	ConsoleStyle? Style = null,
	bool Visible = true,
    LetterCasing LetterCasing = LetterCasing.Upper
);

internal record class AsciiViewStyle(
	ConsoleStyle? TextStyle = null,
	FullBorderStyle? Border = null,
	HorizontalSpacing? Padding = null,
	HorizontalSpacing? Margin = null,
	bool Visible = true,
	int? ColumnGroupingSize = null,
    AsciiColumnHeaderStyle? Header = null
);

internal record class AsciiColumnHeaderStyle(
    ConsoleStyle? Style = null,
    bool Visible = true,
    LetterCasing LetterCasing = LetterCasing.Upper
);

internal record class HorizontalSpacing(
	int Left = 0,
	int Right = 0
);

internal record class Spacing(
	int Left = 0,
	int Right = 0,
	int Top = 0,
	int Bottom = 0
);

internal record class FullBorderStyle(
	BorderStyle? Left = null,
	BorderStyle? Right = null
);

internal record class ScrollbarStyle(
	ConsoleStyle? TrackStyle = null,
	ConsoleStyle? ThumbStyle = null,
	HorizontalSpacing? Margin = null
);

internal enum LetterCasing
{
	Upper,
	Lower
}

internal static class Themes
{
	public static readonly ConsoleTheme Dark = new(
		AddressMargin: new(
			Border: new(
				Right: new(
					Pattern: BorderPattern.DotDot
				)
			)
		),
		HexView: new(
			Padding: new(Left: 1, Right: 1),
			ColumnGroupingSize: 4
		),
		Scrollbar: new(
			Margin: new(Left: 1)
		),
		AsciiView: new(
			Border: new(
				Left: new(
					Pattern: BorderPattern.Solid
				)
			),
			Padding: new(Left: 1),
			ColumnGroupingSize: 4,
			Header: new(Visible: true)
		),
		FormattingRules:
		[
			new(MinimumValue: 0x00, MaximumValue: 0x1F, Style: new(ForegroundColor: ConsoleColor.DarkGray)),
			new(MinimumValue: 0x20, MaximumValue: 0x7E, Style: new(ForegroundColor: ConsoleColor.White)),
		]
	);

	public static readonly ConsoleTheme Midnight = new(
		Columns: 20,
		AddressMargin: new(
			Border: new(
			),
			TextStyle: new(BackgroundColor: ConsoleColor.DarkBlue)
		),
		HexView: new(
			Padding: new(Left: 3, Right: 3),
			ColumnGroupingSize: 5
		),
		Scrollbar: new(
			Margin: new(Left: 1)
		),
		AsciiView: new(
			Border: new(
				Left: new(
					Pattern: BorderPattern.Dashes
				)
			),
			Padding: new(Left: 1),
			ColumnGroupingSize: 5
		),
		FormattingRules:
		[
			new(MinimumValue: 0x00, MaximumValue: 0x1F, Style: new(ForegroundColor: ConsoleColor.DarkGray)),
			new(MinimumValue: 0x20, MaximumValue: 0x7E, Style: new(ForegroundColor: ConsoleColor.Yellow)),
			new(MinimumValue: 0x80, MaximumValue: 0xFF, Style: new(ForegroundColor: ConsoleColor.Red)),
		],
		Padding: new(2, 2, 2, 2),
		RowGroupingSize: 5
	);
}


[JsonSerializable(typeof(ConsoleTheme))]
[JsonSourceGenerationOptions(
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
	WriteIndented = false,
	UseStringEnumConverter = true,
	IgnoreReadOnlyFields = true,
	IncludeFields = false
)]
internal partial class ConsoleThemeJsonSerializerContext : JsonSerializerContext { }