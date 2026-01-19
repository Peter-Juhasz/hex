using System;

namespace HexEditor.WinUI.Scrolling;

internal sealed class ViewScroller : IViewScroller
{
	public event EventHandler<ScrollVerticalOffsetChangedEventArgs>? VerticalOffsetChanged;

	public double VerticalOffset { get; private set; }

	public void ScrollTo(double verticalOffset)
	{
		if (!double.AreApproximatelyEqual(VerticalOffset, verticalOffset, 1d))
		{
			VerticalOffset = verticalOffset;
			VerticalOffsetChanged?.Invoke(this, new ScrollVerticalOffsetChangedEventArgs(Math.Round(verticalOffset)));
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
