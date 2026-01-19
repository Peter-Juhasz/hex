using HexEditor.Model;
using HexEditor.ViewModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace HexEditor.WinUI.AddressBar;

internal sealed class AddressBarMargin : ContentControl
{
	public AddressBarMargin(WinUIHexView view, ViewScroller viewScroller, VisualTheme theme) : base()
	{
		_theme = theme;
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
			MinWidth = _theme.FontWidth * 8 + 8 * 2,
		};
		_scrollView.Content = _canvas;

		_view = view;
		_view.VisibleRowsChanged += OnViewVisibleRowsChanged;
		viewScroller.OffsetChanged += OnScrollOffsetChanged;
		viewScroller.ScrollableHeightChanged += OnScrollableHeightChanged;
	}


	private readonly ScrollView _scrollView;
	private readonly Canvas _canvas;
	private readonly VisualTheme _theme;


	private readonly Brush _addressBarForegroundBrush = new SolidColorBrush(Color.FromArgb(255, 122, 122, 122));
	private readonly WinUIHexView _view;

	private void OnViewVisibleRowsChanged(object? sender, VisibleRowsChangedEventArgs e)
	{
		DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
		{
			foreach (var row in e.RemovedRows)
			{
				for (int i = 0; i < _canvas.Children.Count; i++)
				{
					if (_canvas.Children[i] is TextBlock addressTextBlock &&
						addressTextBlock.Tag is long offset &&
						offset == row.Extent.Span.StartOffset)
					{
						addressTextBlock.PointerPressed -= OnAddressClick;
						_canvas.Children.RemoveAt(i);
						break;
					}
				}
			}

			foreach (var row in e.AddedRows)
			{
				var addressTextBlock = new TextBlock()
				{
					Text = row.Extent.Span.StartOffset.ToString("X8"),
					FontFamily = _theme.FontFamily,
					FontSize = _theme.FontSize,
					Foreground = _addressBarForegroundBrush,
					IsTextSelectionEnabled = false,
					IsHitTestVisible = true,
					TextWrapping = TextWrapping.NoWrap,
					TextTrimming = TextTrimming.None,
					TextAlignment = TextAlignment.Right,
					Tag = row.Extent.Span.StartOffset,
				};
				Canvas.SetLeft(addressTextBlock, 8);
				Canvas.SetTop(addressTextBlock, Math.Round(row.VisualBounds.Top));
				addressTextBlock.PointerPressed += OnAddressClick;
				_canvas.Children.Add(addressTextBlock);
			}
		});
	}

	private void OnAddressClick(object sender, PointerRoutedEventArgs e)
	{
		var address = (long)((FrameworkElement)sender).Tag;
		var snapshot = _view.Snapshot;
		var point = new SnapshotPoint(snapshot, address);
		var row = _view.GetContainingRow(point);
		_view.SelectionManager.Select(row);
	}

	#region Scrolling
	private static readonly ScrollingScrollOptions scrollOptions = new ScrollingScrollOptions(ScrollingAnimationMode.Disabled);

	private void OnScrollOffsetChanged(object? sender, ScrollChangedEventArgs e)
	{
		_scrollView.ScrollTo(0, e.VerticalOffset, scrollOptions);
	}

	private void OnScrollableHeightChanged(object? sender, ScrollableHeightChangedEventArgs e)
	{
		_canvas.Height = e.NewHeight;
	}
	#endregion
}
