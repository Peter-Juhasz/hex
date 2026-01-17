using HexEditor.Classification;
using HexEditor.Core.Tagging;
using HexEditor.Model;
using System.Collections.Immutable;

namespace HexEditor.ViewModel;

internal partial class ConsoleHexView : IHexView
{
    public ConsoleHexView(IBinarySnapshot viewBuffer)
    {
        _viewBuffer = viewBuffer;
        MinimumAddressLength = CalculateRequiredAddressLengthInCharacters(viewBuffer.Length);
		SetThemeCore(Themes.Dark);

		_classificationAggregator = new EmptyTagger<ClassificationTag>();
    }

	private const double RowHeight = 1d;

    private int Columns = -1;
	private int Rows = -1;
	private long _totalRowCount = -1;

    private int MinimumAddressLength;

    private int VerticalScrollbarThumbScreenRowHeight = -1;
	private int VerticalScrollbarThumbScreenRowStartIndex = -1;

	private ConsoleTheme? _theme;
	private ImmutableArray<ValueFormattingRule> _rules = [];
	private ImmutableArray<IHexViewRow> _visibleRows = [];

	public ConsoleTheme? Theme => _theme;

	public ImmutableArray<IHexViewRow> VisibleRows => _visibleRows;
	public event EventHandler<VisibleRowsChangedEventArgs>? VisibleRowsChanged;
	public event EventHandler<HeightChangedEventArgs>? ScrollableHeightChanged;

	public double ViewportHeight => Console.BufferHeight;

	public double ViewportWidth => Console.BufferWidth;

	public double ScrollableHeight => TotalRowCount * RowHeight;

	private readonly ITagger<ClassificationTag> _classificationAggregator;

	public async Task<IHexViewRow?> TryGetRow(long index, CancellationToken cancellationToken)
	{
		// adjust index with grouping
		if (_theme?.RowGroupingSize is int groupingSize)
		{
			if ((index + 1) % (groupingSize + 1) == 0)
			{
				return null;
			}

			index -= index / (groupingSize + 1);
		}

		var bytesPerRow = Columns;
		var offset = index * bytesPerRow;
		if (offset >= _viewBuffer.Length)
		{
			return null;
		}

		var length = (int)Math.Min(bytesPerRow, _viewBuffer.Length - offset);
		var rowSpan = new LongSpan(offset, length);
		var snapshotSpan = new SnapshotSpan(_viewBuffer, rowSpan);
		var data = new byte[length];
		await snapshotSpan.CopyToAsync(data, cancellationToken);

		var classifications = await _classificationAggregator.GetTagsAsync(snapshotSpan, cancellationToken);

		var formatted = Format(new(
			Span: snapshotSpan,
			Data: data,
			Rules: _rules,
			Classifications: classifications
		));

		// TODO: add padding to calculation
		var row = new HexViewRow(this, new(Left: 0, Top: index, Width: Console.BufferWidth, Height: RowHeight), snapshotSpan, data, formatted, formatted);
		return row;
	}

	public long TotalRowCount => _totalRowCount;

    private long _rowIndex = 0;
    private readonly IBinarySnapshot _viewBuffer;

    public long FirstVisibleRowIndex => _rowIndex;

	public long LastVisibleRowIndex => _rowIndex + VisibleRowCount - 1;

    public long FirstVisibleOffset
    {
        get
        {
			var dataRowIndex = _rowIndex;

			if (_theme?.RowGroupingSize is int groupingSize)
			{
				var groupsBefore = dataRowIndex / (groupingSize);
				dataRowIndex -= groupsBefore;
            }

            return dataRowIndex * Columns;
        }
    }

    public long LastVisibleOffset => FirstVisibleOffset + Math.Min(_viewBuffer.Length - FirstVisibleOffset, Rows * Columns);

	public int VisibleRowCount => Math.Min((int)(TotalRowCount - _rowIndex), Rows);

	public int VisibleByteCount => (int)(LastVisibleOffset - FirstVisibleOffset);

	public int BytesPerScreen => VisibleRowCount * Columns;

	public int RowsPerScreen => Rows;

	public long LastPageIndex => Math.Max(0, (TotalRowCount - 1) / Rows);

	public long LastPageRowIndex => LastPageIndex * Rows;

	public SnapshotSpan VisibleSpan => new(_viewBuffer, new(FirstVisibleOffset, VisibleByteCount));

    public Task ResizeWindowAsync(double viewportWidth, double viewportHeight, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(viewportWidth);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(viewportHeight);

		var newWindowWidth = (int)Math.Floor(viewportWidth);
		var newWindowHeight = (int)Math.Floor(viewportHeight);

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

		var oldHeight = ScrollableHeight;
		Columns = newColumns;
		Rows = newRows;
		_totalRowCount = CalculateTotalRows(newRows);

		VerticalScrollbarThumbScreenRowHeight = Math.Max(1, (int)((RowsPerScreen / (double)TotalRowCount) * RowsPerScreen));
		VerticalScrollbarThumbScreenRowStartIndex = (int)((FirstVisibleRowIndex / (double)TotalRowCount) * RowsPerScreen);

		ScrollableHeightChanged?.Invoke(this, new(oldHeight, newHeight: ScrollableHeight));

		return LoadAndInvalidateAsync(cancellationToken);
	}

	public Task ApplyThemeAsync(ConsoleTheme? newTheme, CancellationToken cancellationToken)
    {
        SetThemeCore(newTheme);

        return ResizeAsync(
            newColumns: newTheme?.Columns ?? CalculateBytesPerRow(Console.BufferWidth),
            newRows: newTheme?.Rows ?? Console.BufferHeight,
            cancellationToken
        );
    }

    private void SetThemeCore(ConsoleTheme? newTheme)
    {
        _theme = newTheme;
		_rules = newTheme switch
		{
			{ FormattingRules: { Count: > 0 } rules } => rules.ToImmutableArray(),
			_ => []
        };
    }

    private async Task LoadAndInvalidateAsync(CancellationToken cancellationToken)
	{
		var visibleSpan = VisibleSpan;
		await InvalidateAsync(cancellationToken);
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

	public Task ScrollUpByRowAsync(CancellationToken cancellationToken)
	{
		return ScrollToRowAsync(Math.Max(0, _rowIndex - 1), cancellationToken);
	}

	public Task ScrollDownByRowAsync(CancellationToken cancellationToken)
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