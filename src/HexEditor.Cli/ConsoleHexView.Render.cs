namespace HexEditor.ViewModel;

internal partial class ConsoleHexView
{
	private int CalculateBytesPerRow(int windowWidth)
	{
		var usableWidth = windowWidth - (
			// Address
			(_theme?.AddressMargin?.Visible == false ? 0 : (
				(_theme?.AddressMargin?.Margin?.Left ?? 0) +
				(_theme?.AddressMargin?.Border?.Left != null ? 1 : 0) +
				(_theme?.AddressMargin?.Padding?.Left ?? 0) +
				Math.Max(MinimumAddressLength, _theme?.AddressMargin?.MinimumWidth ?? 0) +
				(_theme?.AddressMargin?.ShowSuffix == true ? 1 : 0) +
				(_theme?.AddressMargin?.Padding?.Right ?? 0) +
				(_theme?.AddressMargin?.Border?.Right != null ? 1 : 0) +
				(_theme?.AddressMargin?.Margin?.Right ?? 0)
			)) +

			// Hex
			(_theme?.HexView?.Visible == false ? 0 : (
				(_theme?.HexView?.Margin?.Left ?? 0) +
				(_theme?.HexView?.Border?.Left != null ? 1 : 0) +
				(_theme?.HexView?.Padding?.Left ?? 0) +
				(_theme?.HexView?.Padding?.Right ?? 0) +
				(_theme?.HexView?.Border?.Right != null ? 1 : 0) +
				(_theme?.HexView?.Margin?.Right ?? 0)
			)) +

			// ASCII
			(_theme?.AsciiView?.Visible == false ? 0 : (
				(_theme?.AsciiView?.Margin?.Left ?? 0) +
				(_theme?.AsciiView?.Border?.Left != null ? 1 : 0) +
				(_theme?.AsciiView?.Padding?.Left ?? 0) +
				(_theme?.AsciiView?.Padding?.Right ?? 0) +
				(_theme?.AsciiView?.Border?.Right != null ? 1 : 0) +
				(_theme?.AsciiView?.Margin?.Right ?? 0)
			)) +

			// scrollbar
			(_theme?.Scrollbar == null ? 0 : (
				(_theme?.Scrollbar?.Margin?.Left ?? 0) +
				1 + // Scrollbar width
				(_theme?.Scrollbar?.Margin?.Right ?? 0)
			)) +

			// Padding
			(_theme?.Padding?.Left ?? 0) +
			(_theme?.Padding?.Right ?? 0)
		);
		return (int)MathF.Floor(usableWidth / (
			1 + // Space
			(_theme?.HexView?.Visible == false ? 0 : (
				2 + // Hex byte
				(_theme?.HexView?.ColumnGroupingSize is int grouping ? 1f / grouping : 0) // Extra spaces for grouping
			)) +
			(_theme?.AsciiView?.Visible == false ? 0 : (
				1 + // ASCII representation
				(_theme?.AsciiView?.ColumnGroupingSize is int grouping2 ? 1f / grouping2 : 0) // Extra spaces for grouping
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

		using (UseStyle(_theme?.DefaultStyle))
		{
			// top padding
			if (_theme?.Padding?.Top is int topPadding)
			{
				for (int i = 0; i < topPadding; i++)
				{
					Console.WriteLine();
				}
			}

			// column headers
			if (_theme?.HexView?.Visible != false && _theme?.HexView?.Header?.Visible == true)
			{
				RenderSpacing(_theme?.Padding?.Left);
				RenderHeader();
				RenderSpacing(_theme?.Padding?.Right);
				Console.WriteLine();
			}

			// data rows
			for (int screenRowIndex = 0; screenRowIndex < RowsPerScreen; screenRowIndex++)
			{
				var dataRowIndex = FirstVisibleRowIndex + screenRowIndex;

				RenderSpacing(_theme?.Padding?.Left);

				// data row
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

				// scrollbar
				if (_theme?.Scrollbar != null)
				{
					RenderSpacing(_theme.Scrollbar.Margin?.Left);

					if (screenRowIndex >= VerticalScrollbarThumbScreenRowStartIndex && screenRowIndex <= VerticalScrollbarThumbScreenRowStartIndex + VerticalScrollbarThumbScreenRowHeight)
					{
						using (UseStyle(_theme.Scrollbar.ThumbStyle))
						{
							Console.Write('█');
						}
					}
					else
					{
						using (UseStyle(_theme.Scrollbar.TrackStyle))
						{
							Console.Write('|');
						}
					}

					RenderSpacing(_theme.Scrollbar.Margin?.Right);
				}

				RenderSpacing(_theme?.Padding?.Right);

				// new line
				if (screenRowIndex < Rows - 1)
				{
					Console.WriteLine();
				}
			}

			// bottom padding
			if (_theme?.Padding?.Bottom is int bottomPadding)
			{
				for (int i = 0; i < bottomPadding; i++)
				{
					Console.WriteLine();
				}
			}
		}

		Console.ResetColor();
	}

	private void RenderHeader()
	{
		var addressStyle = _theme?.AddressMargin;
		if (addressStyle?.Visible != false)
		{
			RenderSpacing(addressStyle?.Margin?.Left);
			RenderVerticalBorder(addressStyle?.Border?.Left);
			RenderSpacing(addressStyle?.Padding?.Left);
			using (UseStyle(addressStyle?.TextStyle))
			{
				var addressLength = Math.Max(MinimumAddressLength, addressStyle?.MinimumWidth ?? 0);
				RenderSpacing(addressLength);
			}
			RenderSpacing(addressStyle?.Margin?.Right);
			RenderVerticalBorder(addressStyle?.Border?.Right);
			RenderSpacing(addressStyle?.Padding?.Right);
		}

		var hexViewStyle = _theme?.HexView;
		if (hexViewStyle?.Visible != false)
		{
			RenderSpacing(hexViewStyle?.Margin?.Left);
			RenderVerticalBorder(hexViewStyle?.Border?.Left);
			RenderSpacing(hexViewStyle?.Padding?.Left);
			using (UseStyle(_theme?.HexView?.TextStyle))
			using (UseStyle(_theme?.HexView?.Header?.Style))
			{
				Span<char> formatBuffer = stackalloc char[2];
				for (int col = 0; col < Columns; col++)
				{
					col.TryFormat(formatBuffer, out _, "X2");
					Console.Write(formatBuffer[..2]);
					// separator
					if (col < Columns - 1)
					{
						Console.Write(' ');
						if (_theme?.HexView?.ColumnGroupingSize is int groupingSize)
						{
							if ((col + 1) % groupingSize == 0)
							{
								Console.Write(' ');
							}
						}
					}
				}
			}
			RenderSpacing(hexViewStyle?.Padding?.Right);
			RenderVerticalBorder(hexViewStyle?.Border?.Right);
			RenderSpacing(hexViewStyle?.Margin?.Right);
		}

		var asciiViewStyle = _theme?.AsciiView;
		if (asciiViewStyle?.Visible != false)
		{
			RenderSpacing(asciiViewStyle?.Margin?.Left);
			RenderVerticalBorder(asciiViewStyle?.Border?.Left);
			RenderSpacing(asciiViewStyle?.Padding?.Left);
			using (UseStyle(asciiViewStyle?.TextStyle))
			{
				RenderSpacing(Columns);
			}
			RenderSpacing(asciiViewStyle?.Padding?.Right);
			RenderVerticalBorder(asciiViewStyle?.Border?.Right);
			RenderSpacing(asciiViewStyle?.Margin?.Right);
		}
	}

	private static readonly string[] HexFormatStrings = [ "X0", "X1", "X2", "X3", "X4", "X5", "X6", "X7", "X8", "X9", "X10", "X11", "X12", "X13", "X14", "X15", "X16" ];

	private void RenderRow(IViewRow row)
	{
		var writer = Console.Out;

		Span<char> formatBuffer = stackalloc char[16];

		// Address
		var addressStyle = _theme?.AddressMargin;
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

				if (addressStyle?.ShowSuffix == true)
				{
					writer.Write('h');
				}
			}
			RenderSpacing(addressStyle?.Margin?.Right);
			RenderVerticalBorder(addressStyle?.Border?.Right);
			RenderSpacing(addressStyle?.Padding?.Right);
		}

		var data = row.Data;

		// Hex
		var hexViewStyle = _theme?.HexView;
		if (hexViewStyle?.Visible != false)
		{
			RenderSpacing(hexViewStyle?.Margin?.Left);
			RenderVerticalBorder(hexViewStyle?.Border?.Left);
			RenderSpacing(hexViewStyle?.Padding?.Left);
			using (UseStyle(_theme?.HexView?.TextStyle))
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

						if (_theme?.HexView?.ColumnGroupingSize is int groupingSize)
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
		var asciiViewStyle = _theme?.AsciiView;
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

					// separator
					if (col < data.Length - 1)
					{
						if (_theme?.AsciiView?.ColumnGroupingSize is int groupingSize)
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

		// Address
		var addressStyle = _theme?.AddressMargin;
		if (addressStyle?.Visible != false)
		{
			RenderSpacing(addressStyle?.Margin?.Left);
			RenderVerticalBorder(addressStyle?.Border?.Left);
			RenderSpacing(addressStyle?.Padding?.Left);
			using (UseStyle(addressStyle?.TextStyle))
			{
				var addressLength = Math.Max(MinimumAddressLength, addressStyle?.MinimumWidth ?? 0);
				Span<char> emptyAddress = stackalloc char[addressLength];
				emptyAddress.Fill(' ');
				writer.Write(emptyAddress);
			}
			RenderSpacing(addressStyle?.Margin?.Right);
			RenderVerticalBorder(addressStyle?.Border?.Right);
			RenderSpacing(addressStyle?.Padding?.Right);
		}

		// Hex
		var hexViewStyle = _theme?.HexView;
		if (hexViewStyle?.Visible != false)
		{
			RenderSpacing(hexViewStyle?.Margin?.Left);
			RenderVerticalBorder(hexViewStyle?.Border?.Left);
			RenderSpacing(hexViewStyle?.Padding?.Left);
			using (UseStyle(_theme?.HexView?.TextStyle))
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
		var asciiViewStyle = _theme?.AsciiView;
		if (asciiViewStyle?.Visible != false)
		{
			RenderSpacing(asciiViewStyle?.Margin?.Left);
			RenderVerticalBorder(asciiViewStyle?.Border?.Left);
			RenderSpacing(asciiViewStyle?.Padding?.Left);
			using (UseStyle(asciiViewStyle?.TextStyle))
			{
				Span<char> emptyBuffer = stackalloc char[Columns + 
					(_theme?.AsciiView?.ColumnGroupingSize is int groupingSize ? (Columns - 1) / groupingSize : 0)
				];
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
		(_theme?.HexView?.ColumnGroupingSize is int groupingSize ? (bytes - 1) / groupingSize : 0) // Extra spaces for grouping
	;

	private ConsoleStyle? MatchRule(byte value, ValueFormattingRule.Context context)
	{
		if (_rules == null)
		{
			return null;
		}

		foreach (var rule in _rules)
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
				BorderPattern.Dot => '.',
				BorderPattern.DotDot => ':',
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