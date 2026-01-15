using System.Collections.Immutable;

namespace HexEditor.ViewModel;

public interface IHexView
{
	ImmutableArray<IHexViewRow> VisibleRows { get; }

	long TotalRowCount { get; }


	double ViewportHeight { get; }
	double ViewportWidth { get; }

	double ScrollableHeight { get; }

	Task ResizeAsync(int newColumns, int newRows, CancellationToken cancellationToken);
	Task ResizeWindowAsync(double viewportWidth, double viewportHeight, CancellationToken cancellationToken);


	Task GoToFirstPageAsync(CancellationToken cancellationToken);
	Task GoToLastPageAsync(CancellationToken cancellationToken);
	Task PageDownAsync(CancellationToken cancellationToken);
	Task PageUpAsync(CancellationToken cancellationToken);
	Task ScrollDownByRowAsync(CancellationToken cancellationToken);
	Task ScrollUpByRowAsync(CancellationToken cancellationToken);

	event EventHandler<VisibleLinesChangedEventArgs>? VisibleRowsChanged;
	event EventHandler<HeightChangedEventArgs>? HeightChanged;
}

public class VisibleLinesChangedEventArgs(ImmutableArray<IHexViewRow> removedRows, ImmutableArray<IHexViewRow> addedRows) : EventArgs
{
	public ImmutableArray<IHexViewRow> RemovedRows { get; } = removedRows;
	public ImmutableArray<IHexViewRow> AddedRows { get; } = addedRows;
}

public class HeightChangedEventArgs(double oldHeight, double newHeight) : EventArgs
{
	public double OldHeight { get; } = oldHeight;
	public double NewHeight { get; } = newHeight;
}