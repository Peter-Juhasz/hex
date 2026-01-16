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
	private ImmutableArray<IHexViewRow> _visibleRows;

	public ImmutableArray<IHexViewRow> VisibleRows => _visibleRows;
	public long TotalRowCount { get; }
	public double ViewportHeight { get; private set; }
	public double ViewportWidth { get; private set; }

	public double ScrollableHeight { get; private set; } = 20 * 24;

	public event EventHandler<VisibleRowsChangedEventArgs>? VisibleRowsChanged;

	public event EventHandler<HeightChangedEventArgs>? ScrollableHeightChanged;

	private VisualTheme _visualTheme = theme;

	internal async Task InvalidateAsync(CancellationToken cancellationToken)
	{
		var visibleRowCount = (int)(ViewportHeight / _visualTheme.RowHeight);

		var visibleSpan = snapshot.Slice(0, Math.Min(visibleRowCount * _visualTheme.Columns, snapshot.Length));
		var screenBuffer = new byte[visibleSpan.Span.Length];
		await visibleSpan.CopyToAsync(screenBuffer, cancellationToken);

		var rowsBuilder = ImmutableArray.CreateBuilder<IHexViewRow>();

		var processedOffset = 0L;
		while (processedOffset < visibleSpan.Span.Length)
		{
			var rowSpan = visibleSpan.Slice(processedOffset, Math.Min(_visualTheme.Columns, visibleSpan.Span.Length - processedOffset));
			var rowIndex = (int)(processedOffset / _visualTheme.Columns);

			var viewRow = new HexViewRow(
				this,
				new ViewportBounds(0, rowIndex * _visualTheme.RowHeight, _visualTheme.FontWidth * rowSpan.Span.Length, _visualTheme.RowHeight),
				rowSpan,
				screenBuffer.AsMemory((int)processedOffset, (int)rowSpan.Span.Length),
				[new(
					rowSpan,
					screenBuffer.AsMemory((int)processedOffset, (int)rowSpan.Span.Length),
					FormattedTextRun.ToHexString(screenBuffer.AsSpan((int)processedOffset, (int)rowSpan.Span.Length)),
					0,
					rowSpan.Span.Length * 2 * _visualTheme.FontWidth,
					null
				)],
				[new(
					rowSpan,
					screenBuffer.AsMemory((int)processedOffset, (int)rowSpan.Span.Length),
					FormattedTextRun.ToAsciiString(screenBuffer.AsSpan((int)processedOffset, (int)rowSpan.Span.Length)),
					0,
					rowSpan.Span.Length * _visualTheme.FontWidth,
					null
				)]
			);
			rowsBuilder.Add(viewRow);
			processedOffset += rowSpan.Span.Length;
		}
		_visibleRows = rowsBuilder.ToImmutableArray();
		VisibleRowsChanged?.Invoke(this, new VisibleRowsChangedEventArgs([], _visibleRows));
	}

	public Task ResizeAsync(int newColumns, int newRows, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task ResizeWindowAsync(double viewportWidth, double viewportHeight, CancellationToken cancellationToken)
	{
		if (ViewportWidth == viewportWidth && ViewportHeight == viewportHeight)
		{
			return Task.CompletedTask;
		}

		ViewportHeight = viewportHeight;
		ViewportWidth = viewportWidth;
		var oldHeight = ScrollableHeight;
		var rowCount = snapshot.Length / _visualTheme.Columns;
		ScrollableHeight = rowCount * _visualTheme.RowHeight;
		ScrollableHeightChanged?.Invoke(this, new HeightChangedEventArgs(oldHeight, ScrollableHeight));
		return InvalidateAsync(cancellationToken);
	}

	public Point MapToScreen(SnapshotPoint point) => throw new NotImplementedException();

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
	double FontWidth,
	double RowHeight
);