using System.Collections.Immutable;

namespace HexEditor.Core.ViewModel;

public interface IConsoleHexView
{
	ImmutableArray<IConsoleHexViewRow> VisibleRows { get; }

	long TotalRowCount { get; }


	int ViewportHeight { get; }
	long ViewportWidth { get; }

	long ScrollableHeight { get; }

	Task ResizeAsync(int newColumns, int newRows, CancellationToken cancellationToken);
	Task ResizeWindowAsync(int viewportWidth, int viewportHeight, CancellationToken cancellationToken);


	Task GoToFirstPageAsync(CancellationToken cancellationToken);
	Task GoToLastPageAsync(CancellationToken cancellationToken);
	Task PageDownAsync(CancellationToken cancellationToken);
	Task PageUpAsync(CancellationToken cancellationToken);
	Task ScrollDownByRowAsync(CancellationToken cancellationToken);
	Task ScrollUpByRowAsync(CancellationToken cancellationToken);
}
