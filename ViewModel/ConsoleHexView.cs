using System.Diagnostics.CodeAnalysis;

namespace HexEditor.ViewModel;

internal partial class ConsoleHexView(IViewBuffer viewBuffer) : IHexView
{
	private int Columns = -1;
	private int Rows = -1;

	private int AddressLength = viewBuffer.DataBuffer.Length <= 0xFFFFFFFF ? 8 : 16;

	private int VerticalScrollbarThumbScreenRowHeight = -1;
	private int VerticalScrollbarThumbScreenRowStartIndex = -1;

	private ConsoleTheme? Theme = Themes.Dark;

	public bool TryGetRow(long index, [NotNullWhen(true)] out IViewRow? row)
	{
		var bytesPerLine = Columns;
		var offset = index * bytesPerLine;
		if (offset >= viewBuffer.DataBuffer.Length)
		{
			row = null;
			return false;
		}

		var length = (int)Math.Min(bytesPerLine, viewBuffer.DataBuffer.Length - offset);
		if (!viewBuffer.TryRead(offset, length, out var data))
		{
			row = null;
			return false;
		}

		row = new ViewRow(this, index, offset, length, data);
		return true;
	}

	public int RowCount => (int)((viewBuffer.DataBuffer.Length + Columns - 1) / Columns);

	private long _rowIndex = 0;

	public long FirstVisibleRowIndex => _rowIndex;

	public long LastVisibleRowIndex => _rowIndex + VisibleRowCount - 1;

	public long FirstVisibleOffset => _rowIndex * Columns;

	public long LastVisibleOffset => FirstVisibleOffset + Math.Min(viewBuffer.DataBuffer.Length - FirstVisibleOffset, Rows * Columns);

	public int VisibleRowCount => Math.Min((int)(RowCount - _rowIndex), Rows);

	public int VisibleByteCount => (int)(LastVisibleOffset - FirstVisibleOffset);

	public int VisibleBytesPerScreen => VisibleRowCount * Columns;

	public int RowsPerScreen => Rows;

	public int LastPageIndex => RowCount / Rows * Rows;

	public Task ResizeWindowAsync(int newWindowWidth, int newWindowHeight, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newWindowWidth);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newWindowHeight);

		var newRows = newWindowHeight;
		var newColumns = CalculateBytesPerRow(newWindowWidth);
		return ResizeAsync(newColumns: newColumns, newRows: newRows, cancellationToken);
	}

	public Task ResizeAsync(int newColumns, int newRows, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newColumns);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newRows);

		Columns = newColumns;
		Rows = newRows;

		VerticalScrollbarThumbScreenRowHeight = Math.Max(1, (int)((RowsPerScreen / (double)RowCount) * RowsPerScreen));
		VerticalScrollbarThumbScreenRowStartIndex = (int)((FirstVisibleRowIndex / (double)RowCount) * RowsPerScreen);

		return LoadAndInvalidateAsync(cancellationToken);
	}

	public Task ApplyThemeAsync(ConsoleTheme? newTheme, CancellationToken cancellationToken)
	{
		Theme = newTheme;

		return ResizeAsync(
			newColumns: newTheme?.Columns ?? CalculateBytesPerRow(Console.WindowWidth),
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
		if (VisibleRowCount < Rows)
		{
			return Task.CompletedTask;
		}

		ScrollTo(_rowIndex + Rows);
		return LoadAndInvalidateAsync(cancellationToken);
	}

	public Task PageUpAsync(CancellationToken cancellationToken)
	{
		if (RowCount < Rows)
		{
			return Task.CompletedTask;
		}

		if (_rowIndex == 0)
		{
			return Task.CompletedTask;
		}

		ScrollTo(Math.Max(0, _rowIndex - Rows));
		return LoadAndInvalidateAsync(cancellationToken);
	}

	public Task ScrollUpAsync(CancellationToken cancellationToken)
	{
		if (_rowIndex == 0)
		{
			return Task.CompletedTask;
		}

		ScrollTo(Math.Max(0, _rowIndex - 1));
		return LoadAndInvalidateAsync(cancellationToken);
	}

	public Task ScrollDownAsync(CancellationToken cancellationToken)
	{
		if (VisibleRowCount < Rows)
		{
			return Task.CompletedTask;
		}

		ScrollTo(_rowIndex + 1);
		return LoadAndInvalidateAsync(cancellationToken);
	}

	public Task GoToFirstPageAsync(CancellationToken cancellationToken)
	{
		if (_rowIndex == 0)
		{
			return Task.CompletedTask;
		}

		ScrollTo(0);
		return LoadAndInvalidateAsync(cancellationToken);
	}

	public Task GoToLastPageAsync(CancellationToken cancellationToken)
	{
		var lastPageIndex = LastPageIndex;
		if (_rowIndex == lastPageIndex)
		{
			return Task.CompletedTask;
		}

		ScrollTo(lastPageIndex);
		return LoadAndInvalidateAsync(cancellationToken);
	}

	private void ScrollTo(long lineIndex)
	{
		_rowIndex = lineIndex;
		VerticalScrollbarThumbScreenRowStartIndex = (int)((FirstVisibleRowIndex / (double)RowCount) * RowsPerScreen);
	}
}