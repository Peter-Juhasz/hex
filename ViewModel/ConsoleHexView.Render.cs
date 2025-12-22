namespace HexEditor.ViewModel;

public partial class ConsoleHexView
{
	private int CalculateBytesPerLine(int windowWidth)
	{
		var usableWidth = windowWidth - (
			// Address
			(Theme?.AddressBar?.Margin?.Left ?? 0) +
			(Theme?.AddressBar?.Border?.Left != null ? 1 : 0) +
			(Theme?.AddressBar?.Padding?.Left ?? 0) +
			AddressLength +
			(Theme?.AddressBar?.Padding?.Right ?? 0) +
			(Theme?.AddressBar?.Border?.Right != null ? 1 : 0) +
			(Theme?.AddressBar?.Margin?.Right ?? 0) +

			// Hex
			(Theme?.HexView?.Margin?.Left ?? 0) +
			(Theme?.HexView?.Border?.Left != null ? 1 : 0) +
			(Theme?.HexView?.Padding?.Left ?? 0) +
			(Theme?.HexView?.Padding?.Right ?? 0) +
			(Theme?.HexView?.Border?.Right != null ? 1 : 0) +
			(Theme?.HexView?.Margin?.Right ?? 0) +

			// ASCII
			(Theme?.AsciiView?.Margin?.Left ?? 0) +
			(Theme?.AsciiView?.Border?.Left != null ? 1 : 0) +
			(Theme?.AsciiView?.Padding?.Left ?? 0) +
			(Theme?.AsciiView?.Padding?.Right ?? 0) +
			(Theme?.AsciiView?.Border?.Right != null ? 1 : 0) +
			(Theme?.AsciiView?.Margin?.Right ?? 0)
		);
		return (int)MathF.Floor(usableWidth / (
			1 + // Space
			2 + // Hex byte
			(1f / GroupingSize) + // Extra spaces for grouping
			1 // ASCII representation
		));
	}

	public void Render()
	{
		if (Columns == -1)
		{
			throw new InvalidOperationException("View has not been initialized. Call ResizeAsync first.");
		}

		Console.Clear();

		if (viewBuffer.DataBuffer.Length == 0)
		{
			return;
		}

		using (UseStyle(Theme?.DefaultStyle))
		{
			for (long lineIndex = FirstVisibleLineIndex; lineIndex <= LastVisibleLineIndex; lineIndex++)
			{
				if (TryGetLine(lineIndex, out var line))
				{
					RenderLine(line);
				}
			}
		}

		Console.ResetColor();
	}

	private void RenderLine(IViewLine line)
	{
		var writer = Console.Out;

		Span<char> formatBuffer = stackalloc char[16];

		// Address
		var addressStyle = Theme?.AddressBar;
		RenderSpacing(addressStyle?.Margin?.Left);
		RenderVerticalBorder(addressStyle?.Border?.Left);
		RenderSpacing(addressStyle?.Padding?.Left);
		using (UseStyle(addressStyle?.TextStyle))
		{
			line.Offset.TryFormat(formatBuffer, out _, AddressLength switch
			{
				2 => "X2",
				4 => "X4",
				8 => "X8",
				_ => "X16" 
			});
			writer.Write(formatBuffer[..AddressLength]);
		}
		RenderSpacing(addressStyle?.Margin?.Right);
		RenderVerticalBorder(addressStyle?.Border?.Right);
		RenderSpacing(addressStyle?.Padding?.Right);

		var data = line.Data;

		// Hex
		var hexViewStyle = Theme?.HexView;
		RenderSpacing(hexViewStyle?.Margin?.Left);
		RenderVerticalBorder(hexViewStyle?.Border?.Left);
		RenderSpacing(hexViewStyle?.Padding?.Left);
		using (UseStyle(Theme?.HexView?.TextStyle))
		{
			for (int col = 0; col < data.Length; col++)
			{
				// get value
				byte value = data[col];

				// determine formatting
				using (UseStyle(MatchRule(value)))
				{
					// write hex value
					value.TryFormat(formatBuffer, out _, "X2");
					writer.Write(formatBuffer[..2]);
				}

				// separator
				if (col < data.Length - 1)
				{
					writer.Write(' ');

					if ((col + 1) % GroupingSize == 0)
					{
						writer.Write(' ');
					}
				}
			}
		}
		RenderSpacing(hexViewStyle?.Padding?.Right);
		RenderVerticalBorder(hexViewStyle?.Border?.Right);
		RenderSpacing(hexViewStyle?.Margin?.Right);

		// ASCII
		var asciiViewStyle = Theme?.AsciiView;
		RenderSpacing(asciiViewStyle?.Margin?.Left);
		RenderVerticalBorder(asciiViewStyle?.Border?.Left);
		RenderSpacing(asciiViewStyle?.Padding?.Left);
		using (UseStyle(asciiViewStyle?.TextStyle))
		{
			for (int col = 0; col < data.Length; col++)
			{
				// get value
				byte value = data[col];

				// determine formatting
				using (UseStyle(MatchRule(value)))
				{
					// write character or dot
					if (value >= 32 && value <= 126)
					{
						writer.Write((char)value);
					}
					else
					{
						writer.Write('.');
					}
				}
			}
		}
		RenderSpacing(asciiViewStyle?.Padding?.Right);
		RenderVerticalBorder(asciiViewStyle?.Border?.Right);
		RenderSpacing(asciiViewStyle?.Margin?.Right);

		writer.Write(Environment.NewLine);
	}

	private ConsoleStyle? MatchRule(byte value)
	{
		if (Theme?.FormattingRules == null)
		{
			return null;
		}
		foreach (var rule in Theme.FormattingRules)
		{
			if (rule.IsMatch(value))
			{
				return rule.Style;
			}
		}
		return null;
	}

	private static void RenderSpacing(int? length)
	{
		if (length == null)
		{
			return;
		}

		for (int i = 0; i < length; i++)
		{
			Console.Write(' ');
		}
	}

	private static void RenderVerticalBorder(BorderStyle? style)
	{
		if (style == null)
		{
			return;
		}

		using (UseStyle(style))
		{
			Console.Write(style.Pattern switch
			{
				BorderPattern.Dotted => ":",
				BorderPattern.Dashes => "¦",
				BorderPattern.Solid  => "|",
				BorderPattern.Double => "║",
				BorderPattern.Full   => "█",
				_ => throw new ArgumentOutOfRangeException()
			});
		}
	}

	private static StyleState UseStyle(ConsoleStyle? style)
	{
		var captured = CaptureStyle();
		if (style == null)
		{
			return captured;
		}

		if (style != null)
		{
			if (style.ForegroundColor != null)
			{
				Console.ForegroundColor = style.ForegroundColor.Value;
			}
			if (style.BackgroundColor != null)
			{
				Console.BackgroundColor = style.BackgroundColor.Value;
			}
		}

		return captured;
	}

	private static StyleState CaptureStyle() => new(
		foregroundColor: Console.ForegroundColor,
		backgroundColor: Console.BackgroundColor
	);

	private readonly ref struct StyleState(
		ConsoleColor foregroundColor,
		ConsoleColor backgroundColor
	) : IDisposable
	{
		private readonly ConsoleColor _foregroundColor = foregroundColor;
		private readonly ConsoleColor _backgroundColor = backgroundColor;

		public void Dispose()
		{
			Console.ForegroundColor = _foregroundColor;
			Console.BackgroundColor = _backgroundColor;
		}
	}
}