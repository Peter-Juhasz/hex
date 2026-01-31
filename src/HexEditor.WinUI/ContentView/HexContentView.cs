using HexEditor.Core.Caret;
using HexEditor.Core.Model;
using HexEditor.Core.QuickInfo;
using HexEditor.Core.Tagging;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using HexEditor.WinUI.Theming;
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
using System.Linq;
using System.Numerics;
using System.Threading;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;

namespace HexEditor.WinUI.ContentView;

internal sealed class HexContentView : Canvas
{
	public HexContentView(IGraphicalHexView view, VisualTheme theme, ITagAggregator<QuickInfoTag> quickInfoTagAggregator) : base()
	{
		_view = view;
		_theme = theme;

		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
		this.IsTabStop = true;
		this.MinWidth = IHexViewRow.GetTotalVisualWidthOfHexRow(theme.Columns, theme.FontWidth, theme.HexView?.PrimaryGrouping ?? 0, theme.HexView?.SecondaryGrouping ?? 0);
		this.Background = theme.HexView?.Background ?? new SolidColorBrush(Colors.Transparent);
		_canvas = this;

		// render
		_view.VisibleRowsChanged += OnViewVisibleRowsChanged;

		// quick info
		_quickInfoTagAggregator = quickInfoTagAggregator;
		_quickInfoTimer.Tick += OnQuickInfoTimerTick;

		// keyboard
		this.KeyDown += OnKeyDown;

		// mouse
		this.PointerPressed += OnPointerPressed;
		this.PointerMoved += OnPointerMoved;
		this.PointerReleased += OnPointerReleased;
	}

	private readonly Canvas _canvas;

	private readonly IGraphicalHexView _view;
	private readonly VisualTheme _theme;

	private readonly ITagAggregator<QuickInfoTag> _quickInfoTagAggregator;
	private Flyout? _quickInfoFlyout;
	private readonly DispatcherTimer _quickInfoTimer = new()
	{
		Interval = TimeSpan.FromMilliseconds(100),
	};
	private readonly BackgroundTaskQueue _backgroundTaskQueue = new(default);
	private Point? _lastPointerPoint;

	private SnapshotPoint? _anchorPoint;

