using HexEditor.Core.Tagging;
using HexEditor.Formats;
using HexEditor.Model;
using HexEditor.ViewModel;
using HexEditor.WinUI.AddressBar;
using HexEditor.WinUI.ContentView;
using HexEditor.WinUI.Outlining;
using HexEditor.WinUI.Scrolling;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace HexEditor.WinUI;

public partial class EditorHost : ContentControl
{
	public EditorHost(IBinarySnapshot snapshot, string contentType)
	{
		this.RequestedTheme = ElementTheme.Light;
		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
		this.VerticalContentAlignment = VerticalAlignment.Stretch;

		_grid = new Grid()
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Top,
			Background = new SolidColorBrush(Colors.White),
		};
		_grid.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = GridLength.Auto
		});
		_grid.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(8, GridUnitType.Pixel),
		});
		_grid.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(16, GridUnitType.Pixel),
		});
		_grid.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(2, GridUnitType.Star),
		});
		_grid.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(1, GridUnitType.Star),
		});

		_grid.RowDefinitions.Clear();
		_grid.RowDefinitions.Add(new RowDefinition()
		{
			Height = new GridLength(1, GridUnitType.Auto),
		});

		var taggerProvider = new ReflectionTaggerProvider([typeof(UrlTagger).Assembly]);

		_view = new WinUIHexView(snapshot, _visualTheme);
		_viewScroller = new ViewScroller();
		_viewScroller.VerticalOffsetChanged += OnViewScrollerScrollOffsetChanged;

		_hexContentView = new HexContentView(_view, _visualTheme, _viewScroller);
		_outliningMargin = new OutliningMargin(_view, _viewScroller, _visualTheme, taggerProvider, contentType);
		_hexOutliningHighlightLayer = new HexOutliningHighlightLayer(_view, _outliningMargin, _visualTheme, _viewScroller);
		Grid.SetColumn(_hexOutliningHighlightLayer, 3);
		_grid.Children.Add(_hexOutliningHighlightLayer);

		Grid.SetColumn(_hexContentView, 3);
		_grid.Children.Add(_hexContentView);

		_asciiOutliningHighlightLayer = new AsciiOutliningHighlightLayer(_view, _outliningMargin, _visualTheme, _viewScroller);
		Grid.SetColumn(_asciiOutliningHighlightLayer, 4);
		_grid.Children.Add(_asciiOutliningHighlightLayer);

		_asciiContentView = new AsciiContentView(_view, _viewScroller, _visualTheme);
		Grid.SetColumn(_asciiContentView, 4);
		_grid.Children.Add(_asciiContentView);

		_addressBarMargin = new AddressBarMargin(_view, _viewScroller, _visualTheme);
		Grid.SetColumn(_addressBarMargin, 0);
		_grid.Children.Add(_addressBarMargin);

		Grid.SetColumn(_outliningMargin, 2);
		_grid.Children.Add(_outliningMargin);

		_scrollView = new ScrollView()
		{
			VerticalAlignment = VerticalAlignment.Stretch,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			VerticalContentAlignment = VerticalAlignment.Top,
			HorizontalScrollMode = ScrollingScrollMode.Disabled,
			HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden,
			VerticalScrollBarVisibility = ScrollingScrollBarVisibility.Auto,
			Content = _grid,
		};
		this.Content = _scrollView;

		this.Loaded += OnLoaded;
		this.Unloaded += OnUnloaded;

		_view.ScrollableHeightChanged += OnModelScrollableHeightChanged;
		this.PreviewKeyDown += OnKeyDown;
		this.SizeChanged += OnSizeChanged;

		_scrollTimer.Tick += OnScrollTick;
	}

	private readonly ScrollView _scrollView;
	private readonly Grid _grid;
	private readonly DispatcherTimer _scrollTimer = new()
	{
		Interval = TimeSpan.FromMilliseconds(10)
	};

	private readonly AddressBarMargin _addressBarMargin;
	private readonly OutliningMargin _outliningMargin;
	private readonly HexOutliningHighlightLayer _hexOutliningHighlightLayer;
	private readonly HexContentView _hexContentView;
	private readonly AsciiOutliningHighlightLayer _asciiOutliningHighlightLayer;
	private readonly AsciiContentView _asciiContentView;
	private readonly ViewScroller _viewScroller;

	private readonly WinUIHexView _view;
	private readonly BackgroundTaskQueue _queue = new(default);


	private VisualTheme _visualTheme = new(
		Columns: 24,
		FontFamily: new FontFamily("Cascadia Mono"),
		FontSize: 16,
		FontWidth: FontSizeToWidth(16),
		RowHeight: 24
	);
	private static double FontSizeToWidth(double fontSize) => fontSize * (8.25d / 14d);

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		var newHeight = e.NewSize.Height;
		_viewScroller.ResizeViewport(newHeight);
		_queue.Enqueue(c => _view.ResizeWindowAsync(0, newHeight, c));
	}

	private void OnViewScrollerScrollOffsetChanged(object? sender, ScrollVerticalOffsetChangedEventArgs e)
	{
		_queue.Enqueue(c => _view.ScrollToAsync(e.NewVerticalOffset, c));
	}

	private void OnModelScrollableHeightChanged(object? sender, HeightChangedEventArgs e)
	{
		// route to UI thread
		DispatcherQueue.TryEnqueue(() =>
		{
			_grid.Height = e.NewHeight;
			_viewScroller.SetScrollableHeight(e.NewHeight);
		});
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		DispatcherQueue.TryEnqueue(() =>
		{
			var newHeight = _scrollView.ActualHeight;
			_queue.Enqueue(c => _view.ResizeWindowAsync(0, newHeight, c));
			_scrollView.ScrollPresenter.ViewChanged += OnScrollViewChanged;
		});
	}

	private void OnScrollViewChanged(ScrollPresenter sender, object args)
	{
		_scrollTimer.Stop();
		_scrollTimer.Start();
	}

	private void OnScrollTick(object? sender, object e)
	{
		_scrollTimer.Stop();

		var offset = Math.Round(_scrollView.ScrollPresenter.VerticalOffset);
		_viewScroller.ScrollTo(offset);
	}

	private void OnKeyDown(object sender, KeyRoutedEventArgs e)
	{
		var controlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);

		switch (e.Key)
		{
			case VirtualKey.Home when controlState.HasFlag(CoreVirtualKeyStates.Down):
				_view.Caret.MoveToHome();
				e.Handled = true;
				break;

			case VirtualKey.End when controlState.HasFlag(CoreVirtualKeyStates.Down):
				_view.Caret.MoveToEnd();
				e.Handled = true;
				break;

			case VirtualKey.Home when controlState.HasFlag(CoreVirtualKeyStates.None):
				_view.Caret.MoveToRowStart();
				e.Handled = true;
				break;

			case VirtualKey.End when controlState.HasFlag(CoreVirtualKeyStates.None):
				_view.Caret.MoveToRowEnd();
				e.Handled = true;
				break;

			case VirtualKey.Up when controlState.HasFlag(CoreVirtualKeyStates.Down):
				_viewScroller.ScrollBy(-_visualTheme.RowHeight);
				e.Handled = true;
				break;

			case VirtualKey.Down when controlState.HasFlag(CoreVirtualKeyStates.Down):
				_viewScroller.ScrollBy(_visualTheme.RowHeight);
				e.Handled = true;
				break;

			case VirtualKey.Escape:
				_view.Selection.Clear();
				e.Handled = true;
				break;
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
	}
}

internal static partial class Extensions
{
	private static double _rasterizationScale = 0d;

	extension(XamlRoot? root)
	{
		public double SnapToPixels(double value)
		{
			if (_rasterizationScale == 0d)
			{
				_rasterizationScale = root?.RasterizationScale ?? 1.0d;
			}

			return (Math.Round(value * _rasterizationScale) + 0.5) / _rasterizationScale;
		}
	}

	extension(PathFigure figure)
	{
		public void Fill(Point[] points)
		{
			double maxX = points[0].X;
			double maxY = points[0].Y;

			var segments = new PathSegmentCollection();
			for (int i = 1; i < points.Length; i++)
			{
				var point = points[i];
				segments.Add(new LineSegment() { Point = point });

				if (point.X > maxX) maxX = point.X;
				if (point.Y > maxY) maxY = point.Y;
			}

			figure.StartPoint = points[0];
			figure.Segments = segments;
		}
	}

	extension(double)
	{
		public static bool AreApproximatelyEqual(double a, double b, double tolerance = double.Epsilon)
		{
			return Math.Abs(a - b) <= tolerance;
		}
	}
}