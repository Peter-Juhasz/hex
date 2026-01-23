using HexEditor.Core.Caret;
using HexEditor.Core.Classification;
using HexEditor.Core.ContentType;
using HexEditor.Core.Hyperlinks;
using HexEditor.Core.Model;
using HexEditor.Core.Scrolling;
using HexEditor.Core.Selection;
using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using HexEditor.WinUI.ContentView;
using HexEditor.WinUI.Scrolling;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace HexEditor.WinUI;

public class WinUIHexView : IGraphicalHexView
{
	public WinUIHexView(IBinarySnapshot snapshot, string contentType, VisualTheme theme, ITaggerProvider taggerProvider, IContentTypeRegistry contentTypeRegistry)
	{
		this.snapshot = snapshot;
		ScrollableHeight = theme.RowHeight;
		_theme = theme;
		Selection = new SelectionManager(this);
		Caret = new CaretManager(this);
		Viewport = new ViewScroller(this, theme);

		var interestedContentTypes = contentTypeRegistry.GetBaseTypesAndSelf(contentType).Select(t => t.Type).ToImmutableArray();
		_classificationTagAggregator = new SequentialTagAggregator<ClassificationTag>(taggerProvider.CreateTaggers<ClassificationTag>(interestedContentTypes));
		_urlTagAggregator = new SequentialTagAggregator<UrlTag>(taggerProvider.CreateTaggers<UrlTag>(interestedContentTypes));

		TotalRowCount = (snapshot.Length / _theme.Columns) + 1;
		ScrollableHeight = TotalRowCount * _theme.RowHeight;
	}

	private readonly ITagAggregator<ClassificationTag> _classificationTagAggregator;
	private readonly ITagAggregator<UrlTag> _urlTagAggregator;

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

	public ICaret Caret { get; }

	public IViewport Viewport { get; }

	internal async Task InvalidateAsync(CancellationToken cancellationToken) => await InvalidateAsync(snapshot, cancellationToken);

