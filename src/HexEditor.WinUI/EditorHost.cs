using HexEditor.Core.Caret;
using HexEditor.Core.ContentType;
using HexEditor.Core.Diagnostics;
using HexEditor.Core.Hyperlinks;
using HexEditor.Core.Model;
using HexEditor.Core.QuickInfo;
using HexEditor.Core.ReferenceHighlight;
using HexEditor.Core.Scrolling;
using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using HexEditor.WinUI.AddressBar;
using HexEditor.WinUI.Caret;
using HexEditor.WinUI.ColumnHighlight;
using HexEditor.WinUI.ContentView;
using HexEditor.WinUI.Outlining;
using HexEditor.WinUI.RowHighlight;
using HexEditor.WinUI.Scrolling;
using HexEditor.WinUI.Selection;
using HexEditor.WinUI.Squiggles;
using HexEditor.WinUI.Theming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Windows.System;
using Windows.UI.Core;

namespace HexEditor.WinUI;

public partial class EditorHost : ContentControl
{
	public EditorHost(
		IServiceProvider serviceProvider,
		IBinarySnapshot snapshot,
		ITaggerProvider taggerProvider,
		IContentTypeRegistry contentTypeRegistry,
		VisualTheme theme
	)
	{
		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
		this.VerticalContentAlignment = VerticalAlignment.Stretch;
		this.IsTabStop = true;
		_visualTheme = theme;

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
			Background = _visualTheme.Background,
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
			Width = new GridLength(8, GridUnitType.Pixel),
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

		var interestedContentTypes = contentTypeRegistry.GetBaseTypesAndSelf(snapshot.Source.ContentType).Select(t => t.Type).ToImmutableArray();
		var diagnosticTagAggregator = new LockingTagAggregator<DiagnosticTag>(
			new FullCachingTagAggregator<DiagnosticTag>(
				new ParallelTagAggregator<DiagnosticTag>(
					taggerProvider.CreateTaggers<DiagnosticTag>(interestedContentTypes)
				)
			)
		);

		var quickInfoTagAggregator = new LockingTagAggregator<QuickInfoTag>(
			new LastCallWithEditorStateCachingTagAggregator<QuickInfoTag>(
				new PostFilteringTagAggregator<QuickInfoTag>(
					new ParallelTagAggregator<QuickInfoTag>(
						taggerProvider.CreateTaggers<QuickInfoTag>(interestedContentTypes)
							.AddRange(taggerProvider.CreateTaggers<UrlTag>(interestedContentTypes).Select(t => new UrlToQuickInfoAdapter(t)))
							.AddRange(taggerProvider.CreateTaggers<DiagnosticTag>(interestedContentTypes).Select(t => new DiagnosticToQuickInfoAdapter(t)))
					)
				),
				serviceProvider.GetRequiredService<IViewAccessor>()
			)
		);

		var referenceTagAggregator = new LockingTagAggregator<ReferenceTag>(
			new LastCallWithEditorStateCachingTagAggregator<ReferenceTag>(
				new PostFilteringTagAggregator<ReferenceTag>(
					new ParallelTagAggregator<ReferenceTag>(
						taggerProvider.CreateTaggers<ReferenceTag>(interestedContentTypes)
					)
				),
				serviceProvider.GetRequiredService<IViewAccessor>()
			)
		);

		_view = new WinUIHexView(snapshot, _visualTheme, taggerProvider, contentTypeRegistry);
		_view.Viewport.VerticalOffsetChanged += OnViewScrollerScrollOffsetChanged;
		_view.Viewport.ScrollableHeightChanged += OnModelScrollableHeightChanged;
		//_view.SnapshotManager.Changed += OnActiveSnapshotChanged;

		_hexContentView = new HexContentView(_view, _visualTheme, quickInfoTagAggregator);
		_outliningMargin = new OutliningMargin(_view, _visualTheme, taggerProvider, contentTypeRegistry);

		if (_visualTheme.RowHighlight != null)
		{
			_rowHighlightLayer = new RowHighlightLayer(_view, _visualTheme);
			Grid.SetColumn(_rowHighlightLayer, 0);
			Grid.SetColumnSpan(_rowHighlightLayer, _grid.ColumnDefinitions.Count);
			_grid.Children.Add(_rowHighlightLayer);
		}

		_hexOutliningHighlightLayer = new HexOutliningHighlightLayer(_view, _outliningMargin, _visualTheme);
		Grid.SetColumn(_hexOutliningHighlightLayer, 4);
		_grid.Children.Add(_hexOutliningHighlightLayer);

		if (_visualTheme.HexView?.ColumnHighlight != null)
		{
			_hexColumnHighlightLayer = new HexColumnHighlightLayer(_view, _visualTheme);
			Grid.SetColumn(_hexColumnHighlightLayer, 4);
			_grid.Children.Add(_hexColumnHighlightLayer);
		}

		_hexReferenceHighlightLayer = new HexReferenceHighlightLayer(_view, referenceTagAggregator, _visualTheme);
		Grid.SetColumn(_hexReferenceHighlightLayer, 4);
		_grid.Children.Add(_hexReferenceHighlightLayer);

		_hexSelectionLayer = new HexSelectionLayer(_view, _visualTheme);
		Grid.SetColumn(_hexSelectionLayer, 4);
		_grid.Children.Add(_hexSelectionLayer);

		_hexSquigglesLayer = new HexSquigglesLayer(_view, diagnosticTagAggregator, _visualTheme);
		Grid.SetColumn(_hexSquigglesLayer, 4);
		_grid.Children.Add(_hexSquigglesLayer);

		Grid.SetColumn(_hexContentView, 4);
		_grid.Children.Add(_hexContentView);

		_hexCaretLayer = new HexCaretLayer(_view, _visualTheme);
		Grid.SetColumn(_hexCaretLayer, 4);
		_grid.Children.Add(_hexCaretLayer);

		_asciiOutliningHighlightLayer = new AsciiOutliningHighlightLayer(_view, _outliningMargin, _visualTheme);
		Grid.SetColumn(_asciiOutliningHighlightLayer, 5);
		_grid.Children.Add(_asciiOutliningHighlightLayer);

		if (_visualTheme.AsciiView?.ColumnHighlight != null)
		{
			_asciiColumnHighlightLayer = new AsciiColumnHighlightLayer(_view, _visualTheme);
			Grid.SetColumn(_asciiColumnHighlightLayer, 5);
			_grid.Children.Add(_asciiColumnHighlightLayer);
		}

		_asciiReferenceHighlightLayer = new AsciiReferenceHighlightLayer(_view, referenceTagAggregator, _visualTheme);
		Grid.SetColumn(_asciiReferenceHighlightLayer, 5);
		_grid.Children.Add(_asciiReferenceHighlightLayer);

		_asciiSelectionLayer = new AsciiSelectionLayer(_view, _visualTheme);
		Grid.SetColumn(_asciiSelectionLayer, 5);
		_grid.Children.Add(_asciiSelectionLayer);

		_asciiSquigglesLayer = new AsciiSquigglesLayer(_view, diagnosticTagAggregator, _visualTheme);
		Grid.SetColumn(_asciiSquigglesLayer, 5);
		_grid.Children.Add(_asciiSquigglesLayer);

		_asciiContentView = new AsciiContentView(_view, _visualTheme);
		Grid.SetColumn(_asciiContentView, 5);
		_grid.Children.Add(_asciiContentView);

		_asciiCaretLayer = new AsciiCaretLayer(_view, _visualTheme);
		Grid.SetColumn(_asciiCaretLayer, 5);
		_grid.Children.Add(_asciiCaretLayer);

		_addressBarMargin = new AddressBarMargin(_view, _visualTheme);
		Grid.SetColumn(_addressBarMargin, 0);
		_grid.Children.Add(_addressBarMargin);

		Grid.SetColumn(_outliningMargin, 2);
		_grid.Children.Add(_outliningMargin);

		var statusBar = CreateStatusBar(snapshot.Source.ContentType);
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

	private readonly RowHighlightLayer? _rowHighlightLayer;

	private readonly HexOutliningHighlightLayer _hexOutliningHighlightLayer;
	private readonly HexReferenceHighlightLayer _hexReferenceHighlightLayer;
	private readonly HexColumnHighlightLayer? _hexColumnHighlightLayer;
	private readonly HexContentView _hexContentView;
	private readonly HexSquigglesLayer _hexSquigglesLayer;
	private readonly HexSelectionLayer _hexSelectionLayer;
	private readonly HexCaretLayer _hexCaretLayer;

	private readonly AsciiOutliningHighlightLayer _asciiOutliningHighlightLayer;
	private readonly AsciiReferenceHighlightLayer _asciiReferenceHighlightLayer;
	private readonly AsciiColumnHighlightLayer? _asciiColumnHighlightLayer;
	private readonly AsciiContentView _asciiContentView;
	private readonly AsciiSquigglesLayer _asciiSquigglesLayer;
	private readonly AsciiSelectionLayer _asciiSelectionLayer;
	private readonly AsciiCaretLayer _asciiCaretLayer;

	private TextBlock _caretPositionTextBlock;
	private TextBlock _contentTypeTextBlock;

	private readonly WinUIHexView _view;
	private readonly BackgroundTaskQueue _queue = new(default);

	public IGraphicalHexView View => _view;


	private VisualTheme _visualTheme;

	[MemberNotNull(nameof(_caretPositionTextBlock))]
	[MemberNotNull(nameof(_contentTypeTextBlock))]
	private FrameworkElement CreateStatusBar(ContentTypeDefinition? contentType)
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

		_contentTypeTextBlock = new TextBlock()
		{
			Margin = new Thickness(4),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			Text = contentType?.Type,
		};
		Grid.SetColumn(_contentTypeTextBlock, 2);
		statusBarGrid.Children.Add(_contentTypeTextBlock);

		return statusBarGrid;
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs e)
	{
		var newHeight = e.NewSize.Height;
		var newWidth = _hexContentView.ActualWidth;
		_view.Viewport.Resize(newWidth, newHeight);
		_queue.Enqueue(c => _view.InvalidateAsync(c));
	}

