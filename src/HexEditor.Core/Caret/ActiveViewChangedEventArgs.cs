using System;

namespace HexEditor.Core.Caret;

public class ActiveViewChangedEventArgs(ActiveView activeView) : EventArgs
{
	public ActiveView ActiveView { get; } = activeView;
}