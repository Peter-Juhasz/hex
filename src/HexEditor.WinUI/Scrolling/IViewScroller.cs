using System;

namespace HexEditor.WinUI.Scrolling;

public interface IViewScroller
{
	double ScrollableHeight { get; }
	double ViewportHeight { get; }
	double VerticalOffset { get; }

	void SetScrollableHeight(double newHeight);
	void ScrollBy(double delta);
	void ScrollTo(double verticalOffset);
	void ResizeViewport(double newHeight);

	event EventHandler<ScrollVerticalOffsetChangedEventArgs>? VerticalOffsetChanged;
	event EventHandler<ScrollableHeightChangedEventArgs>? ScrollableHeightChanged;
	event EventHandler<ViewportHeightChangedEventArgs>? ViewportChanged;
}