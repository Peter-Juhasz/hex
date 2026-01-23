using System.Collections.Immutable;

namespace HexEditor.Core.ViewModel;

public class VisibleRowsChangedEventArgs(ImmutableArray<IHexViewRow> removedRows, ImmutableArray<IHexViewRow> addedRows) : EventArgs
{
	public ImmutableArray<IHexViewRow> RemovedRows { get; } = removedRows;
	public ImmutableArray<IHexViewRow> AddedRows { get; } = addedRows;
}
