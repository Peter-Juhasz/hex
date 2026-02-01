using HexEditor.Core.Caret;
using HexEditor.Core.Classification;
using HexEditor.Core.ContentType;
using HexEditor.Core.Hyperlinks;
using HexEditor.Core.Model;
using HexEditor.Core.Scrolling;
using HexEditor.Core.Selection;
using HexEditor.Core.Tagging;
using HexEditor.Core.Unnecessary;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using HexEditor.WinUI.ContentView;
using HexEditor.WinUI.Scrolling;
using HexEditor.WinUI.Theming;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace HexEditor.WinUI;

public class WinUIHexView : IGraphicalHexView
{
	public WinUIHexView(IBinarySnapshot snapshot, VisualTheme theme, ITaggerProvider taggerProvider, IContentTypeRegistry contentTypeRegistry)
	{
		this.snapshot = snapshot;
		ScrollableHeight = theme.RowHeight;
		_theme = theme;
		Columns = _theme.Columns ?? 16;
		Selection = new SelectionManager(this);
		Caret = new CaretManager(this);
		Viewport = new ViewScroller(this, theme);

		var interestedContentTypes = contentTypeRegistry.GetBaseTypesAndSelf(snapshot.Source.ContentType).Select(t => t.Type).ToImmutableArray();
		_classificationTagAggregator = new SequentialTagAggregator<ClassificationTag>(taggerProvider.CreateTaggers<ClassificationTag>(interestedContentTypes));
		_urlTagAggregator = new SequentialTagAggregator<UrlTag>(taggerProvider.CreateTaggers<UrlTag>(interestedContentTypes));
		_unnecessaryTagAggregator = new SequentialTagAggregator<UnnecessaryTag>(taggerProvider.CreateTaggers<UnnecessaryTag>(interestedContentTypes));

		TotalRowCount = (snapshot.Length / this.Columns) + 1;
		ScrollableHeight = TotalRowCount * _theme.RowHeight;
	}

	private readonly ITagAggregator<ClassificationTag> _classificationTagAggregator;
	private readonly ITagAggregator<UrlTag> _urlTagAggregator;
	private readonly ITagAggregator<UnnecessaryTag> _unnecessaryTagAggregator;

	private ImmutableArray<IHexViewRow> _visibleRows = [];

	public ImmutableArray<IHexViewRow> VisibleRows => _visibleRows;
	public long TotalRowCount { get; private set; }

	public IBinarySnapshot Snapshot => snapshot;

	public double ScrollableHeight { get; private set; }

	public event EventHandler<VisibleRowsChangedEventArgs>? VisibleRowsChanged;

	public event EventHandler<HeightChangedEventArgs>? ScrollableHeightChanged;

	private VisualTheme _theme;
	private readonly IBinarySnapshot snapshot;

	public ISelection Selection { get; }

	public ISnapshotManager SnapshotManager { get; }

	public ICaret Caret { get; }

	public int Columns { get; private set; }
	private double _lastCalculatedViewportWidth;

	public IViewport Viewport { get; }

	public SnapshotSpan VisibleSpan
	{
		get
		{
			var visibleRowCount = (long)(Viewport.Height / _theme.RowHeight) + 2;
			var firstVisibleRowIndex = (long)(Viewport.VerticalOffset / _theme.RowHeight);
			var firstVisibleOffset = firstVisibleRowIndex * this.Columns;
			return snapshot.Slice(firstVisibleOffset, Math.Min(visibleRowCount * this.Columns, snapshot.Length - firstVisibleOffset));
		}
	}

