using System.Collections.Immutable;

namespace HexEditor.ViewModel;

public interface IHexView
{
	ImmutableArray<IHexViewRow> VisibleRows { get; }

	long TotalRowCount { get; }


	double ViewportHeight { get; }
	double ViewportWidth { get; }
	Task ResizeAsync(int newColumns, int newRows, CancellationToken cancellationToken);
	Task ResizeWindowAsync(double viewportWidth, double viewportHeight, CancellationToken cancellationToken);


	Task GoToFirstPageAsync(CancellationToken cancellationToken);
	Task GoToLastPageAsync(CancellationToken cancellationToken);
	Task PageDownAsync(CancellationToken cancellationToken);
	Task PageUpAsync(CancellationToken cancellationToken);
	Task ScrollDownByRowAsync(CancellationToken cancellationToken);
	Task ScrollUpByRowAsync(CancellationToken cancellationToken);

	public event EventHandler? VisibleRowsChanged;
}
