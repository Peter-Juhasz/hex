using HexEditor.Model;
using System;

namespace HexEditor.WinUI.Scrolling;

public interface IViewScroller
{
	double ScrollableHeight { get; }
	double ViewportHeight { get; }
	double VerticalOffset { get; }

	void ResizeViewport(double newHeight);

	void ScrollBy(double delta);
	void ScrollTo(double verticalOffset);
	void ScrollUpByRow();
	void ScrollDownByRow();
	void ScrollUpByPage();
	void ScrollDownByPage();
	void BringIntoView(SnapshotPoint point);

	event EventHandler<ScrollVerticalOffsetChangedEventArgs>? VerticalOffsetChanged;
	event EventHandler<ScrollableHeightChangedEventArgs>? ScrollableHeightChanged;
	event EventHandler<ViewportHeightChangedEventArgs>? ViewportChanged;
}