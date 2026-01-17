using HexEditor.Model;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using Windows.Foundation;
using Windows.UI;

namespace HexEditor.WinUI.Outlining;

internal sealed class HexOutliningHighlightLayer : ContentControl
{
	public HexOutliningHighlightLayer(WinUIHexView view, OutliningMargin outliningMargin, VisualTheme theme, ViewScroller viewScroller) : base()
	{
		this.Padding = new Thickness(0);
		this.CornerRadius = new CornerRadius(0);
		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);

		_scrollView = new ScrollView
		{
			VerticalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden,
			HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			VerticalScrollMode = ScrollingScrollMode.Disabled,
			CornerRadius = new CornerRadius(0),
			Padding = new Thickness(0),
		};
		this.Content = _scrollView;

		_canvas = new Canvas
		{
			MinWidth = (theme.Columns * 2) * theme.FontWidth,
		};
		_scrollView.Content = _canvas;

		_view = view;
		_theme = theme;

		outliningMargin.OutliningRegionSelectionRequested += OnOutliningRegionSelectionRequested;
		outliningMargin.OutliningRegionDismissRequested += OnDismissed;

		viewScroller.OffsetChanged += OnScrollOffsetChanged;
		viewScroller.ScrollableHeightChanged += OnScrollableHeightChanged;
	}


	private readonly ScrollView _scrollView;
	private readonly Canvas _canvas;

	private readonly WinUIHexView _view;
	private readonly VisualTheme _theme;
	private readonly Brush _pointerOverBrush = new SolidColorBrush(Color.FromArgb(255, 235, 238, 244));

	private void OnOutliningRegionSelectionRequested(object? sender, OutliningRegionSelectionRequestedEventArgs e)
	{
		var span = e.Span;
		var startPoint = _view.MapToVisualHex(span.Span.Start);
		var endPoint = _view.MapToVisualHex(span.Span.End);
		var startRowTop = startPoint.Y;
		var endRowTop = endPoint.Y;
		if (startRowTop == endRowTop)
		{
			return;
		}

		var endRowBottom = endRowTop + _theme.RowHeight;
		var height = endRowBottom - startRowTop;

		var fullRowWidth = (_theme.Columns * 2) * _theme.FontWidth;

		var polygon = new Path
		{
			Data = new PathGeometry()
			{
				Figures =
				[
					new PathFigure()
					{
						StartPoint = new Point(startPoint.X, _theme.RowHeight),
						Segments =
						[
							new LineSegment() { Point = new Point(startPoint.X, 0) },
							new LineSegment() { Point = new Point(fullRowWidth, 0) },
							new LineSegment() { Point = new Point(fullRowWidth, height - _theme.RowHeight) },
							new LineSegment() { Point = new Point(endPoint.X, height - _theme.RowHeight) },
							new LineSegment() { Point = new Point(endPoint.X, height) },
							new LineSegment() { Point = new Point(0, height) },
							new LineSegment() { Point = new Point(0, _theme.RowHeight) },
						],
						IsFilled = true,
						IsClosed = true,
					}
				]
			},
			Width = fullRowWidth,
			Height = height,
			Fill = _pointerOverBrush,
		};
		Canvas.SetLeft(polygon, 0);
		Canvas.SetTop(polygon, startRowTop);
		_canvas.Children.Add(polygon);
	}

	private void OnDismissed(object? sender, EventArgs e)
	{
		_canvas.Children.Clear();
	}

	#region Scrolling
	private static readonly ScrollingScrollOptions scrollOptions = new ScrollingScrollOptions(ScrollingAnimationMode.Disabled);

	private void OnScrollOffsetChanged(object? sender, ScrollChangedEventArgs e)
	{
		_scrollView.ScrollTo(0, e.VerticalOffset, scrollOptions);
	}

	private void OnScrollableHeightChanged(object sender, ScrollableHeightChangedEventArgs e)
	{
		_canvas.Height = e.NewHeight;
	}
	#endregion
}
