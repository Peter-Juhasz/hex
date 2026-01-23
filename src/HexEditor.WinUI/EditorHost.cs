using HexEditor.Core.ContentType;
using HexEditor.Core.Tagging;
using HexEditor.Formats;
using HexEditor.Model;
using HexEditor.WinUI.AddressBar;
using HexEditor.WinUI.Caret;
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
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;

namespace HexEditor.WinUI;

public partial class EditorHost : ContentControl
{
	public EditorHost(IBinarySnapshot snapshot, string contentType)
	{
		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
		this.VerticalContentAlignment = VerticalAlignment.Stretch;
		this.IsTabStop = true;

		_outerGrid = new Grid()
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
		};
		_outerGrid.RowDefinitions.Add(new RowDefinition()
		{
			Height = new GridLength(1, GridUnitType.Star),
		});
		_outerGrid.RowDefinitions.Add(new RowDefinition()
		{
			Height = GridLength.Auto,
		});

		_grid = new Grid()
		{
			RequestedTheme = ElementTheme.Light,
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
			Width = GridLength.Auto
		});
		_grid.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(2, GridUnitType.Star),
		});
		_grid.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(1, GridUnitType.Star),
		});

		_grid.RowDefinitions.Add(new RowDefinition()
		{
			Height = new GridLength(1, GridUnitType.Star),
		});

		var contentTypeDefinitionType = typeof(ContentTypeDefinition);
		var contentTypeRegistry = new ContentTypeRegistry(typeof(UrlTagger).Assembly
			.GetExportedTypes()
			.Where(t => contentTypeDefinitionType.IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
			.Select(t => (ContentTypeDefinition)Activator.CreateInstance(t)!)
		);
		var taggerProvider = new ReflectionTaggerProvider([typeof(UrlTagger).Assembly]);

		_view = new WinUIHexView(snapshot, contentType, _visualTheme, taggerProvider, contentTypeRegistry);
		_view.Viewport.VerticalOffsetChanged += OnViewScrollerScrollOffsetChanged;
		_view.Viewport.ScrollableHeightChanged += OnModelScrollableHeightChanged;

		_hexContentView = new HexContentView(_view, _visualTheme);
		_outliningMargin = new OutliningMargin(_view, _visualTheme, taggerProvider, contentType, contentTypeRegistry);
		_hexOutliningHighlightLayer = new HexOutliningHighlightLayer(_view, _outliningMargin, _visualTheme);
		Grid.SetColumn(_hexOutliningHighlightLayer, 3);
		_grid.Children.Add(_hexOutliningHighlightLayer);

		Grid.SetColumn(_hexContentView, 3);
		_grid.Children.Add(_hexContentView);

		_asciiOutliningHighlightLayer = new AsciiOutliningHighlightLayer(_view, _outliningMargin, _visualTheme);
		Grid.SetColumn(_asciiOutliningHighlightLayer, 4);
		_grid.Children.Add(_asciiOutliningHighlightLayer);

		_asciiContentView = new AsciiContentView(_view, _visualTheme);
		Grid.SetColumn(_asciiContentView, 4);
		_grid.Children.Add(_asciiContentView);

		_addressBarMargin = new AddressBarMargin(_view, _visualTheme);
		Grid.SetColumn(_addressBarMargin, 0);
		_grid.Children.Add(_addressBarMargin);

		Grid.SetColumn(_outliningMargin, 2);
		_grid.Children.Add(_outliningMargin);

		var statusBar = CreateStatusBar();
		Grid.SetRow(statusBar, 1);
		_outerGrid.Children.Add(statusBar);

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

		Grid.SetRow(_scrollView, 0);
		_outerGrid.Children.Add(_scrollView);

		this.Content = _outerGrid;

		this.Loaded += OnLoaded;
		this.Unloaded += OnUnloaded;

		this.PreviewKeyDown += OnKeyDown;
		this.SizeChanged += OnSizeChanged;

		_invalidationTimer.Tick += OnScrollTick;
		_view.Caret.CaretPositionChanged += OnCaretPositionChanged;
	}

	private readonly ScrollView _scrollView;
	private readonly Grid _grid;
	private readonly Grid _outerGrid;
	private readonly DispatcherTimer _invalidationTimer = new()
	{
		Interval = TimeSpan.FromMilliseconds(100)
	};

	private readonly AddressBarMargin _addressBarMargin;
	private readonly OutliningMargin _outliningMargin;
	private readonly HexOutliningHighlightLayer _hexOutliningHighlightLayer;
	private readonly HexContentView _hexContentView;
	private readonly AsciiOutliningHighlightLayer _asciiOutliningHighlightLayer;
	private readonly AsciiContentView _asciiContentView;

	private TextBlock _caretPositionTextBlock;

	private readonly WinUIHexView _view;
	private readonly BackgroundTaskQueue _queue = new(default);


	private VisualTheme _visualTheme = new(
		Columns: 24,
		FontFamily: new FontFamily("Cascadia Mono"),
		FontSize: 16,
		FontWidth: VisualTheme.FontSizeToWidth(16),
		RowHeight: 24,
		ClassificationStyleMap: new Dictionary<string, WinUITextRunStyle>()
		{
			[AsciiClassifier.NonPrintableTag.Type] = new WinUITextRunStyle(
				Opacity: 0.5
			),
		},
		HyperlinkStyle: new WinUITextRunStyle(
			Foreground: new SolidColorBrush(Colors.Blue),
			IsUnderline: true
		)
	);

	private FrameworkElement CreateStatusBar()
	{
		var statusBarGrid = new Grid()
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Bottom,
			RequestedTheme = ElementTheme.Default,
		};
		statusBarGrid.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = new GridLength(1, GridUnitType.Star),
		});
		statusBarGrid.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = GridLength.Auto,
		});
		statusBarGrid.RowDefinitions.Add(new RowDefinition()
		{
			Height = GridLength.Auto,
		});
		_caretPositionTextBlock = new TextBlock()
		{
			Margin = new Thickness(4),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Right,
			Text = "Row 00000000, Col 00 (0x00000000)",
		};
		Grid.SetColumn(_caretPositionTextBlock, 1);
		statusBarGrid.Children.Add(_caretPositionTextBlock);
		return statusBarGrid;
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		var newHeight = e.NewSize.Height;
		_view.Viewport.Resize(newHeight);
		_queue.Enqueue(c => _view.InvalidateAsync(c));
	}

	private void OnViewScrollerScrollOffsetChanged(object? sender, ScrollVerticalOffsetChangedEventArgs e)
	{
		_scrollView.ScrollTo(0, e.NewVerticalOffset);
	}

	private void OnModelScrollableHeightChanged(object? sender, ScrollableHeightChangedEventArgs e)
	{
		// route to UI thread
		DispatcherQueue.TryEnqueue(() =>
		{
			_grid.Height = e.NewHeight;
		});
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		DispatcherQueue.TryEnqueue(() =>
		{
			var newHeight = _scrollView.ActualHeight;
			_view.Viewport.Resize(newHeight);
			_queue.Enqueue(c => _view.InvalidateAsync(c));

			_grid.Height = _view.ScrollableHeight;
			_scrollView.ScrollPresenter.ViewChanged += OnScrollViewChanged;
		});
	}

	private void OnScrollViewChanged(ScrollPresenter sender, object args)
	{
		if (_invalidationTimer.IsEnabled)
		{
			return;
		}

		_invalidationTimer.Start();
	}

	private void OnScrollTick(object? sender, object e)
	{
		_invalidationTimer.Stop();

		var offset = Math.Round(_scrollView.ScrollPresenter.VerticalOffset);
		((ViewScroller)_view.Viewport).SynchronizeVerticalOffset(offset);
		_queue.Enqueue(c => _view.InvalidateAsync(c));
	}

	private void OnKeyDown(object sender, KeyRoutedEventArgs e)
	{
		var controlState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);

		switch (e.Key)
		{
			case VirtualKey.Up when controlState.HasFlag(CoreVirtualKeyStates.Down):
				_view.Viewport.ScrollUpByRow();
				e.Handled = true;
				break;

			case VirtualKey.Down when controlState.HasFlag(CoreVirtualKeyStates.Down):
				_view.Viewport.ScrollDownByRow();
				e.Handled = true;
				break;
		}
	}

	private void OnCaretPositionChanged(object? sender, CaretPositionChangedEventArgs e)
	{
		var row = _view.GetContainingRow(e.CaretPosition.Point);
		var column = (int)(e.CaretPosition.Point.Position - row.Start.Position);
		_caretPositionTextBlock.Text = $"Row {row.Start.Position:X8}, Col {column:X2} (0x{e.CaretPosition.Point.Position:X8})";
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