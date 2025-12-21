using System.Diagnostics.CodeAnalysis;

namespace HexEditor.ViewModel;

public class ConsoleHexView(IViewBuffer viewBuffer) : IHexView
{
	private int Columns = -1;
	private int Rows = -1;

	private int GroupingSize = 4;
	private int AddressLength = viewBuffer.DataBuffer.Length <= 0xFFFFFFFF ? 8 : 16;

	private ConsoleFormattingRule[] FormattingRules =
	[
		new ConsoleFormattingRule(0x00, 0x1F, ConsoleColor.DarkGray),
		new ConsoleFormattingRule(0x20, 0x7E, ConsoleColor.White),
	];

	private int CalculateBytesPerLine(int windowWidth)
	{
		var usableWidth = windowWidth - (
			4 * 2 + // Address
			1 +     // Separator
			3 +     // Spaces between HEX and ASCII
			0
		);
		return (int)MathF.Floor(usableWidth / (
			1 + // Space
			2 + // Hex byte
			(1f / GroupingSize) + // Extra spaces for grouping
			1 // ASCII representation
		));
	}

	public bool TryGetLine(long index, [NotNullWhen(true)] out IViewLine? line)
	{
		var bytesPerLine = Columns;
		var offset = index * bytesPerLine;
		if (offset >= viewBuffer.DataBuffer.Length)
		{
			line = null;
			return false;
		}

		var length = (int)Math.Min(bytesPerLine, viewBuffer.DataBuffer.Length - offset);
		if (!viewBuffer.TryRead(offset, length, out var data))
		{
			line = null;
			return false;
		}

		line = new ViewLine(this, index, offset, length, data);
		return true;
	}

	public int LineCount => (int)((viewBuffer.DataBuffer.Length + Columns - 1) / Columns);

	private long _lineIndex = 0;

	public long FirstVisibleLineIndex => _lineIndex;

	public long LastVisibleLineIndex => _lineIndex + VisibleLineCount - 1;

	public long FirstVisibleOffset => _lineIndex * Columns;

	public long LastVisibleOffset => FirstVisibleOffset + Math.Min(viewBuffer.DataBuffer.Length - FirstVisibleOffset, Rows * Columns);

	public int VisibleLineCount => Math.Min((int)(LineCount - _lineIndex), Rows);

	public int VisibleByteCount => (int)(LastVisibleOffset - FirstVisibleOffset);

	public int VisibleBytesPerScreen => VisibleLineCount * Columns;

	public int LastPageIndex => LineCount / Rows * Rows;

