using HexEditor.Core.Model;
using System;
using System.Numerics;

namespace HexEditor.Core.Scrolling;

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

	Vector2 MapToVisual(Vector2 point);

	event EventHandler<ScrollVerticalOffsetChangedEventArgs>? VerticalOffsetChanged;
	event EventHandler<ScrollableHeightChangedEventArgs>? ScrollableHeightChanged;
	event EventHandler<ViewportHeightChangedEventArgs>? ViewportChanged;
}