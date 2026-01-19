using System;

namespace HexEditor.WinUI.Scrolling;

public class ScrollableHeightChangedEventArgs(double newHeight) : EventArgs
{
	public double NewHeight { get; } = newHeight;
}
