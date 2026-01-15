using HexEditor.ViewModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.Foundation;
using Windows.UI;

namespace HexEditor.WinUI;

internal class AddressBarMargin : ContentControl
{
	public AddressBarMargin(IHexView view, ScrollView editorScrollView) : base()
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
		DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
		{
			var rows = visibleRows;

			foreach (var row in rows)
			{
				var addressTextBlock = new TextBlock()
				{
					Text = row.Extent.Span.StartOffset.ToString("X8"),
					FontFamily = _addressBarFontFamily,
					FontSize = _fontSize,
					Foreground = _addressBarForegroundBrush,
					Tag = row.Extent.Span.StartOffset,
				};
				Canvas.SetLeft(addressTextBlock, 8);
				Canvas.SetTop(addressTextBlock, row.VisualBounds.Top);
				_canvas.Children.Add(addressTextBlock);
			}
		});
	}

	private void OnViewHeightChanged(object sender, HeightChangedEventArgs e)
	{
		DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
		{
			_canvas.Height = e.NewHeight;
		});
	}
}
