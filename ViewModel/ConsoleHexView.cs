using System;
using System.Diagnostics.CodeAnalysis;

namespace HexEditor.ViewModel;

public class ConsoleHexView(IViewBuffer viewBuffer) : IHexView
{
	private int WindowWidth = -1;
	private int WindowHeight = -1;
	private int BytesPerLine = -1;

	private int GroupingSize = 4;

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
		var bytesPerLine = BytesPerLine;
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

	public int LineCount => (int)((viewBuffer.DataBuffer.Length + BytesPerLine - 1) / BytesPerLine);

	private long _lineIndex = 0;

	public long FirstVisibleLineIndex => _lineIndex;

	public long LastVisibleLineIndex => _lineIndex + VisibleLineCount - 1;

	public long FirstVisibleOffset => _lineIndex * BytesPerLine;

	public long LastVisibleOffset => FirstVisibleOffset + Math.Min(viewBuffer.DataBuffer.Length - FirstVisibleOffset, WindowHeight * BytesPerLine);

	public int VisibleLineCount => Math.Min((int)(LineCount - _lineIndex), WindowHeight);

	public int VisibleByteCount => (int)(LastVisibleOffset - FirstVisibleOffset);

	public Task ResizeWindowAsync(int newWindowWidth, int newWindowHeight, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newWindowWidth);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newWindowHeight);

		if (newWindowWidth == WindowWidth && newWindowHeight == WindowHeight)
		{
			return Task.CompletedTask;
		}

		var oldLineOffset = _lineIndex;

		WindowWidth = newWindowWidth;
		WindowHeight = newWindowHeight;
		BytesPerLine = CalculateBytesPerLine(newWindowWidth);
		
		return viewBuffer.LoadChunkAsync(FirstVisibleOffset, VisibleByteCount, cancellationToken);
	}

	public Task PageDownAsync(CancellationToken cancellationToken)
	{
		if (VisibleLineCount < WindowHeight)
		{
			return Task.CompletedTask;
		}

		_lineIndex += WindowHeight;
		return viewBuffer.LoadChunkAsync(FirstVisibleOffset, VisibleByteCount, cancellationToken);
	}

	public Task PageUpAsync(CancellationToken cancellationToken)
	{
		if (LineCount < WindowHeight)
		{
			return Task.CompletedTask;
		}

		_lineIndex = Math.Max(0, _lineIndex - WindowHeight);
		return viewBuffer.LoadChunkAsync(FirstVisibleOffset, VisibleByteCount, cancellationToken);
	}

	public Task ScrollUpAsync(CancellationToken cancellationToken)
	{
		if (_lineIndex == 0)
		{
			return Task.CompletedTask;
		}

		_lineIndex = Math.Max(0, _lineIndex - 1);
		return viewBuffer.LoadChunkAsync(FirstVisibleOffset, VisibleByteCount, cancellationToken);
	}

	public Task ScrollDownAsync(CancellationToken cancellationToken)
	{
		if (VisibleLineCount < WindowHeight)
		{
			return Task.CompletedTask;
		}

		_lineIndex++;
		return viewBuffer.LoadChunkAsync(FirstVisibleOffset, VisibleByteCount, cancellationToken);
	}


	public void Render()
	{
		if (BytesPerLine == -1)
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
		Span<char> screenBuffer = stackalloc char[WindowWidth];
		var writer = new SpanWriter<char>(screenBuffer);

		// Address
		var addressLength = 8;
		line.Offset.TryFormat(writer.GetSpan(addressLength), out _, "X8");
		writer.Advance(addressLength);
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

			data[col].TryFormat(writer.GetSpan(2), out _, "X2");
			writer.Advance(2);
		}

		writer.Write(" | ");

		// ASCII
		for (int col = 0; col < data.Length; col++)
		{
			var b = data[col];
			if (b >= 32 && b <= 126)
			{
				writer.Write((char)b);
			}
			else
			{
				writer.Write('.');
			}
		}

		writer.Write(Environment.NewLine);

		var output = Console.Out;
		output.Write(writer.WrittenSpan);
	}
}