	public Task ResizeWindowAsync(int newWindowWidth, int newWindowHeight, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newWindowWidth);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newWindowHeight);

		var newRows = newWindowHeight;
		var newColumns = CalculateBytesPerLine(newWindowWidth);
		return ResizeAsync(newColumns: newColumns, newRows: newRows, cancellationToken);
	}

	public Task ResizeAsync(int newColumns, int newRows, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newColumns);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newRows);

		if (newColumns == Columns && newRows == Rows)
		{
			return Task.CompletedTask;
		}

		Columns = newColumns;
		Rows = newRows;

		return LoadAndInvalidateAsync(cancellationToken);
	}

	private async Task LoadAndInvalidateAsync(CancellationToken cancellationToken)
	{
		await viewBuffer.LoadChunkAsync(FirstVisibleOffset, VisibleByteCount, cancellationToken);
		Render();
	}

	public Task PageDownAsync(CancellationToken cancellationToken)
	{
		if (VisibleLineCount < Rows)
		{
			return Task.CompletedTask;
		}

		_lineIndex += Rows;
		return LoadAndInvalidateAsync(cancellationToken);
	}

	public Task PageUpAsync(CancellationToken cancellationToken)
	{
		if (LineCount < Rows)
		{
			return Task.CompletedTask;
		}

		if (_lineIndex == 0)
		{
			return Task.CompletedTask;
		}

		_lineIndex = Math.Max(0, _lineIndex - Rows);
		return LoadAndInvalidateAsync(cancellationToken);
	}

	public Task ScrollUpAsync(CancellationToken cancellationToken)
	{
		if (_lineIndex == 0)
		{
			return Task.CompletedTask;
		}

		_lineIndex = Math.Max(0, _lineIndex - 1);
		return LoadAndInvalidateAsync(cancellationToken);
	}

	public Task ScrollDownAsync(CancellationToken cancellationToken)
	{
		if (VisibleLineCount < Rows)
		{
			return Task.CompletedTask;
		}

		_lineIndex++;
		return LoadAndInvalidateAsync(cancellationToken);
	}

	public Task GoToFirstPageAsync(CancellationToken cancellationToken)
	{
		if (_lineIndex == 0)
		{
			return Task.CompletedTask;
		}

		_lineIndex = 0;
		return LoadAndInvalidateAsync(cancellationToken);
	}

	public Task GoToLastPageAsync(CancellationToken cancellationToken)
	{
		var lastPageIndex = LastPageIndex;
		if (_lineIndex == lastPageIndex)
		{
			return Task.CompletedTask;
		}

		_lineIndex = lastPageIndex;
		return LoadAndInvalidateAsync(cancellationToken);
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
		
		for (long lineIndex = FirstVisibleLineIndex; lineIndex <= LastVisibleLineIndex; lineIndex++)
		{
			if (TryGetLine(lineIndex, out var line))
			{
				RenderLine(line);
			}
		}
	}

	private void RenderLine(IViewLine line)
	{
		var writer = Console.Out;

		Span<char> formatBuffer = stackalloc char[16];

		// Address
		line.Offset.TryFormat(formatBuffer, out _, AddressLength switch { 8 => "X8", _ => "X16" });
		writer.Write(formatBuffer[..AddressLength]);
		writer.Write(':');

		// Hex
		var data = line.Data;
		for (int col = 0; col < data.Length; col++)
		{
			writer.Write(' ');

			if (col > 0 && col % GroupingSize == 0)
			{
				writer.Write(' ');
			}

			// get value
			var value = data[col];

			// determine formatting
			ConsoleFormattingRule? effectiveRule = null;
			if (FormattingRules?.Length > 0)
			{
				foreach (var rule in FormattingRules)
				{
					if (rule.IsMatch(value))
					{
						effectiveRule = rule;
						break;
					}
				}
			}

			if (effectiveRule != null)
			{
				if (effectiveRule.ForegroundColor != null)
				{
					Console.ForegroundColor = effectiveRule.ForegroundColor.Value;
				}

				if (effectiveRule.BackgroundColor != null)
				{
					Console.BackgroundColor = effectiveRule.BackgroundColor.Value;
				}
			}

			// write hex value
			value.TryFormat(formatBuffer, out _, "X2");
			writer.Write(formatBuffer[..2]);

			// reset formatting
			if (effectiveRule != null)
			{
				Console.ResetColor();
			}
		}

		writer.Write(" | ");

		// ASCII
		for (int col = 0; col < data.Length; col++)
		{
			// get value
			var value = data[col];

			// determine formatting
			ConsoleFormattingRule? effectiveRule = null;
			if (FormattingRules?.Length > 0)
			{
				foreach (var rule in FormattingRules)
				{
					if (rule.IsMatch(value))
					{
						effectiveRule = rule;
						break;
					}
				}
			}

			if (effectiveRule != null)
			{
				if (effectiveRule.ForegroundColor != null)
				{
					Console.ForegroundColor = effectiveRule.ForegroundColor.Value;
				}

				if (effectiveRule.BackgroundColor != null)
				{
					Console.BackgroundColor = effectiveRule.BackgroundColor.Value;
				}
			}

			// write character or dot
			if (value >= 32 && value <= 126)
			{
				writer.Write((char)value);
			}
			else
			{
				writer.Write('.');
			}

			// reset formatting
			if (effectiveRule != null)
			{
				Console.ResetColor();
			}
		}

		writer.Write(Environment.NewLine);
	}
}