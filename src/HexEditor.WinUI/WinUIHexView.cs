using HexEditor.Model;
using HexEditor.ViewModel;
using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace HexEditor.WinUI;

internal class WinUIHexView(IBinarySnapshot snapshot, VisualTheme theme) : IHexView
{
	private ImmutableArray<IHexViewRow> _visibleRows = [];

	public ImmutableArray<IHexViewRow> VisibleRows => _visibleRows;
	public long TotalRowCount { get; }

	public double ViewportHeight { get; private set; }
	public double ViewportWidth { get; private set; }
	public double VerticalOffset { get; private set; }

	public double ScrollableHeight { get; private set; } = theme.RowHeight;

	public event EventHandler<VisibleRowsChangedEventArgs>? VisibleRowsChanged;

	public event EventHandler<HeightChangedEventArgs>? ScrollableHeightChanged;

	private VisualTheme _visualTheme = theme;

	internal async Task InvalidateAsync(IBinarySnapshot snapshot, CancellationToken cancellationToken)
	{
		var visibleRowCount = (int)(ViewportHeight / _visualTheme.RowHeight) + 2;
		var firstVisibleRowIndex = (int)(VerticalOffset / _visualTheme.RowHeight);
		var firstVisibleOffset = firstVisibleRowIndex * _visualTheme.Columns;

		var visibleSpan = snapshot.Slice(firstVisibleOffset, Math.Min(visibleRowCount * _visualTheme.Columns, snapshot.Length - firstVisibleOffset));
		var screenBuffer = new byte[visibleSpan.Span.Length];
		await visibleSpan.CopyToAsync(screenBuffer, cancellationToken);

		var oldRows = _visibleRows;
		var totalRowsBuilder = ImmutableArray.CreateBuilder<IHexViewRow>();
		var newRowsBuilder = ImmutableArray.CreateBuilder<IHexViewRow>();

		var processedRelativeOffset = 0L;
		while (processedRelativeOffset < visibleSpan.Span.Length)
		{
			var rowSpan = visibleSpan.Slice(processedRelativeOffset, Math.Min(_visualTheme.Columns, visibleSpan.Span.Length - processedRelativeOffset));

			// try to reuse existing row if possible
			var isReused = false;
			for (var i = 0; i < oldRows.Length; i++)
			{
				var existingRow = oldRows[i];
				if (existingRow.Extent.Equals(rowSpan))
				{
					totalRowsBuilder.Add(existingRow);
					processedRelativeOffset += rowSpan.Span.Length;
					isReused = true;
					break;
				}
			}

			if (isReused)
			{
				continue;
			}

			// create new row
			var rowIndex = (int)(processedRelativeOffset / _visualTheme.Columns);
			var viewRow = new HexViewRow(
				this,
				new ViewportBounds(
					Left: 0, 
					Top: (firstVisibleRowIndex + rowIndex) * _visualTheme.RowHeight, 
					Width: _visualTheme.FontWidth * rowSpan.Span.Length, 
					Height: _visualTheme.RowHeight
				),
				rowSpan,
				screenBuffer.AsMemory((int)processedRelativeOffset, (int)rowSpan.Span.Length),
				[new(
					rowSpan,
					screenBuffer.AsMemory((int)processedRelativeOffset, (int)rowSpan.Span.Length),
					FormattedTextRun.ToHexString(screenBuffer.AsSpan((int)processedRelativeOffset, (int)rowSpan.Span.Length)),
					0,
					rowSpan.Span.Length * 2 * _visualTheme.FontWidth,
					null
				)],
				[new(
					rowSpan,
					screenBuffer.AsMemory((int)processedRelativeOffset, (int)rowSpan.Span.Length),
					FormattedTextRun.ToAsciiString(screenBuffer.AsSpan((int)processedRelativeOffset, (int)rowSpan.Span.Length)),
					0,
					rowSpan.Span.Length * _visualTheme.FontWidth,
					null
				)]
			);
			totalRowsBuilder.Add(viewRow);
			newRowsBuilder.Add(viewRow);
			processedRelativeOffset += rowSpan.Span.Length;
		}

		// diff
		var removedRowsBuilder = ImmutableArray.CreateBuilder<IHexViewRow>();
		foreach (var oldRow in oldRows)
		{
			var isStillVisible = false;
			foreach (var newRow in totalRowsBuilder)
			{
				if (oldRow.Extent.Equals(newRow.Extent))
				{
					isStillVisible = true;
					break;
				}
			}
			if (!isStillVisible)
			{
				removedRowsBuilder.Add(oldRow);
			}
		}

		if (removedRowsBuilder.Count == 0 && newRowsBuilder.Count == 0)
		{
			return;
		}

		// report changes
		_visibleRows = totalRowsBuilder.ToImmutable();
		VisibleRowsChanged?.Invoke(this, new VisibleRowsChangedEventArgs(removedRowsBuilder.ToImmutable(), newRowsBuilder.ToImmutable()));
	}

	public Task ResizeAsync(int newColumns, int newRows, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task ResizeWindowAsync(double viewportWidth, double viewportHeight, CancellationToken cancellationToken)
	{
		if (ViewportHeight == viewportHeight)
		{
			return Task.CompletedTask;
		}

		ViewportHeight = viewportHeight;
		ViewportWidth = viewportWidth;
		var oldHeight = ScrollableHeight;
		var rowCount = snapshot.Length / _visualTheme.Columns;
		ScrollableHeight = rowCount * _visualTheme.RowHeight;
		ScrollableHeightChanged?.Invoke(this, new HeightChangedEventArgs(oldHeight, ScrollableHeight));
		return InvalidateAsync(snapshot, cancellationToken);
	}

	public Task ScrollToAsync(double verticalOffset, CancellationToken cancellationToken)
	{
		if (VerticalOffset == verticalOffset)
		{
			return Task.CompletedTask;
		}

		VerticalOffset = verticalOffset;
		return InvalidateAsync(snapshot, cancellationToken);
	}

	public Point MapToVisualHex(SnapshotPoint point)
	{
		var (rowIndex, columnIndex) = Math.DivRem(point.Position, _visualTheme.Columns);
		return new Point(columnIndex * 2 * _visualTheme.FontWidth, rowIndex * _visualTheme.RowHeight);
	}

	public Point MapToVisualAscii(SnapshotPoint point)
	{
		var (rowIndex, columnIndex) = Math.DivRem(point.Position, _visualTheme.Columns);
		return new Point(columnIndex * _visualTheme.FontWidth, rowIndex * _visualTheme.RowHeight);
	}

	public Task ScrollDownByRowAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task ScrollUpByRowAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task GoToFirstPageAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task GoToLastPageAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task PageDownAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task PageUpAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
}

public record class VisualTheme(
	int Columns,
	double FontSize,
	double FontWidth,
	double RowHeight
);