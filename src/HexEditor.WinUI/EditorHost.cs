using HexEditor.Core.Tagging;
using HexEditor.Formats;
using HexEditor.Model;
using HexEditor.ViewModel;
using HexEditor.WinUI.AddressBar;
using HexEditor.WinUI.ContentView;
using HexEditor.WinUI.Outlining;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.System;
using Windows.UI.Core;

namespace HexEditor.WinUI;

public partial class EditorHost : Grid
{
	public EditorHost(IBinarySnapshot snapshot, string contentType)
	{
		this.RequestedTheme = ElementTheme.Light;
		this.Background = new SolidColorBrush(Colors.White);
		this.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = GridLength.Auto
		});
		this.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(8, GridUnitType.Pixel),
		});
		this.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(16, GridUnitType.Pixel),
		});
		this.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(2, GridUnitType.Star),
		});
		this.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(1, GridUnitType.Star),
		});
		this.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(16, GridUnitType.Pixel),
		});

		this.RowDefinitions.Add(new RowDefinition()
		{
			Height = new GridLength(1, GridUnitType.Star),
		});

		var taggerProvider = new ReflectionTaggerProvider([typeof(UrlTagger).Assembly]);

		_view = new WinUIHexView(snapshot, _visualTheme);
		_viewScroller = new ViewScroller();
		_viewScroller.OffsetChanged += OnScrollerScrollOffsetChanged;
		_viewScroller.ScrollableHeightChanged += OnScrollerScrollableHeightChanged;

		_verticalScrollBar = new ScrollBar()
		{
			Orientation = Orientation.Vertical,
			VerticalAlignment = VerticalAlignment.Stretch,
			RequestedTheme = ElementTheme.Light,
			IndicatorMode = ScrollingIndicatorMode.MouseIndicator,
		};
		_verticalScrollBar.SmallChange = _visualTheme.RowHeight;
		Grid.SetColumn(_verticalScrollBar, 5);
		this.Children.Add(_verticalScrollBar);

		_hexContentView = new HexContentView(_view, _visualTheme, _viewScroller);
		_outliningMargin = new OutliningMargin(_view, _viewScroller, _visualTheme, taggerProvider, contentType);
		_hexOutliningHighlightLayer = new HexOutliningHighlightLayer(_view, _outliningMargin, _visualTheme, _viewScroller);
		Grid.SetColumn(_hexOutliningHighlightLayer, 3);
		this.Children.Add(_hexOutliningHighlightLayer);

		Grid.SetColumn(_hexContentView, 3);
		this.Children.Add(_hexContentView);

		_asciiOutliningHighlightLayer = new AsciiOutliningHighlightLayer(_view, _outliningMargin, _visualTheme, _viewScroller);
		Grid.SetColumn(_asciiOutliningHighlightLayer, 4);
		this.Children.Add(_asciiOutliningHighlightLayer);

		_asciiContentView = new AsciiContentView(_view, _viewScroller, _visualTheme);
		Grid.SetColumn(_asciiContentView, 4);
		this.Children.Add(_asciiContentView);

		_addressBarMargin = new AddressBarMargin(_view, _viewScroller, _visualTheme);
		Grid.SetColumn(_addressBarMargin, 0);
		this.Children.Add(_addressBarMargin);

		Grid.SetColumn(_outliningMargin, 2);
		this.Children.Add(_outliningMargin);

		this.Loaded += OnLoaded;
		this.Unloaded += OnUnloaded;

		_view.ScrollableHeightChanged += OnModelScrollableHeightChanged;
		_verticalScrollBar.ValueChanged += OnScrollBarValueChanged;
		this.PointerWheelChanged += OnPointerWheelChanged;
		this.PreviewKeyDown += OnKeyDown;
		this.SizeChanged += OnSizeChanged;
	}

	private readonly AddressBarMargin _addressBarMargin;
	private readonly OutliningMargin _outliningMargin;
	private readonly HexOutliningHighlightLayer _hexOutliningHighlightLayer;
	private readonly HexContentView _hexContentView;
	private readonly AsciiOutliningHighlightLayer _asciiOutliningHighlightLayer;
	private readonly AsciiContentView _asciiContentView;
	private readonly ScrollBar _verticalScrollBar;
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

	private void OnScrollerScrollOffsetChanged(object? sender, ScrollChangedEventArgs e)
	{
		_queue.Enqueue(c => _view.ScrollToAsync(e.VerticalOffset, c));
	}

	private void OnScrollBarValueChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		// route to view scroller, already on UI thread
		_viewScroller.ScrollTo(e.NewValue);
	}

	private void OnScrollerScrollableHeightChanged(object? sender, ScrollableHeightChangedEventArgs e)
	{
		// set scrollbar
		_verticalScrollBar.Maximum = e.NewHeight;
	}

	private void OnModelScrollableHeightChanged(object? sender, HeightChangedEventArgs e)
	{
		// route to UI thread
		DispatcherQueue.TryEnqueue(() =>
		{
			_viewScroller.SetScrollableHeight(e.NewHeight);
		});
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		DispatcherQueue.TryEnqueue(() =>
		{
			var newHeight = this.ActualHeight;
			_queue.Enqueue(c => _view.ResizeWindowAsync(0, newHeight, c));
		});
	}

	private void OnKeyDown(object sender, KeyRoutedEventArgs e)
	{
		switch (e.Key)
		{
			case VirtualKey.PageUp:
				_viewScroller.ScrollBy(-_viewScroller.ViewportHeight);
				e.Handled = true;
				break;

			case VirtualKey.PageDown:
				_viewScroller.ScrollBy(_viewScroller.ViewportHeight);
				e.Handled = true;
				break;

			case VirtualKey.Home when InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down):
				_viewScroller.ScrollTo(0);
				e.Handled = true;
				break;

			case VirtualKey.End when InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down):
				_viewScroller.ScrollTo(_viewScroller.ScrollableHeight - _viewScroller.ViewportHeight);
				e.Handled = true;
				break;

			case VirtualKey.Up when InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down):
				_viewScroller.ScrollBy(-_visualTheme.RowHeight);
				e.Handled = true;
				break;

			case VirtualKey.Down when InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down):
				_viewScroller.ScrollBy(_visualTheme.RowHeight);
				e.Handled = true;
				break;
		}
	}

	private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
	{
		e.Handled = true;

		var point = e.GetCurrentPoint(this);
		var delta = point.Properties.MouseWheelDelta;
		_viewScroller.ScrollBy(-delta);
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
}