using HexEditor.Model;
using HexEditor.ViewModel;
using System;

namespace HexEditor.WinUI.Scrolling;

internal sealed class ViewScroller : IViewScroller
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
		ScrollBy(-ViewportHeight);
	}

	public void ScrollDownByPage()
	{
		ScrollBy(ViewportHeight);
	}

	public void BringIntoView(SnapshotPoint point)
	{
		var top = _view.MapToVisualAscii(point).Y;
		var bottom = top + _theme.RowHeight;
		if (top < VerticalOffset)
		{
			ScrollTo(top);
		}
		else if (bottom > VerticalOffset + ViewportHeight)
		{
			ScrollTo(bottom - ViewportHeight);
		}
	}


	public event EventHandler<ScrollableHeightChangedEventArgs>? ScrollableHeightChanged;

	public double ScrollableHeight { get; private set; }


	private void OnScrollableHeightChanged(object? sender, HeightChangedEventArgs e)
	{
		if (ScrollableHeight != e.NewHeight)
		{
			ScrollableHeight = e.NewHeight;
			ScrollableHeightChanged?.Invoke(this, new ScrollableHeightChangedEventArgs(e.NewHeight));
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
