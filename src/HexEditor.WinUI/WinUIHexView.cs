using HexEditor.Model;
using HexEditor.ViewModel;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace HexEditor.WinUI;

internal class WinUIHexView(IBinarySnapshot snapshot) : IHexView
{
	private ImmutableArray<IHexViewRow> _visibleRows;

	public ImmutableArray<IHexViewRow> VisibleRows => _visibleRows;
	public long TotalRowCount { get; }
	public double ViewportHeight { get; }
	public double ViewportWidth { get; }

	public double ScrollableHeight { get; } = 20 * 24;

	public event EventHandler<VisibleLinesChangedEventArgs>? VisibleRowsChanged;

	public event EventHandler<HeightChangedEventArgs>? HeightChanged;

	internal async Task InvalidateAsync()
	{
		var rows = Enumerable.Range(0, 20).Select(i => (IHexViewRow)new ViewRow(
			this,
			new ViewportBounds(0, i * 24, 100, 24),
			snapshot.Slice(i, 1),
			new byte[] { (byte)i },
			[new FormattedSpan(snapshot.Slice(i, 1), new byte[] { (byte)i }, null)],
			[new FormattedSpan(snapshot.Slice(i, 1), new byte[] { (byte)i }, null)]
		));
		_visibleRows = rows.ToImmutableArray();
		HeightChanged?.Invoke(this, new HeightChangedEventArgs(0.0, ScrollableHeight));
		VisibleRowsChanged?.Invoke(this, new VisibleLinesChangedEventArgs([], _visibleRows));
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

	public Task ResizeAsync(int newColumns, int newRows, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task ResizeWindowAsync(double viewportWidth, double viewportHeight, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task ScrollDownByRowAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task ScrollUpByRowAsync(CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Point MapToScreen(SnapshotPoint point) => throw new NotImplementedException();
}