	internal async Task InvalidateAsync(CancellationToken cancellationToken)
	{
		// recalculate columns
		if (_theme.Columns == null)
		{
			if (!double.AreApproximatelyEqual(_lastCalculatedViewportWidth, Viewport.Width, 1d))
			{
				var viewportWidth = Viewport.Width; // of hex view
				var fontWidth = _theme.FontWidth;
				var primaryGrouping = _theme.HexView?.PrimaryGrouping ?? 0;
				var secondaryGrouping = _theme.HexView?.SecondaryGrouping ?? 0;
				Columns = IHexViewRow.GetMaxColumnCountFromHexView(viewportWidth, fontWidth, primaryGrouping, secondaryGrouping);
				_lastCalculatedViewportWidth = Viewport.Width;
			}
		}

		// calculate visible span
		var visibleSpan = VisibleSpan;
		var firstVisibleRowIndex = (long)(Viewport.VerticalOffset / _theme.RowHeight);

		// read data into buffer
		var screenBuffer = new byte[visibleSpan.Span.Length];
		await visibleSpan.CopyToAsync(screenBuffer, cancellationToken);

		// collect tags
		var classificationTags = await _classificationTagAggregator.GetTagsAsync(visibleSpan, cancellationToken).ConfigureAwait(false);
		var urlTags = await _urlTagAggregator.GetTagsAsync(visibleSpan, cancellationToken).ConfigureAwait(false);
		var unnecessaryTags = await _unnecessaryTagAggregator.GetTagsAsync(visibleSpan, cancellationToken).ConfigureAwait(false);

		var allTags = new TagSpan[classificationTags.Length + urlTags.Length + unnecessaryTags.Length];
		var written = 0;

		classificationTags.CopyTo(allTags);
		written += classificationTags.Length;

		urlTags.CopyTo(allTags.AsSpan(written));
		written += urlTags.Length;

		unnecessaryTags.CopyTo(allTags.AsSpan(written));

		var screenTagSpanMap = new TagIntersectionMap(allTags);

		// build rows
		var oldRows = _visibleRows;
		var totalRowsBuilder = ImmutableArray.CreateBuilder<IHexViewRow>();
		var newRowsBuilder = ImmutableArray.CreateBuilder<IHexViewRow>();

		var processedRelativeOffset = 0L;
		while (processedRelativeOffset < visibleSpan.Span.Length)
		{
			var rowSpan = visibleSpan.Slice(processedRelativeOffset, Math.Min(this.Columns, visibleSpan.Span.Length - processedRelativeOffset));

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
			var rowIndex = (int)(processedRelativeOffset / this.Columns);
			var rowTags = screenTagSpanMap.Slice(rowSpan);
			var viewRow = RowFormatter.Format(new(
				View: this,
				Theme: _theme,
				Top: (firstVisibleRowIndex + rowIndex) * _theme.RowHeight,
				Span: rowSpan,
				Data: screenBuffer.AsMemory((int)processedRelativeOffset, (int)rowSpan.Span.Length),
				Tags: rowTags
			));
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

	public ViewportBounds MapToVisualHex(SnapshotPoint point)
	{
		SnapshotMismatchException.ThrowIfMismatch(Snapshot, point.Snapshot);

		var (rowIndex, columnIndex) = Math.DivRem(point.Position, this.Columns);

		var primaryGrouping = _theme.HexView?.PrimaryGrouping ?? 0;
		var secondaryGrouping = _theme.HexView?.SecondaryGrouping ?? 0;
		var x = IHexViewRow.GetVisualLeftOfHexColumn((int)columnIndex, _theme.FontWidth, primaryGrouping, secondaryGrouping);

		return new ViewportBounds(
			Left: x,
			Top: rowIndex * _theme.RowHeight,
			Width: _theme.FontWidth * 2,
			Height: _theme.RowHeight
		);
	}

	public ViewportBounds MapToVisualAscii(SnapshotPoint point)
	{
		SnapshotMismatchException.ThrowIfMismatch(Snapshot, point.Snapshot);

		var (rowIndex, columnIndex) = Math.DivRem(point.Position, this.Columns);

		var primaryGrouping = _theme.AsciiView?.PrimaryGrouping ?? 0;
		var secondaryGrouping = _theme.AsciiView?.SecondaryGrouping ?? 0;
		var x = IHexViewRow.GetVisualLeftOfAsciiColumn((int)columnIndex, _theme.FontWidth, primaryGrouping, secondaryGrouping);

		return new ViewportBounds(
			Left: x,
			Top: rowIndex * _theme.RowHeight,
			Width: _theme.FontWidth,
			Height: _theme.RowHeight
		);
	}

	public long MapRowIndexFromVerticalOffset(double verticalOffset)
	{
		return Math.Clamp((long)(verticalOffset / _theme.RowHeight), 0, TotalRowCount);
	}

	public double MapRowIndexToVerticalOffset(long rowIndex)
	{
		return rowIndex * _theme.RowHeight;
	}

	public ImmutableArray<SnapshotSpan> GetRowSegments(SnapshotSpan span)
	{
		SnapshotMismatchException.ThrowIfMismatch(Snapshot, span.Snapshot);

		using var builder = new PooledArrayBuilder<SnapshotSpan>();
		var firstRow = this.GetContainingRow(span.Start);
		if (firstRow.End >= span.End)
		{
			builder.Add(span);
			return builder.ToImmutableArray();
		}

		builder.Add(firstRow.Slice(span.Start - firstRow.Start));

		var remaining = span.Snapshot.Slice(firstRow.End.Position);
		while (remaining.Length > 0)
		{
			var nextRow = remaining.Slice(0, Math.Min(Math.Min(span.End - remaining.Start, this.Columns), remaining.Length));
			builder.Add(nextRow);

			if (nextRow.Length != this.Columns)
			{
				break;
			}
		}

		return builder.ToImmutableArray();
	}

	public SnapshotPoint MapFromVisualHex(Vector2 point)
	{
		var rowIndex = MapRowIndexFromVerticalOffset(point.Y);

		var primaryGrouping = _theme.HexView?.PrimaryGrouping ?? 0;
		var secondaryGrouping = _theme.HexView?.SecondaryGrouping ?? 0;
		var columnIndex = IHexViewRow.GetColumnIndexFromHexPosition(point.X, _theme.FontWidth, primaryGrouping, secondaryGrouping);

		return new SnapshotPoint(snapshot, Math.Min(rowIndex * this.Columns + columnIndex, snapshot.Length));
	}

	public SnapshotPoint MapFromVisualAscii(Vector2 point)
	{
		var rowIndex = MapRowIndexFromVerticalOffset(point.Y);

		var primaryGrouping = _theme.AsciiView?.PrimaryGrouping ?? 0;
		var secondaryGrouping = _theme.AsciiView?.SecondaryGrouping ?? 0;
		var columnIndex = IHexViewRow.GetColumnIndexFromAsciiPosition(point.X, _theme.FontWidth, primaryGrouping, secondaryGrouping);

		return new SnapshotPoint(snapshot, Math.Min(rowIndex * this.Columns + columnIndex, snapshot.Length));
	}

	public SnapshotSpan MapRowFromVisual(double verticalOffset)
	{
		var rowIndex = MapRowIndexFromVerticalOffset(verticalOffset);
		var rowStart = rowIndex * this.Columns;
		var rowEnd = Math.Min(rowStart + this.Columns, snapshot.Length);
		return new SnapshotSpan(snapshot, new LongSpan(rowStart, rowEnd - rowStart));
	}

	public Vector2[] MapToVisualHex(SnapshotSpan span)
	{
		SnapshotMismatchException.ThrowIfMismatch(Snapshot, span.Snapshot);

		if (span.IsEmpty)
		{
			return [];
		}

		var startPoint = MapToVisualHex(span.Start);
		var endPoint = MapToVisualHex(span.End - 1);

		if (double.AreApproximatelyEqual(startPoint.Y, endPoint.Y, 1d))
		{
			return
			[
				startPoint.TopLeft,
				endPoint.TopRight,
				endPoint.BottomRight,
				startPoint.BottomLeft,
			];
		}

		var primaryGrouping = _theme.HexView?.PrimaryGrouping ?? 0;
		var secondaryGrouping = _theme.HexView?.SecondaryGrouping ?? 0;
		var fullRowWidth = IHexViewRow.GetTotalVisualWidthOfHexRow(this.Columns, _theme.FontWidth, primaryGrouping, secondaryGrouping);

		return
		[
			startPoint.TopLeft,
			new((float)fullRowWidth, (float)startPoint.Top),
			new((float)fullRowWidth, (float)endPoint.Top),
			endPoint.TopRight,
			endPoint.BottomRight,
			new(0, (float)endPoint.Bottom),
			new(0, (float)startPoint.Bottom),
			startPoint.BottomLeft,
		];
	}

	public Vector2[] MapToVisualAscii(SnapshotSpan span)
	{
		SnapshotMismatchException.ThrowIfMismatch(Snapshot, span.Snapshot);

		if (span.IsEmpty)
		{
			return [];
		}

		var startPoint = MapToVisualAscii(span.Start);
		var endPoint = MapToVisualAscii(span.End - 1);

		if (double.AreApproximatelyEqual(startPoint.Y, endPoint.Y, 1d))
		{
			return
			[
				startPoint.TopLeft,
				endPoint.TopRight,
				endPoint.BottomRight,
				startPoint.BottomLeft,
			];
		}

		var primaryGrouping = _theme.AsciiView?.PrimaryGrouping ?? 0;
		var secondaryGrouping = _theme.AsciiView?.SecondaryGrouping ?? 0;
		var fullRowWidth = IHexViewRow.GetTotalVisualWidthOfAsciiRow(this.Columns, _theme.FontWidth, primaryGrouping, secondaryGrouping);

		return
		[
			startPoint.TopLeft,
			new((float)fullRowWidth, (float)startPoint.Top),
			new((float)fullRowWidth, (float)endPoint.Top),
			endPoint.TopRight,
			endPoint.BottomRight,
			new(0, (float)endPoint.Bottom),
			new(0, (float)startPoint.Bottom),
			startPoint.BottomLeft,
		];
	}

	public SnapshotSpan GetContainingRow(SnapshotPoint point)
	{
		SnapshotMismatchException.ThrowIfMismatch(Snapshot, point.Snapshot);

		var rowIndex = point.Position / this.Columns;
		var rowStart = rowIndex * this.Columns;
		var rowEnd = Math.Min(rowStart + this.Columns, snapshot.Length);
		return new SnapshotSpan(snapshot, new LongSpan(rowStart, rowEnd - rowStart));
	}
}
