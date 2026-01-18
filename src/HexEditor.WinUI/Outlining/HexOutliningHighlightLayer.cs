using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
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

	private Path? _regionPath;

	private void OnOutliningRegionSelectionRequested(object? sender, OutliningRegionSelectionRequestedEventArgs e)
	{
		if (_regionPath == null)
		{
			_regionPath = new Path()
			{
				Data = new PathGeometry()
				{
					Figures = [new PathFigure()
					{
						IsFilled = true,
						IsClosed = true,
					}],
				},
				Fill = _pointerOverBrush,
				IsHitTestVisible = false,
			};
			Canvas.SetZIndex(_regionPath, -1);
			_canvas.Children.Add(_regionPath);
		}

		var points = _view.MapToVisualHex(e.Span.Span);
		var figure = ((PathGeometry)_regionPath.Data).Figures[0];
		figure.Fill(points);
		_regionPath.Visibility = Visibility.Visible;
	}

	private void OnDismissed(object? sender, EventArgs e)
	{
		_regionPath?.Visibility = Visibility.Collapsed;
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