	private void OnViewVisibleRowsChanged(object? sender, VisibleRowsChangedEventArgs e)
	{
		DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.High, () =>
		{
			// remove old rows
			foreach (var row in e.RemovedRows)
			{
				for (var i = 0; i < _canvas.Children.Count; i++)
				{
					if (_canvas.Children[i] is FrameworkElement { Tag: IHexViewRow rowTag } fe && rowTag.Extent == row.Extent)
					{
						_canvas.Children.RemoveAt(i);
						break;
					}
				}
			}

			// add new rows
			foreach (var row in e.AddedRows)
			{
				var rowCanvas = new Canvas()
				{
					Height = row.VisualBounds.Height,
					Width = row.VisualBounds.Width,
					Tag = row,
					IsHitTestVisible = false,
				};
				Canvas.SetTop(rowCanvas, Math.Round(row.VisualBounds.Top));

				foreach (var run in row.HexRuns)
				{
					var hexTextBlock = new TextBlock()
					{
						Text = run.Text,
						FontFamily = _theme.FontFamily,
						FontSize = _theme.FontSize,
						Foreground = _theme.HexView?.Foreground ?? _theme.Foreground,
						IsTextSelectionEnabled = false,
						IsHitTestVisible = false,
						TextWrapping = TextWrapping.NoWrap,
						TextTrimming = TextTrimming.None,
						TextAlignment = TextAlignment.Left,
						Tag = run,
					};
					if (run.Style is TextRunStyle style)
					{
						if (style.Background is not null)
						{
							var rectangle = new Rectangle()
							{
								Width = run.RenderedWidth,
								Height = _theme.RowHeight,
								Fill = style.Background,
								IsHitTestVisible = false,
							};
							if (style.Opacity is not null)
							{
								rectangle.Opacity = style.Opacity.Value;
							}
							Canvas.SetZIndex(rectangle, -2);
							Canvas.SetLeft(rectangle, Math.Round(run.LeftPosition));
							rowCanvas.Children.Add(rectangle);
						}
						hexTextBlock.Apply(style);
					}
					Canvas.SetLeft(hexTextBlock, Math.Round(run.LeftPosition));
					rowCanvas.Children.Add(hexTextBlock);
				}

				_canvas.Children.Add(rowCanvas);
			}
		});
	}

	#region Selection
	private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		var pointerPoint = e.GetCurrentPoint(this);

		if (pointerPoint.Properties.IsLeftButtonPressed == true)
		{
			this.Focus(FocusState.Pointer);

			_anchorPoint = _view.MapFromVisualHex(pointerPoint.Position.ToVector2());
			e.Handled = true;
		}

		_view.Caret.ChangeView(ActiveView.Hex);
	}

	private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		var pointerPoint = e.GetCurrentPoint(this);

		if (_anchorPoint is SnapshotPoint anchorPoint)
		{
			var activePoint = _view.MapFromVisualHex(pointerPoint.Position.ToVector2());
			_view.Selection.Select(anchorPoint, activePoint);
			e.Handled = true;
			return;
		}

		if (pointerPoint.Properties is { IsLeftButtonPressed: false, IsRightButtonPressed: false, IsMiddleButtonPressed: false })
		{
			var previousPosition = _lastPointerPoint;
			if (previousPosition == pointerPoint.Position)
			{
				return;
			}

			_quickInfoFlyout?.Hide();

			_lastPointerPoint = pointerPoint.Position;
			_quickInfoTimer.Start();
		}
	}

	private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		var pointerPoint = e.GetCurrentPoint(this);
		if (pointerPoint.Properties.IsLeftButtonPressed == false)
		{
			var activePoint = _view.MapFromVisualHex(pointerPoint.Position.ToVector2());
			if (_anchorPoint == activePoint)
			{
				_view.Caret.MoveTo(activePoint);
			}

			_anchorPoint = null;
			e.Handled = true;
		}
	}
	#endregion

	#region QuickInfo
	private void OnQuickInfoTimerTick(object? sender, object e)
	{
		_quickInfoTimer.Stop();

		var pointerPoint = _lastPointerPoint;
		if (pointerPoint is null)
		{
			return;
		}

		var point = _view.MapFromVisualHex(pointerPoint.Value.ToVector2());
		_backgroundTaskQueue.Enqueue(async ct =>
		{
			var span = SnapshotSpan.Create(point, point.Position < point.Snapshot.Length ? 1 : 0);
			var tags = await _quickInfoTagAggregator.GetTagsAsync(span, ct).ConfigureAwait(false);
			if (ct.IsCancellationRequested)
			{
				return;
			}

			DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
			{
				HandleQuickInfo(tags, pointerPoint.Value, ct);
			});
		});
	}

	private void HandleQuickInfo(ImmutableArray<TagSpan<QuickInfoTag>> tags, Point point, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return;
		}

		if (tags.IsEmpty)
		{
			_quickInfoFlyout?.Hide();
			return;
		}

		if (_lastPointerPoint != point)
		{
			_quickInfoFlyout?.Hide();
			return;
		}

		var spanStart = tags.Min(t => t.Span.Start.Position);
		var spanEnd = tags.Max(t => t.Span.End.Position);
		var maximumSpan = new SnapshotSpan(tags[0].Span.Snapshot, new(spanStart, spanEnd - spanStart));
		var polygon = _view.MapToVisualHex(maximumSpan);
		var minX = polygon.Min(v => v.X);
		var maxY = polygon.Max(v => v.Y);

		if (_quickInfoFlyout == null)
		{
			_quickInfoFlyout = new Flyout()
			{
				AreOpenCloseAnimationsEnabled = false,
			};
			var style = new Style(typeof(FlyoutPresenter));
			style.Setters.Add(new Setter(FlyoutPresenter.PaddingProperty, new Thickness(8, 4, 8, 4)));
			style.Setters.Add(new Setter(FlyoutPresenter.MinHeightProperty, 8));
			style.Setters.Add(new Setter(FlyoutPresenter.FontSizeProperty, 12));
			style.Setters.Add(new Setter(FlyoutPresenter.CornerRadiusProperty, new CornerRadius(8)));
			style.Setters.Add(new Setter(FlyoutPresenter.BackgroundProperty, new SolidColorBrush(Color.FromArgb(255, 249, 249, 249))));
			_quickInfoFlyout.FlyoutPresenterStyle = style;
			var repeater = new ItemsRepeater();
			repeater.ItemsSource = tags.Select(t => (t.Tag as TextQuickInfoTag)?.Text).ToArray();
			_quickInfoFlyout.Content = repeater;
		}

		_quickInfoFlyout.ShowAt(this, new FlyoutShowOptions()
		{
			Position = _view.Viewport.TranslateFromVisualToViewport(new Vector2(minX, maxY)).ToPoint(),
			ShowMode = FlyoutShowMode.Transient,
			Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft,
		});
	}
	#endregion

	#region Keyboard
	private void OnKeyDown(object sender, KeyRoutedEventArgs e)
	{
		var isControlDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
		var isShiftDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

		if (isShiftDown)
		{
			switch (e.Key)
			{
				case VirtualKey.Home when isControlDown:
					_view.Selection.MoveActivePointToHome();
					e.Handled = true;
					break;

				case VirtualKey.End when isControlDown:
					_view.Selection.MoveActivePointToEnd();
					e.Handled = true;
					break;

				case VirtualKey.Home when !isControlDown:
					_view.Selection.MoveActivePointToRowStart();
					e.Handled = true;
					break;

				case VirtualKey.End when !isControlDown:
					_view.Selection.MoveActivePointToRowEnd();
					e.Handled = true;
					break;

				case VirtualKey.Left when !isControlDown:
					_view.Selection.MoveActivePointToPreviousByte();
					e.Handled = true;
					break;

				case VirtualKey.Right when !isControlDown:
					_view.Selection.MoveActivePointToNextByte();
					e.Handled = true;
					break;

				case VirtualKey.Left when isControlDown:
					_view.Selection.MoveActivePointToPreviousColumnGroup();
					e.Handled = true;
					break;

				case VirtualKey.Right when isControlDown:
					_view.Selection.MoveActivePointToNextColumnGroup();
					e.Handled = true;
					break;

				case VirtualKey.Up when !isControlDown:
					_view.Selection.MoveActivePointUpByRow();
					e.Handled = true;
					break;

				case VirtualKey.Down when !isControlDown:
					_view.Selection.MoveActivePointDownByRow();
					e.Handled = true;
					break;
			}
		}
		else
		{
			switch (e.Key)
			{
				case VirtualKey.Home when isControlDown:
					_view.Caret.MoveToHome();
					e.Handled = true;
					break;

				case VirtualKey.End when isControlDown:
					_view.Caret.MoveToEnd();
					e.Handled = true;
					break;

				case VirtualKey.Home when !isControlDown:
					_view.Caret.MoveToRowStart();
					e.Handled = true;
					break;

				case VirtualKey.End when !isControlDown:
					_view.Caret.MoveToRowEnd();
					e.Handled = true;
					break;

				case VirtualKey.Left when !isControlDown:
					_view.Caret.MoveToPreviousByte();
					e.Handled = true;
					break;

				case VirtualKey.Right when !isControlDown:
					_view.Caret.MoveToNextByte();
					e.Handled = true;
					break;

				case VirtualKey.Left when isControlDown:
					_view.Caret.MoveToPreviousColumnGroup();
					e.Handled = true;
					break;

				case VirtualKey.Right when isControlDown:
					_view.Caret.MoveToNextColumnGroup();
					e.Handled = true;
					break;

				case VirtualKey.Up when !isControlDown:
					_view.Caret.MoveUpByRow();
					e.Handled = true;
					break;

				case VirtualKey.Down when !isControlDown:
					_view.Caret.MoveDownByRow();
					e.Handled = true;
					break;

				case VirtualKey.PageUp when !isControlDown:
					_view.Caret.MoveUpByPage();
					e.Handled = true;
					break;

				case VirtualKey.PageDown when !isControlDown:
					_view.Caret.MoveDownByPage();
					e.Handled = true;
					break;

				case VirtualKey.PageUp when isControlDown:
					_view.Caret.MoveToPageTop();
					e.Handled = true;
					break;

				case VirtualKey.PageDown when isControlDown:
					_view.Caret.MoveToPageBottom();
					e.Handled = true;
					break;

				case VirtualKey.A when isControlDown:
					_view.Selection.SelectAll();
					e.Handled = true;
					break;

				case VirtualKey.Escape:
					_view.Selection.Clear();
					e.Handled = true;
					break;
			}
		}
	}
	#endregion
}
