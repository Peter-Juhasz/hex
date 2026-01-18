using System;

namespace HexEditor.WinUI.Selection;

public class SelectionChangedEventArgs : EventArgs
{
	public SelectionChangedEventArgs(BinarySelectionSpan? selection)
	{
		Selection = selection;
	}

	public BinarySelectionSpan? Selection { get; }
}