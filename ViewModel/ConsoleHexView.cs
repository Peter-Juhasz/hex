using System.Diagnostics.CodeAnalysis;

namespace HexEditor.ViewModel;

internal partial class ConsoleHexView(IViewBuffer viewBuffer) : IHexView
{
	private int Columns = -1;
	private int Rows = -1;

	private int AddressLength = viewBuffer.DataBuffer.Length <= 0xFFFFFFFF ? 8 : 16;

	private ConsoleTheme? Theme = Themes.Dark;

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

	public Task ApplyThemeAsync(ConsoleTheme? newTheme, CancellationToken cancellationToken)
	{
		Theme = newTheme;

		return ResizeAsync(
			newColumns: newTheme?.Columns ?? CalculateBytesPerLine(Console.WindowWidth),
			newRows: newTheme?.Rows ?? Console.WindowHeight,
			cancellationToken
		);
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
}