using System;

namespace HexEditor.WinUI.Scrolling;

public class ViewportHeightChangedEventArgs(double newHeight) : EventArgs
{
	public double NewHeight { get; } = newHeight;
}
