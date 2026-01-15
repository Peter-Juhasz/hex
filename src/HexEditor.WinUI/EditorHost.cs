using HexEditor.Model;
using HexEditor.ViewModel;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
			Width = new GridLength(1, GridUnitType.Star),
		});
		this.ColumnDefinitions.Add(new ColumnDefinition()
		{
			Width = GridLength.Auto,
		});

		this.RowDefinitions.Add(new RowDefinition()
		{
			Height = new GridLength(1, GridUnitType.Star),
		});

		_view = new WinUIHexView(snapshot);

		_editorScrollView = new ScrollView()
		{
			VerticalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden,
			HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
		};
		_editorCanvas = new Canvas()
		{
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Stretch,
		};
		_editorScrollView.Content = _editorCanvas;
		Grid.SetColumn(_editorScrollView, 3);
		this.Children.Add(_editorScrollView);

		_verticalScrollBar = new AnnotatedScrollBar()
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment= VerticalAlignment.Stretch,
		};
		_verticalScrollBar.Labels.Add(new AnnotatedScrollBarLabel("Track", 10));
		Grid.SetColumn(_verticalScrollBar, 4);
		this.Children.Add(_verticalScrollBar);

		_addressBarMargin = new AddressBarMargin(_view, _editorScrollView);
		Grid.SetColumn(_addressBarMargin, 0);
		this.Children.Add(_addressBarMargin);

		_outliningMargin = new OutliningMargin(_view, _editorScrollView);
		Grid.SetColumn(_outliningMargin, 2);
		this.Children.Add(_outliningMargin);

		this.Loaded += OnLoaded;
		this.Unloaded += OnUnloaded;

		_view.VisibleRowsChanged += OnViewVisibleRowsChanged;
		_view.HeightChanged += OnViewHeightChanged;
		_ = _view.InvalidateAsync();
	}

	private readonly WinUIHexView _view;

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		DispatcherQueue.TryEnqueue(() =>
		{
			_editorScrollView.ScrollPresenter.VerticalScrollController = _verticalScrollBar.ScrollController;
		});
	}

	private readonly AddressBarMargin _addressBarMargin;
	private readonly OutliningMargin _outliningMargin;

	private readonly ScrollView _editorScrollView;
	private readonly Canvas _editorCanvas;

	private readonly AnnotatedScrollBar _verticalScrollBar;

	private readonly double _fontSize = 14;
	private readonly FontFamily _editorFontFamily = new FontFamily("Cascadia Mono");
	private readonly Brush _editorForegroundBrush = new SolidColorBrush(Colors.Black);

	private void OnViewVisibleRowsChanged(object sender, EventArgs e)
	{
		var rows = _view.VisibleRows;

		DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
		{
			foreach (var row in rows)
			{
				foreach (var run in row.HexRuns)
				{
					var hexTextBlock = new TextBlock()
					{
						Text = run.Text,
						FontFamily = _editorFontFamily,
						FontSize = _fontSize,
						Foreground = _editorForegroundBrush,
						Tag = run,
					};
					Canvas.SetLeft(hexTextBlock, 8);
					Canvas.SetTop(hexTextBlock, row.VisualBounds.Top);
					_editorCanvas.Children.Add(hexTextBlock);
				}

				foreach (var run in row.AsciiRuns)
				{
					var asciiTextBlock = new TextBlock()
					{
						Text = run.Text,
						FontFamily = _editorFontFamily,
						FontSize = _fontSize,
						Foreground = _editorForegroundBrush,
						Tag = run,
					};
					Canvas.SetLeft(asciiTextBlock, 128);
					Canvas.SetTop(asciiTextBlock, row.VisualBounds.Top);
					_editorCanvas.Children.Add(asciiTextBlock);
				}
			}
		});
	}

	private void OnViewHeightChanged(object sender, HeightChangedEventArgs e)
	{
		DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
		{
			_editorCanvas.Height = e.NewHeight;
			//_verticalScrollBar.ScrollController.SetValues(0, e.NewHeight, 0, e.NewHeight);
		});
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
	}
}
