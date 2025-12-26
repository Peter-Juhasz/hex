using System.Diagnostics.CodeAnalysis;

namespace HexEditor.ViewModel;

internal partial class ConsoleHexView : IHexView
{
    public ConsoleHexView(IViewBuffer viewBuffer)
    {
        this.viewBuffer = viewBuffer;
        MinimumAddressLength = CalculateRequiredAddressLengthInCharacters(viewBuffer.DataBuffer.Length);
		SetThemeCore(Themes.Dark);
    }

    private int Columns = -1;
	private int Rows = -1;

	private int MinimumAddressLength;

    private int VerticalScrollbarThumbScreenRowHeight = -1;
	private int VerticalScrollbarThumbScreenRowStartIndex = -1;

	private ConsoleTheme? _theme;
	private ValueFormattingRule[]? _rules = null;

	public ConsoleTheme? Theme => _theme;

    public bool TryGetRow(long index, [NotNullWhen(true)] out IViewRow? row)
	{
		var bytesPerRow = Columns;
		var offset = index * bytesPerRow;
		if (offset >= viewBuffer.DataBuffer.Length)
		{
			row = null;
			return false;
		}

		var length = (int)Math.Min(bytesPerRow, viewBuffer.DataBuffer.Length - offset);
		if (!viewBuffer.TryRead(offset, length, out var data))
		{
			row = null;
			return false;
		}

		row = new ViewRow(this, index, offset, length, data);
		return true;
	}

	public int TotalRowCount => (int)((viewBuffer.DataBuffer.Length + Columns - 1) / Columns);

	private long _rowIndex = 0;
    private readonly IViewBuffer viewBuffer;

    public long FirstVisibleRowIndex => _rowIndex;

	public long LastVisibleRowIndex => _rowIndex + VisibleRowCount - 1;

	public long FirstVisibleOffset => _rowIndex * Columns;

	public long LastVisibleOffset => FirstVisibleOffset + Math.Min(viewBuffer.DataBuffer.Length - FirstVisibleOffset, Rows * Columns);

	public int VisibleRowCount => Math.Min((int)(TotalRowCount - _rowIndex), Rows);

	public int VisibleByteCount => (int)(LastVisibleOffset - FirstVisibleOffset);

	public int BytesPerScreen => VisibleRowCount * Columns;

	public int RowsPerScreen => Rows;

	public int LastPageIndex => Math.Max(0, (TotalRowCount - 1) / Rows);

	public int LastPageRowIndex => LastPageIndex * Rows;

	public Task ResizeWindowAsync(int newWindowWidth, int newWindowHeight, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newWindowWidth);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newWindowHeight);

		var newRows = _theme?.Rows ?? newWindowHeight - (
			(_theme?.Padding?.Top ?? 0) +
			(_theme?.HexView?.Header?.Visible ?? _theme?.AsciiView?.Header?.Visible == true ? 1 : 0) +
			(_theme?.Padding?.Bottom ?? 0)
		);
		var newColumns = _theme?.Columns ?? CalculateBytesPerRow(newWindowWidth);
		return ResizeAsync(newColumns: newColumns, newRows: newRows, cancellationToken);
	}

	public Task ResizeAsync(int newColumns, int newRows, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newColumns);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newRows);

		Columns = newColumns;
		Rows = newRows;

		VerticalScrollbarThumbScreenRowHeight = Math.Max(1, (int)((RowsPerScreen / (double)TotalRowCount) * RowsPerScreen));
		VerticalScrollbarThumbScreenRowStartIndex = (int)((FirstVisibleRowIndex / (double)TotalRowCount) * RowsPerScreen);

		return LoadAndInvalidateAsync(cancellationToken);
	}

	public Task ApplyThemeAsync(ConsoleTheme? newTheme, CancellationToken cancellationToken)
    {
        SetThemeCore(newTheme);

        return ResizeAsync(
            newColumns: newTheme?.Columns ?? CalculateBytesPerRow(Console.WindowWidth),
            newRows: newTheme?.Rows ?? Console.WindowHeight,
            cancellationToken
        );
    }

    private void SetThemeCore(ConsoleTheme? newTheme)
    {
        _theme = newTheme;
		_rules = newTheme switch
		{
			{ FormattingRules: { Count: > 0 } rules } => rules.ToArray(),
			_ => null
        };
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

		return ScrollToRowAsync(_rowIndex + Rows, cancellationToken);
	}

	public Task PageUpAsync(CancellationToken cancellationToken)
	{
		if (TotalRowCount < Rows)
		{
			return Task.CompletedTask;
		}

		if (_rowIndex == 0)
		{
			return Task.CompletedTask;
		}

		var currentPageIndex = _rowIndex / Rows;
		return ScrollToPageAsync(Math.Max(0, currentPageIndex - 1), cancellationToken);
	}

	public Task ScrollUpAsync(CancellationToken cancellationToken)
	{
		return ScrollToRowAsync(Math.Max(0, _rowIndex - 1), cancellationToken);
	}

	public Task ScrollDownAsync(CancellationToken cancellationToken)
	{
		if (VisibleRowCount < Rows)
		{
			return Task.CompletedTask;
		}

		return ScrollToRowAsync(_rowIndex + 1, cancellationToken);
	}

	public Task GoToFirstPageAsync(CancellationToken cancellationToken)
	{
		if (_rowIndex == 0)
		{
			return Task.CompletedTask;
		}

		return ScrollToPageAsync(0, cancellationToken);
	}

	public Task GoToLastPageAsync(CancellationToken cancellationToken)
	{
		return ScrollToPageAsync(LastPageIndex, cancellationToken);
	}

	public Task ScrollToPageAsync(long pageIndex, CancellationToken cancellationToken)
	{
		if (pageIndex < 0 || pageIndex > LastPageIndex)
		{
			throw new ArgumentOutOfRangeException(nameof(pageIndex));
		}

		var targetRowIndex = pageIndex * Rows;
		return ScrollToRowAsync(targetRowIndex, cancellationToken);
	}

	public Task ScrollToRowAsync(long rowIndex, CancellationToken cancellationToken)
	{
		if (rowIndex < 0 || rowIndex >= TotalRowCount)
		{
			throw new ArgumentOutOfRangeException(nameof(rowIndex));
		}

		if (_rowIndex == rowIndex)
		{
			return Task.CompletedTask;
		}

		_rowIndex = rowIndex;
		VerticalScrollbarThumbScreenRowStartIndex = (int)((FirstVisibleRowIndex / (double)TotalRowCount) * RowsPerScreen);
		return LoadAndInvalidateAsync(cancellationToken);
	}

	private static int CalculateRequiredAddressLengthInCharacters(long dataLength) => (int)Math.Ceiling(Math.Log(dataLength + 1, 16));
}