	private void OnActiveSnapshotChanged(object? sender, SnapshotChangedEventArgs e)
	{
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
			var newWidth = _hexContentView.ActualWidth;
			_view.Viewport.Resize(newWidth, newHeight);
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
		public void Fill(Vector2[] points)
		{
			var segments = new PathSegmentCollection();
			for (int i = 1; i < points.Length; i++)
			{
				var point = points[i];
				segments.Add(new LineSegment() { Point = point.ToPoint() });
			}

			figure.StartPoint = points[0].ToPoint();
			figure.Segments = segments;
		}
	}

	extension(Shape path)
	{
		public void Apply(ShapeStyle style)
		{
			if (style.Fill != null)
			{
				path.Fill = style.Fill;
			}
			if (style.StrokeThickness > 0)
			{
				path.Stroke = style.Stroke;
				path.StrokeThickness = style.StrokeThickness.Value;
			}
			if (style.Opacity != null)
			{
				path.Opacity = style.Opacity.Value;
			}
		}
	}

	extension(TextBlock textBlock)
	{
		public void Apply(TextRunStyle style)
		{
			if (style.Foreground is not null)
			{
				textBlock.Foreground = style.Foreground;
			}
			if (style.FontWeight is not null)
			{
				textBlock.FontWeight = style.FontWeight.Value;
			}
			if (style.Opacity is not null)
			{
				textBlock.Opacity = style.Opacity.Value;
			}
			if (style.Underline)
			{
				textBlock.TextDecorations |= Windows.UI.Text.TextDecorations.Underline;
			}
			if (style.Strikethrough)
			{
				textBlock.TextDecorations |= Windows.UI.Text.TextDecorations.Strikethrough;
			}
			if (style.Italic)
			{
				textBlock.FontStyle = Windows.UI.Text.FontStyle.Italic;
			}
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