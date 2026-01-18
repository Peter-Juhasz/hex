using HexEditor.Model;
using HexEditor.ViewModel;
using HexEditor.WinUI.Selection;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Immutable;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace HexEditor.WinUI;

public class WinUIHexView : IHexView
{
	public WinUIHexView(IBinarySnapshot snapshot, VisualTheme theme)
	{
		this.snapshot = snapshot;
		ScrollableHeight = theme.RowHeight;
		_theme = theme;
		SelectionManager = new(this);
	}

	private ImmutableArray<IHexViewRow> _visibleRows = [];

	public ImmutableArray<IHexViewRow> VisibleRows => _visibleRows;
	public long TotalRowCount { get; private set; }

	public IBinarySnapshot Snapshot => snapshot;

	public double ViewportHeight { get; private set; }
	public double ViewportWidth { get; private set; }
	public double VerticalOffset { get; private set; }

	public double ScrollableHeight { get; private set; }

	public event EventHandler<VisibleRowsChangedEventArgs>? VisibleRowsChanged;

	public event EventHandler<HeightChangedEventArgs>? ScrollableHeightChanged;

	private VisualTheme _theme;
	private readonly IBinarySnapshot snapshot;

	public SelectionManager SelectionManager { get; }

	internal async Task InvalidateAsync(IBinarySnapshot snapshot, CancellationToken cancellationToken)
	{
		var visibleRowCount = (int)(ViewportHeight / _theme.RowHeight) + 2;
		var firstVisibleRowIndex = (int)(VerticalOffset / _theme.RowHeight);
		var firstVisibleOffset = firstVisibleRowIndex * _theme.Columns;

		var visibleSpan = snapshot.Slice(firstVisibleOffset, Math.Min(visibleRowCount * _theme.Columns, snapshot.Length - firstVisibleOffset));
		var screenBuffer = new byte[visibleSpan.Span.Length];
		await visibleSpan.CopyToAsync(screenBuffer, cancellationToken);

		var oldRows = _visibleRows;
		var totalRowsBuilder = ImmutableArray.CreateBuilder<IHexViewRow>();
		var newRowsBuilder = ImmutableArray.CreateBuilder<IHexViewRow>();

		var processedRelativeOffset = 0L;
		while (processedRelativeOffset < visibleSpan.Span.Length)
		{
			var rowSpan = visibleSpan.Slice(processedRelativeOffset, Math.Min(_theme.Columns, visibleSpan.Span.Length - processedRelativeOffset));

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
			var rowIndex = (int)(processedRelativeOffset / _theme.Columns);
			var viewRow = new HexViewRow(
				this,
				new ViewportBounds(
					Left: 0,
					Top: (firstVisibleRowIndex + rowIndex) * _theme.RowHeight,
					Width: _theme.FontWidth * rowSpan.Span.Length,
					Height: _theme.RowHeight
				),
				rowSpan,
				screenBuffer.AsMemory((int)processedRelativeOffset, (int)rowSpan.Span.Length),
				[new(
					rowSpan,
					screenBuffer.AsMemory((int)processedRelativeOffset, (int)rowSpan.Span.Length),
					FormattedTextRun.ToHexString(screenBuffer.AsSpan((int)processedRelativeOffset, (int)rowSpan.Span.Length)),
					0,
					rowSpan.Span.Length * 2 * _theme.FontWidth,
					null
				)],
				[new(
					rowSpan,
					screenBuffer.AsMemory((int)processedRelativeOffset, (int)rowSpan.Span.Length),
					FormattedTextRun.ToAsciiString(screenBuffer.AsSpan((int)processedRelativeOffset, (int)rowSpan.Span.Length)),
					0,
					rowSpan.Span.Length * _theme.FontWidth,
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
		var rowCount = (snapshot.Length / _theme.Columns) + 1;
		TotalRowCount = rowCount;
		ScrollableHeight = rowCount * _theme.RowHeight;
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
		var (rowIndex, columnIndex) = Math.DivRem(point.Position, _theme.Columns);
		return new Point(columnIndex * 2 * _theme.FontWidth, rowIndex * _theme.RowHeight);
	}

	public Point MapToVisualAscii(SnapshotPoint point)
	{
		var (rowIndex, columnIndex) = Math.DivRem(point.Position, _theme.Columns);
		return new Point(columnIndex * _theme.FontWidth, rowIndex * _theme.RowHeight);
	}

	public SnapshotPoint MapFromVisualHex(Point point)
	{
		var rowIndex = Math.Clamp((int)(point.Y / _theme.RowHeight), 0, TotalRowCount);
		var columnIndex = Math.Clamp((int)(point.X / (2 * _theme.FontWidth) + 1), 0, _theme.Columns);
		return new SnapshotPoint(snapshot, Math.Min(rowIndex * _theme.Columns + columnIndex, snapshot.Length));
	}

	public SnapshotPoint MapFromVisualAscii(Point point)
	{
		var rowIndex = Math.Clamp((int)(point.Y / _theme.RowHeight), 0, TotalRowCount);
		var columnIndex = Math.Clamp((int)(point.X / _theme.FontWidth + 1), 0, _theme.Columns);
		return new SnapshotPoint(snapshot, Math.Min(rowIndex * _theme.Columns + columnIndex, snapshot.Length));
	}

	public Point[] MapToVisualHex(SnapshotSpan span)
	{
		var startPoint = MapToVisualHex(span.Start);
		var endPoint = MapToVisualHex(span.End);
		var startRowTop = startPoint.Y;
		var endRowTop = endPoint.Y;

		var endRowBottom = endRowTop + _theme.RowHeight;
		var height = endRowBottom - startRowTop;

		if (startRowTop == endRowTop)
		{
			return
			[
				new Point(startPoint.X, startRowTop + 0),
				new Point(endPoint.X, startRowTop + 0),
				new Point(endPoint.X, startRowTop + _theme.RowHeight),
				new Point(startPoint.X, startRowTop + _theme.RowHeight),
			];
		}

		var fullRowWidth = (_theme.Columns * 2) * _theme.FontWidth;

		return
		[
			new Point(startPoint.X, startRowTop + 0),
			new Point(fullRowWidth, startRowTop + 0),
			new Point(fullRowWidth, startRowTop + height - _theme.RowHeight),
			new Point(endPoint.X, startRowTop + height - _theme.RowHeight),
			new Point(endPoint.X, startRowTop + height),
			new Point(0, startRowTop + height),
			new Point(0, startRowTop + _theme.RowHeight),
			new(startPoint.X, startRowTop + _theme.RowHeight),
		];
	}

	public Point[] MapToVisualAscii(SnapshotSpan span)
	{
		var startPoint = MapToVisualAscii(span.Start);
		var endPoint = MapToVisualAscii(span.End);
		var startRowTop = startPoint.Y;
		var endRowTop = endPoint.Y;

		var endRowBottom = endRowTop + _theme.RowHeight;
		var height = endRowBottom - startRowTop;

		if (startRowTop == endRowTop)
		{
			return
			[
				new Point(startPoint.X, startRowTop + 0),
				new Point(endPoint.X, startRowTop + 0),
				new Point(endPoint.X, startRowTop + _theme.RowHeight),
				new Point(startPoint.X, startRowTop + _theme.RowHeight),
			];
		}

		var fullRowWidth = _theme.Columns * _theme.FontWidth;

		return
		[
			new Point(startPoint.X, startRowTop + 0),
			new Point(fullRowWidth, startRowTop + 0),
			new Point(fullRowWidth, startRowTop + height - _theme.RowHeight),
			new Point(endPoint.X, startRowTop + height - _theme.RowHeight),
			new Point(endPoint.X, startRowTop + height),
			new Point(0, startRowTop + height),
			new Point(0, startRowTop + _theme.RowHeight),
			new(startPoint.X, startRowTop + _theme.RowHeight),
		];
	}

	public Point MapViewportToVisual(Point point) => new(
		x: point.X,
		y: point.Y + VerticalOffset
	);

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
	FontFamily FontFamily,
	double FontSize,
	double FontWidth,
	double RowHeight
);