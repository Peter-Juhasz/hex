using System;

namespace HexEditor.Core.Scrolling;

public class ScrollableHeightChangedEventArgs(double newHeight) : EventArgs
{
	public double NewHeight { get; } = newHeight;
}
