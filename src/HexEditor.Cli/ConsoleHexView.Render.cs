namespace HexEditor.ViewModel;

internal partial class ConsoleHexView
{
	private int CalculateBytesPerRow(int windowWidth)
	{
		var usableWidth = windowWidth - (
			// Address
			(Theme?.AddressMargin?.Visible == false ? 0 : (
				(Theme?.AddressMargin?.Margin?.Left ?? 0) +
				(Theme?.AddressMargin?.Border?.Left != null ? 1 : 0) +
				(Theme?.AddressMargin?.Padding?.Left ?? 0) +
				Math.Max(MinimumAddressLength, Theme?.AddressMargin?.MinimumWidth ?? 0) +
				(Theme?.AddressMargin?.Padding?.Right ?? 0) +
				(Theme?.AddressMargin?.Border?.Right != null ? 1 : 0) +
				(Theme?.AddressMargin?.Margin?.Right ?? 0)
			)) +

			// Hex
			(Theme?.HexView?.Visible == false ? 0 : (
				(Theme?.HexView?.Margin?.Left ?? 0) +
				(Theme?.HexView?.Border?.Left != null ? 1 : 0) +
				(Theme?.HexView?.Padding?.Left ?? 0) +
				(Theme?.HexView?.Padding?.Right ?? 0) +
				(Theme?.HexView?.Border?.Right != null ? 1 : 0) +
				(Theme?.HexView?.Margin?.Right ?? 0)
			)) +

			// ASCII
			(Theme?.AsciiView?.Visible == false ? 0 : (
				(Theme?.AsciiView?.Margin?.Left ?? 0) +
				(Theme?.AsciiView?.Border?.Left != null ? 1 : 0) +
				(Theme?.AsciiView?.Padding?.Left ?? 0) +
				(Theme?.AsciiView?.Padding?.Right ?? 0) +
				(Theme?.AsciiView?.Border?.Right != null ? 1 : 0) +
				(Theme?.AsciiView?.Margin?.Right ?? 0)
			)) +

			// scrollbar
			(Theme?.Scrollbar == null ? 0 : (
				(Theme?.Scrollbar?.Margin?.Left ?? 0) +
				1 + // Scrollbar width
				(Theme?.Scrollbar?.Margin?.Right ?? 0)
			)) +

			// Padding
			(Theme?.Padding?.Left ?? 0) +
			(Theme?.Padding?.Right ?? 0)
		);
		return (int)MathF.Floor(usableWidth / (
			1 + // Space
			(Theme?.HexView?.Visible == false ? 0 : (
				2 + // Hex byte
				(Theme?.HexView?.ColumnGroupingSize is int grouping ? 1f / grouping : 0) // Extra spaces for grouping
			)) +
			(Theme?.AsciiView?.Visible == false ? 0 : (
				1 // ASCII representation
			))
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
			for (int screenRowIndex = 0; screenRowIndex < RowsPerScreen; screenRowIndex++)
			{
				var dataRowIndex = FirstVisibleRowIndex + screenRowIndex;

				RenderSpacing(Theme?.Padding?.Left);

				// Data row
				if (dataRowIndex <= LastVisibleRowIndex)
				{
					if (TryGetRow(dataRowIndex, out var row))
					{
						RenderRow(row);
					}
				}
				else
				{
					RenderEmptyRow();
				}

				// Scrollbar
				if (Theme?.Scrollbar != null)
				{
					RenderSpacing(Theme.Scrollbar.Margin?.Left);

					if (screenRowIndex >= VerticalScrollbarThumbScreenRowStartIndex && screenRowIndex <= VerticalScrollbarThumbScreenRowStartIndex + VerticalScrollbarThumbScreenRowHeight)
					{
						using (UseStyle(Theme.Scrollbar.ThumbStyle))
						{
							Console.Write('█');
						}
					}
					else
					{
						using (UseStyle(Theme.Scrollbar.TrackStyle))
						{
							Console.Write('|');
						}
					}

					RenderSpacing(Theme.Scrollbar.Margin?.Right);
				}

				RenderSpacing(Theme?.Padding?.Right);

				// new line
				if (screenRowIndex < Rows - 1)
				{
					Console.WriteLine();
				}
			}
		}

		Console.ResetColor();
	}

	private static readonly string[] HexFormatStrings = [ "X0", "X1", "X2", "X3", "X4", "X5", "X6", "X7", "X8", "X9", "X10", "X11", "X12", "X13", "X14", "X15", "X16" ];

	private void RenderRow(IViewRow row)
	{
		var writer = Console.Out;

		Span<char> formatBuffer = stackalloc char[16];

		// Address
		var addressStyle = Theme?.AddressMargin;
		if (addressStyle?.Visible != false)
		{
			RenderSpacing(addressStyle?.Margin?.Left);
			RenderVerticalBorder(addressStyle?.Border?.Left);
			RenderSpacing(addressStyle?.Padding?.Left);
			using (UseStyle(addressStyle?.TextStyle))
			{
				var addressLength = Math.Max(MinimumAddressLength, addressStyle?.MinimumWidth ?? 0);
				row.Offset.TryFormat(formatBuffer, out _, HexFormatStrings[addressLength]);
				writer.Write(formatBuffer[..addressLength]);
			}
			RenderSpacing(addressStyle?.Margin?.Right);
			RenderVerticalBorder(addressStyle?.Border?.Right);
			RenderSpacing(addressStyle?.Padding?.Right);
		}

		var data = row.Data;

		// Hex
		var hexViewStyle = Theme?.HexView;
		if (hexViewStyle?.Visible != false)
		{
			RenderSpacing(hexViewStyle?.Margin?.Left);
			RenderVerticalBorder(hexViewStyle?.Border?.Left);
			RenderSpacing(hexViewStyle?.Padding?.Left);
			using (UseStyle(Theme?.HexView?.TextStyle))
			{
				int col;
				for (col = 0; col < data.Length; col++)
				{
					// get value
					byte value = data[col];

					// determine formatting
					using (UseStyle(MatchRule(value, new(
						Offset: row.Offset + col,
						Row: row.RowIndex,
						Column: col
					))))
					{
						// write hex value
						value.TryFormat(formatBuffer, out _, "X2");
						writer.Write(formatBuffer[..2]);
					}

					// separator
					if (col < data.Length - 1)
					{
						writer.Write(' ');

						if (Theme?.HexView?.ColumnGroupingSize is int groupingSize)
						{
							if ((col + 1) % groupingSize == 0)
							{
								writer.Write(' ');
							}
						}
					}
				}

				// fill remaining space
				if (data.Length < Columns)
				{
					var writtenCharacters = CalculateHexRenderLength(data.Length);
					var totalRenderLength = CalculateHexRenderLength(Columns);

					Span<char> emptyBuffer = stackalloc char[totalRenderLength - writtenCharacters];
					emptyBuffer.Fill(' ');
					writer.Write(emptyBuffer);
				}
			}
			RenderSpacing(hexViewStyle?.Padding?.Right);
			RenderVerticalBorder(hexViewStyle?.Border?.Right);
			RenderSpacing(hexViewStyle?.Margin?.Right);
		}

		// ASCII
		var asciiViewStyle = Theme?.AsciiView;
		if (asciiViewStyle?.Visible != false)
		{
			RenderSpacing(asciiViewStyle?.Margin?.Left);
			RenderVerticalBorder(asciiViewStyle?.Border?.Left);
			RenderSpacing(asciiViewStyle?.Padding?.Left);
			using (UseStyle(asciiViewStyle?.TextStyle))
			{
				int col;
				for (col = 0; col < data.Length; col++)
				{
					// get value
					byte value = data[col];

					// determine formatting
					using (UseStyle(MatchRule(value, new(
						Offset: row.Offset + col,
						Row: row.RowIndex,
						Column: col
					))))
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

				// fill remaining space
				if (data.Length < Columns)
				{
					Span<char> emptyBuffer = stackalloc char[Columns - data.Length];
					emptyBuffer.Fill(' ');
					writer.Write(emptyBuffer);
				}
			}
			RenderSpacing(asciiViewStyle?.Padding?.Right);
			RenderVerticalBorder(asciiViewStyle?.Border?.Right);
			RenderSpacing(asciiViewStyle?.Margin?.Right);
		}
	}

	private void RenderEmptyRow()
	{
		var writer = Console.Out;

		Span<char> formatBuffer = stackalloc char[16];

		// Address
		var addressStyle = Theme?.AddressMargin;
		if (addressStyle?.Visible != false)
		{
			RenderSpacing(addressStyle?.Margin?.Left);
			RenderVerticalBorder(addressStyle?.Border?.Left);
			RenderSpacing(addressStyle?.Padding?.Left);
			using (UseStyle(addressStyle?.TextStyle))
			{
				Span<char> emptyAddress = stackalloc char[MinimumAddressLength];
				emptyAddress.Fill(' ');
				writer.Write(emptyAddress);
			}
			RenderSpacing(addressStyle?.Margin?.Right);
			RenderVerticalBorder(addressStyle?.Border?.Right);
			RenderSpacing(addressStyle?.Padding?.Right);
		}

		// Hex
		var hexViewStyle = Theme?.HexView;
		if (hexViewStyle?.Visible != false)
		{
			RenderSpacing(hexViewStyle?.Margin?.Left);
			RenderVerticalBorder(hexViewStyle?.Border?.Left);
			RenderSpacing(hexViewStyle?.Padding?.Left);
			using (UseStyle(Theme?.HexView?.TextStyle))
			{
				var totalRenderLength = CalculateHexRenderLength(Columns);
				Span<char> emptyBuffer = stackalloc char[totalRenderLength];
				emptyBuffer.Fill(' ');
				writer.Write(emptyBuffer);
			}
			RenderSpacing(hexViewStyle?.Padding?.Right);
			RenderVerticalBorder(hexViewStyle?.Border?.Right);
			RenderSpacing(hexViewStyle?.Margin?.Right);
		}

		// ASCII
		var asciiViewStyle = Theme?.AsciiView;
		if (asciiViewStyle?.Visible != false)
		{
			RenderSpacing(asciiViewStyle?.Margin?.Left);
			RenderVerticalBorder(asciiViewStyle?.Border?.Left);
			RenderSpacing(asciiViewStyle?.Padding?.Left);
			using (UseStyle(asciiViewStyle?.TextStyle))
			{
				Span<char> emptyBuffer = stackalloc char[Columns];
				emptyBuffer.Fill(' ');
				writer.Write(emptyBuffer);
			}
			RenderSpacing(asciiViewStyle?.Padding?.Right);
			RenderVerticalBorder(asciiViewStyle?.Border?.Right);
			RenderSpacing(asciiViewStyle?.Margin?.Right);
		}
	}

	private int CalculateHexRenderLength(int bytes) =>
		bytes * 2 + // Hex digits
		(bytes - 1) + // Spaces between bytes
		(Theme?.HexView?.ColumnGroupingSize is int groupingSize ? (bytes - 1) / groupingSize : 0) // Extra spaces for grouping
	;

	private ConsoleStyle? MatchRule(byte value, ValueFormattingRule.Context context)
	{
		if (Theme?.FormattingRules == null)
		{
			return null;
		}

		foreach (var rule in Theme.FormattingRules)
		{
			if (rule.IsMatch(value, context))
			{
				return rule.Style;
			}
		}

		return null;
	}

	private static void RenderSpacing(int? length)
	{
		if (length is null or 0)
		{
			return;
		}

		Span<char> buffer = stackalloc char[length.Value];
		buffer.Fill(' ');
		Console.Write(buffer);
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
				BorderPattern.Dotted => ':',
				BorderPattern.Dashes => '¦',
				BorderPattern.Solid => '|',
				BorderPattern.Double => '║',
				BorderPattern.Full => '█',
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