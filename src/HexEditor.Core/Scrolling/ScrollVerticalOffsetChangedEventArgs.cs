using System;

namespace HexEditor.Core.Scrolling;

public class ScrollVerticalOffsetChangedEventArgs(double verticalOffset) : EventArgs
{
	public double NewVerticalOffset { get; } = verticalOffset;
}
