namespace HexEditor.ViewModel;

internal record class ConsoleFormattingRule(
	byte LowValue,
	byte HighValue,
	ConsoleColor? ForegroundColor = null,
	ConsoleColor? BackgroundColor = null
)
{
	public bool IsMatch(byte value) => value >= LowValue && value <= HighValue;
}