	internal async Task InvalidateAsync(IBinarySnapshot snapshot, CancellationToken cancellationToken)
	{
		// calculate visible span
		var visibleRowCount = (int)(Viewport.Height / _theme.RowHeight) + 2;
		var firstVisibleRowIndex = (int)(Viewport.VerticalOffset / _theme.RowHeight);
		var firstVisibleOffset = firstVisibleRowIndex * _theme.Columns;

		var visibleSpan = snapshot.Slice(firstVisibleOffset, Math.Min(visibleRowCount * _theme.Columns, snapshot.Length - firstVisibleOffset));

		// read data into buffer
		var screenBuffer = new byte[visibleSpan.Span.Length];
		await visibleSpan.CopyToAsync(screenBuffer, cancellationToken);

		// collect tags
		var classificationTags = await _classificationTagAggregator.GetTagsAsync(visibleSpan, cancellationToken).ConfigureAwait(false);
		var urlTags = await _urlTagAggregator.GetTagsAsync(visibleSpan, cancellationToken).ConfigureAwait(false);
		
		var allTags = new TagSpan[classificationTags.Length + urlTags.Length];
		var allTagsIndex = 0;
		for (int i = 0; i < classificationTags.Length; i++)
		{
			allTags[allTagsIndex++] = classificationTags[i];
		}

		for (int i = 0; i < urlTags.Length; i++)
		{
			allTags[allTagsIndex++] = urlTags[i];
		}

		var screenTagSpanMap = new TagSpanSplitMap(allTags);

		// build rows
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
			var rowTags = screenTagSpanMap.Slice(rowSpan);
			var viewRow = RowFormatter.Format(new(
				View: this as IConsoleHexView,
				Theme: _theme,
				Top: (firstVisibleRowIndex + rowIndex) * _theme.RowHeight,
				Span: rowSpan,
				Data: screenBuffer.AsMemory((int)processedRelativeOffset, (int)rowSpan.Span.Length),
				Tags: screenTagSpanMap
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

	public Vector2 MapToVisualHex(SnapshotPoint point)
	{
		var (rowIndex, columnIndex) = Math.DivRem(point.Position, _theme.Columns);
		return new Vector2((float)(columnIndex * 2 * _theme.FontWidth), (float)(rowIndex * _theme.RowHeight));
	}

	public Vector2 MapToVisualAscii(SnapshotPoint point)
	{
		var (rowIndex, columnIndex) = Math.DivRem(point.Position, _theme.Columns);
		return new Vector2((float)(columnIndex * _theme.FontWidth), (float)(rowIndex * _theme.RowHeight));
	}

	public SnapshotPoint MapFromVisualHex(Vector2 point)
	{
		var rowIndex = Math.Clamp((int)(point.Y / _theme.RowHeight), 0, TotalRowCount);
		var columnIndex = Math.Clamp((int)(point.X / (2 * _theme.FontWidth) + 1), 0, _theme.Columns);
		return new SnapshotPoint(snapshot, Math.Min(rowIndex * _theme.Columns + columnIndex, snapshot.Length));
	}

	public SnapshotPoint MapFromVisualAscii(Vector2 point)
	{
		var rowIndex = Math.Clamp((int)(point.Y / _theme.RowHeight), 0, TotalRowCount);
		var columnIndex = Math.Clamp((int)(point.X / _theme.FontWidth + 1), 0, _theme.Columns);
		return new SnapshotPoint(snapshot, Math.Min(rowIndex * _theme.Columns + columnIndex, snapshot.Length));
	}

	public SnapshotSpan MapRowFromVisual(double verticalOffset)
	{
		var rowIndex = Math.Clamp((int)(verticalOffset / _theme.RowHeight), 0, TotalRowCount);
		var rowStart = rowIndex * _theme.Columns;
		var rowEnd = Math.Min(rowStart + _theme.Columns, snapshot.Length);
		return new SnapshotSpan(snapshot, new LongSpan(rowStart, rowEnd - rowStart));
	}

	public Vector2[] MapToVisualHex(SnapshotSpan span)
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
				new(startPoint.X, startRowTop),
				new(endPoint.X, startRowTop),
				new(endPoint.X, (float)(startRowTop + _theme.RowHeight)),
				new(startPoint.X, (float)(startRowTop + _theme.RowHeight)),
			];
		}

		var fullRowWidth = (_theme.Columns * 2) * _theme.FontWidth;

		return
		[
			new(startPoint.X, startRowTop + 0),
			new((float)fullRowWidth, startRowTop + 0),
			new((float)fullRowWidth, (float)(startRowTop + height - _theme.RowHeight)),
			new(endPoint.X, (float)(startRowTop + height - _theme.RowHeight)),
			new(endPoint.X, (float)(startRowTop + height)),
			new(0, (float)(startRowTop + height)),
			new(0, (float)(startRowTop + _theme.RowHeight)),
			new(startPoint.X, (float)(startRowTop + _theme.RowHeight)),
		];
	}

	public Vector2[] MapToVisualAscii(SnapshotSpan span)
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
				new(startPoint.X, startRowTop),
				new(endPoint.X, startRowTop),
				new(endPoint.X, (float)(startRowTop + _theme.RowHeight)),
				new(startPoint.X, (float)(startRowTop + _theme.RowHeight)),
			];
		}

		var fullRowWidth = _theme.Columns * _theme.FontWidth;

		return
		[
			new(startPoint.X, startRowTop),
			new((float)fullRowWidth, startRowTop),
			new((float)fullRowWidth, (float)(startRowTop + height - _theme.RowHeight)),
			new(endPoint.X, (float)(startRowTop + height - _theme.RowHeight)),
			new(endPoint.X, (float)(startRowTop + height)),
			new(0, (float)(startRowTop + height)),
			new(0, (float)(startRowTop + _theme.RowHeight)),
			new(startPoint.X, (float)(startRowTop + _theme.RowHeight)),
		];
	}

	public SnapshotSpan GetContainingRow(SnapshotPoint point)
	{
		var rowIndex = point.Position / _theme.Columns;
		var rowStart = rowIndex * _theme.Columns;
		var rowEnd = Math.Min(rowStart + _theme.Columns, snapshot.Length);
		return new SnapshotSpan(snapshot, new LongSpan(rowStart, rowEnd - rowStart));
	}
}

public record class VisualTheme(
	FontFamily FontFamily,
	int Columns = 16,
	double FontSize = 14,
	double FontWidth = 8.25d,
	double RowHeight = 24,
	IReadOnlyDictionary<string, WinUITextRunStyle>? ClassificationStyleMap = null,
	WinUITextRunStyle? HyperlinkStyle = null
)
{
	public static double FontSizeToWidth(double fontSize) => fontSize * (8.25d / 14d);
}