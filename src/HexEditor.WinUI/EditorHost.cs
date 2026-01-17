using HexEditor.Model;
using HexEditor.ViewModel;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;

namespace HexEditor.WinUI;

public partial class EditorHost : Grid
{
	public EditorHost(IBinarySnapshot snapshot)
	{
		this.RequestedTheme = ElementTheme.Light;
		this.Background = new SolidColorBrush(Colors.White);
		this.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(80, GridUnitType.Pixel),
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

		_view = new WinUIHexView(snapshot, _visualTheme);

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

		_hexContentView = new HexContentView(_view, _visualTheme);
		_outliningMargin = new OutliningMargin(_view, _hexContentView, _visualTheme);
		_hexOutliningHighlightLayer = new HexOutliningHighlightLayer(_view, _outliningMargin, _visualTheme);
		Grid.SetColumn(_hexOutliningHighlightLayer, 3);
		this.Children.Add(_hexOutliningHighlightLayer);

		_hexContentView = new HexContentView(_view, _visualTheme);
		Grid.SetColumn(_hexContentView, 3);
		this.Children.Add(_hexContentView);

		_asciiOutliningHighlightLayer = new AsciiOutliningHighlightLayer(_view, _outliningMargin, _visualTheme);
		Grid.SetColumn(_asciiOutliningHighlightLayer, 4);
		this.Children.Add(_asciiOutliningHighlightLayer);

		_asciiContentView = new AsciiContentView(_view, _hexContentView, _visualTheme);
		Grid.SetColumn(_asciiContentView, 4);
		this.Children.Add(_asciiContentView);

		_addressBarMargin = new AddressBarMargin(_view, _hexContentView);
		Grid.SetColumn(_addressBarMargin, 0);
		this.Children.Add(_addressBarMargin);

		Grid.SetColumn(_outliningMargin, 2);
		this.Children.Add(_outliningMargin);

		this.Loaded += OnLoaded;
		this.Unloaded += OnUnloaded;

		_hexContentView.ViewChanged += OnEditorViewChanged;
		_view.ScrollableHeightChanged += OnScrollableHeightChanged;
		_verticalScrollBar.ValueChanged += OnScrollBarValueChanged;
	}

	private void OnScrollBarValueChanged(object sender, RangeBaseValueChangedEventArgs e)
	{
		_hexContentView.ScrollTo(e.NewValue);
		_queue.Enqueue(c => _view.ScrollToAsync(e.NewValue, c));
	}

	private void OnScrollableHeightChanged(object? sender, HeightChangedEventArgs e)
	{
		_verticalScrollBar.Maximum = e.NewHeight;
	}

	private void OnEditorViewChanged(ScrollView sender, ViewportChangedEventArgs args)
	{
		_verticalScrollBar.ViewportSize = args.Height;
	}

	private readonly WinUIHexView _view;
	private readonly BackgroundTaskQueue _queue = new(default);

	private VisualTheme _visualTheme = new(
		Columns: 16,
		FontWidth: 8.25,
		RowHeight: 24
	);

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		DispatcherQueue.TryEnqueue(() =>
		{
			_queue.Enqueue(c => _view.ResizeWindowAsync(_hexContentView.ActualWidth, _hexContentView.ActualHeight, c));
		});
	}

	private readonly AddressBarMargin _addressBarMargin;
	private readonly OutliningMargin _outliningMargin;
	private readonly HexOutliningHighlightLayer _hexOutliningHighlightLayer;
	private readonly HexContentView _hexContentView;
	private readonly AsciiOutliningHighlightLayer _asciiOutliningHighlightLayer;
	private readonly AsciiContentView _asciiContentView;
	private readonly ScrollBar _verticalScrollBar;

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