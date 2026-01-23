using System;

namespace HexEditor.Core.Scrolling;

public class ViewportHeightChangedEventArgs(double newHeight) : EventArgs
{
	public double NewHeight { get; } = newHeight;
}
