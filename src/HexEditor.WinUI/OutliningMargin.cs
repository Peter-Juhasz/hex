using HexEditor.ViewModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using Windows.UI;

namespace HexEditor.WinUI;

internal class OutliningMargin : ContentControl
{
	public OutliningMargin(IHexView view, ScrollView editorScrollView) : base()
	{
		this.Padding = new Thickness(0);
		this.CornerRadius = new CornerRadius(0);
		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);

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
			MinWidth = 80,
		};
		_scrollView.Content = _canvas;

		_view = view;
		_view.VisibleRowsChanged += OnViewVisibleRowsChanged;
		_view.HeightChanged += OnViewHeightChanged;

		var scrollOptions = new ScrollingScrollOptions(ScrollingAnimationMode.Disabled);
		editorScrollView.ViewChanged += (s, e) =>
		{
			_scrollView.ScrollTo(0, editorScrollView.VerticalOffset, scrollOptions);
		};
		AddRegion(24, 120);
	}

	private void AddRegion(double startOffset, double endOffset)
	{
		double s = _canvas.XamlRoot?.RasterizationScale ?? 1.0;
		double SnapCenter(double v) => (Math.Round(v * s) + 0.5) / s;
		double SnapEdge(double v) => Math.Round(v * s) / s;

		var line = new Path()
		{
			Data = new PathGeometry()
			{
				Figures = [
					new PathFigure()
					{
						StartPoint = new(SnapCenter(8), startOffset),
						Segments =
						[
							new LineSegment()
							{
								Point = new(SnapCenter(8), SnapCenter(endOffset)),
							},
							new LineSegment()
							{
								Point = new(SnapCenter(16), SnapCenter(endOffset)),
							},
						],
					}
				],
			},
			Stroke = _addressBarForegroundBrush,
			StrokeThickness = 1,
		};
		Canvas.SetTop(line, 24);
		Canvas.SetLeft(line, 0);
		_canvas.Children.Add(line);
	}

	private readonly ScrollView _scrollView;
	private readonly Canvas _canvas;

	private readonly double _fontSize = 14;
	private readonly FontFamily _addressBarFontFamily = new FontFamily("Cascadia Mono");
	private readonly Brush _addressBarForegroundBrush = new SolidColorBrush(Color.FromArgb(255, 122, 122, 122));
	private readonly IHexView _view;

	private void OnViewVisibleRowsChanged(object sender, EventArgs e)
	{
		var visibleRows = _view.VisibleRows;
	}

	private void OnViewHeightChanged(object sender, HeightChangedEventArgs e)
	{
		DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
		{
			_canvas.Height = e.NewHeight;
		});
	}
}
