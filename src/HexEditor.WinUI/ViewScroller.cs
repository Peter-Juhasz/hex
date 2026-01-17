using System;

namespace HexEditor.WinUI;

internal class ViewScroller
{
	public event EventHandler<ScrollChangedEventArgs>? OffsetChanged;

	public double VerticalOffset { get; private set; }

	public void ScrollTo(double verticalOffset)
	{
		if (VerticalOffset != verticalOffset)
		{
			VerticalOffset = verticalOffset;
			OffsetChanged?.Invoke(this, new ScrollChangedEventArgs(Math.Round(verticalOffset)));
		}
	}

	public void ScrollBy(double delta)
	{
		ScrollTo(Math.Clamp(VerticalOffset + delta, 0, ScrollableHeight));
	}


	public event EventHandler<ScrollableHeightChangedEventArgs>? ScrollableHeightChanged;

	public double ScrollableHeight { get; private set; }

	public void SetScrollableHeight(double newHeight)
	{
		if (ScrollableHeight != newHeight)
		{
			ScrollableHeight = newHeight;
			ScrollableHeightChanged?.Invoke(this, new ScrollableHeightChangedEventArgs(newHeight));
		}
	}


	public double ViewportHeight { get; private set; }

	public event EventHandler<ViewportHeightChangedEventArgs>? ViewportChanged;

	public void ResizeViewport(double newHeight)
	{
		if (ViewportHeight != newHeight)
		{
			ViewportHeight = newHeight;
			ViewportChanged?.Invoke(this, new ViewportHeightChangedEventArgs(newHeight));
		}
	}
}

public class ScrollChangedEventArgs(double verticalOffset) : EventArgs
{
	public double VerticalOffset { get; } = verticalOffset;
}

public class ViewportHeightChangedEventArgs(double newHeight) : EventArgs
{
	public double NewHeight { get; } = newHeight;
}

public class ScrollableHeightChangedEventArgs(double newHeight) : EventArgs
{
	public double NewHeight { get; } = newHeight;
}
