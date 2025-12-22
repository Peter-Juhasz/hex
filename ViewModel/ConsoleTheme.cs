namespace HexEditor.ViewModel;

internal record class ConsoleTheme(
	IReadOnlyList<ConsoleFormattingRule> FormattingRules,
	AddressAreaStyle? AddressBar = null,
	HexViewStyle? HexView = null,
	AsciiViewStyle? AsciiView = null,
	ConsoleStyle? DefaultStyle = null
);

internal record class ConsoleFormattingRule(
	byte LowValue,
	byte HighValue,
	ConsoleStyle Style
)
{
	public bool IsMatch(byte value) => value >= LowValue && value <= HighValue;
}

internal record class ConsoleStyle(
	ConsoleColor? ForegroundColor = null,
	ConsoleColor? BackgroundColor = null
);

internal enum BorderPattern
{
	Dotted,
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

internal record class AddressAreaStyle(
	ConsoleStyle? TextStyle = null,
	FullBorderStyle? Border = null,
	Spacing? Padding = null,
	Spacing? Margin = null
);

internal record class HexViewStyle(
	ConsoleStyle? TextStyle = null,
	FullBorderStyle? Border = null,
	Spacing? Padding = null,
	Spacing? Margin = null,
	int? GroupingSize = null
);

internal record class AsciiViewStyle(
	ConsoleStyle? TextStyle = null,
	FullBorderStyle? Border = null,
	Spacing? Padding = null,
	Spacing? Margin = null
);

internal record class Spacing(
	int Left = 0,
	int Right = 0
);

internal record class FullBorderStyle(
	BorderStyle? Left = null,
	BorderStyle? Right = null
);

internal static class Themes
{
	public static readonly ConsoleTheme Dark = new(
		AddressBar: new(
			Border: new(
				Right: new(
					Pattern: BorderPattern.Dotted
				)
			)
		),
		AsciiView: new(
			Border: new(
				Left: new(
					Pattern: BorderPattern.Solid
				)
			),
			Padding: new(Left: 1)
		),
		HexView: new(
			Padding: new(Left: 1, Right: 1),
			GroupingSize: 4
		),
		FormattingRules:
		[
			new ConsoleFormattingRule(0x00, 0x1F, new(ForegroundColor: ConsoleColor.DarkGray)),
			new ConsoleFormattingRule(0x20, 0x7E, new(ForegroundColor: ConsoleColor.White)),
		]
	);
}
