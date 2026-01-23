using HexEditor.Model;
using HexEditor.ViewModel;
using System;
using Windows.Foundation;

namespace HexEditor.WinUI.Scrolling;

internal sealed class ViewScroller : IViewport
{
	public ViewScroller(WinUIHexView view, VisualTheme theme)
	{
		_view = view;
		_theme = theme;
		_view.ScrollableHeightChanged += OnScrollableHeightChanged;
	}

	private readonly WinUIHexView _view;
	private readonly VisualTheme _theme;

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

	internal void SynchronizeVerticalOffset(double verticalOffset)
	{
		VerticalOffset = verticalOffset;
	}

	public void ScrollUpByRow()
	{
		ScrollBy(-_theme.RowHeight);
	}

	public void ScrollDownByRow()
	{
		ScrollBy(_theme.RowHeight);
	}

	public void ScrollUpByPage()
	{
		ScrollBy(-Height);
	}

	public void ScrollDownByPage()
	{
		ScrollBy(Height);
	}

	public void BringIntoView(SnapshotPoint point)
	{
		var top = _view.MapToVisualAscii(point).Y;
		var bottom = top + _theme.RowHeight;
		if (top < VerticalOffset)
		{
			ScrollTo(top);
		}
		else if (bottom > VerticalOffset + Height)
		{
			ScrollTo(bottom - Height);
		}
	}


	public Point MapToVisual(Point point) => new(
		x: point.X,
		y: point.Y + VerticalOffset
	);



	public event EventHandler<ScrollableHeightChangedEventArgs>? ScrollableHeightChanged;

	public double ScrollableHeight => _view.ScrollableHeight;


	private void OnScrollableHeightChanged(object? sender, HeightChangedEventArgs e)
	{
		if (ScrollableHeight != e.NewHeight)
		{
			ScrollableHeightChanged?.Invoke(this, new ScrollableHeightChangedEventArgs(e.NewHeight));
		}
	}


	public double Height { get; private set; }

	public event EventHandler<ViewportHeightChangedEventArgs>? ViewportChanged;

	public void Resize(double newHeight)
	{
		if (Height != newHeight)
		{
			Height = newHeight;
			ViewportChanged?.Invoke(this, new ViewportHeightChangedEventArgs(newHeight));
		}
	}
}
