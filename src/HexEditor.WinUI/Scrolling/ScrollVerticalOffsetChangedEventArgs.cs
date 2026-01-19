using System;

namespace HexEditor.WinUI.Scrolling;

public class ScrollVerticalOffsetChangedEventArgs(double verticalOffset) : EventArgs
{
	public double NewVerticalOffset { get; } = verticalOffset;
}
