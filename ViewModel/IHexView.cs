using System.Diagnostics.CodeAnalysis;

namespace HexEditor.ViewModel;

public interface IHexView
{
	bool TryGetRow(long index, [NotNullWhen(true)] out IViewRow? row);

	int RowCount { get; }
}
