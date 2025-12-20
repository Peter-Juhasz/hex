using System.Diagnostics.CodeAnalysis;

namespace HexEditor.ViewModel;

public interface IHexView
{
	bool TryGetLine(long index, [NotNullWhen(true)] out IViewLine? line);

	int LineCount { get; }
}
