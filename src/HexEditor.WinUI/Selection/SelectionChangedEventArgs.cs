using System;

namespace HexEditor.WinUI.Selection;

public class SelectionChangedEventArgs : EventArgs
{
	public SelectionChangedEventArgs(SelectionSpan? selection)
	{
		Selection = selection;
	}

	public SelectionSpan? Selection { get; }
}