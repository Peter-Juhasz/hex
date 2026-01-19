using HexEditor.Model;
using HexEditor.ViewModel;
using HexEditor.WinUI.Caret;
using HexEditor.WinUI.Scrolling;
using Microsoft.UI;
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
		_view = view;
		_theme = theme;

		this.Padding = new Thickness(0);
		this.CornerRadius = new CornerRadius(0);
		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Top;
		this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
		this.VerticalContentAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);

		_canvas = new Canvas
		{
			VerticalAlignment = VerticalAlignment.Top,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			MinWidth = _theme.FontWidth * 8 + 8 * 2,
			Background = new SolidColorBrush(Colors.Transparent),
		};
		this.Content = _canvas;

		_view.VisibleRowsChanged += OnViewVisibleRowsChanged;
		viewScroller.ScrollableHeightChanged += OnScrollableHeightChanged;

		_view.Caret.CaretPositionChanged += OnCaretPositionChanged;
	}

	private readonly Canvas _canvas;
	private readonly VisualTheme _theme;

	private readonly Brush _foregroundBrush = new SolidColorBrush(Color.FromArgb(255, 122, 122, 122));
	private readonly Brush _caretForegroundBrush = new SolidColorBrush(Colors.Black);
	private readonly WinUIHexView _view;

	private TextBlock? _previouslyActiveTextBlock;

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

			var currentRow = _view.GetContainingRow(_view.Caret.Position.Point);

			foreach (var row in e.AddedRows)
			{
				var addressTextBlock = new TextBlock()
				{
					Text = row.Extent.Span.StartOffset.ToString("X8"),
					FontFamily = _theme.FontFamily,
					FontSize = _theme.FontSize,
					Foreground = _foregroundBrush,
					IsTextSelectionEnabled = false,
					IsHitTestVisible = true,
					TextWrapping = TextWrapping.NoWrap,
					TextTrimming = TextTrimming.None,
					TextAlignment = TextAlignment.Right,
					Tag = row.Extent.Span.StartOffset,
				};
				Canvas.SetLeft(addressTextBlock, 8);
				Canvas.SetTop(addressTextBlock, Math.Round(row.VisualBounds.Top));

				if (row.Extent.Span.StartOffset == currentRow.Span.StartOffset)
				{
					addressTextBlock.Foreground = _caretForegroundBrush;
					_previouslyActiveTextBlock = addressTextBlock;
				}

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
		_view.Selection.Select(row);
	}

	#region Caret
	private void OnCaretPositionChanged(object? sender, CaretPositionChangedEventArgs e)
	{
		var row = _view.GetContainingRow(e.CaretPosition.Point);

		if (_previouslyActiveTextBlock is { Tag: long previousOffset })
		{
			if (previousOffset == row.Span.StartOffset)
			{
				return;
			}

			_previouslyActiveTextBlock.Foreground = _foregroundBrush;
			_previouslyActiveTextBlock = null;
		}

		foreach (var child in _canvas.Children)
		{
			if (child is TextBlock addressTextBlock)
			{
				if (addressTextBlock.Tag is long offset &&
					offset == row.Span.StartOffset
				)
				{
					addressTextBlock.Foreground = _caretForegroundBrush;
					_previouslyActiveTextBlock = addressTextBlock;
					break;
				}
			}
		}
	}
	#endregion

	#region Scrolling
	private void OnScrollableHeightChanged(object? sender, ScrollableHeightChangedEventArgs e)
	{
		_canvas.Height = e.NewHeight;
	}
	#endregion
}
