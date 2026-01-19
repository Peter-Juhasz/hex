using System;

namespace HexEditor.WinUI.Caret;

public class ActiveViewChangedEventArgs(ActiveView activeView) : EventArgs
{
	public ActiveView ActiveView { get; } = activeView;
}