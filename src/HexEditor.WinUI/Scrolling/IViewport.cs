using HexEditor.Model;
using System;
using Windows.Foundation;

namespace HexEditor.WinUI.Scrolling;

public interface IViewport
{
	double ScrollableHeight { get; }
	double Height { get; }
	double VerticalOffset { get; }

	void Resize(double newHeight);

	void ScrollBy(double delta);
	void ScrollTo(double verticalOffset);
	void ScrollUpByRow();
	void ScrollDownByRow();
	void ScrollUpByPage();
	void ScrollDownByPage();
	void BringIntoView(SnapshotPoint point);

	Point MapToVisual(Point point);

	event EventHandler<ScrollVerticalOffsetChangedEventArgs>? VerticalOffsetChanged;
	event EventHandler<ScrollableHeightChangedEventArgs>? ScrollableHeightChanged;
	event EventHandler<ViewportHeightChangedEventArgs>? ViewportChanged;
}