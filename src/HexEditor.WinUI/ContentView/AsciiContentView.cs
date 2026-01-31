using HexEditor.Core.Caret;
using HexEditor.Core.Model;
using HexEditor.Core.ViewModel;
using HexEditor.WinUI.Theming;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Numerics;
using System.Text;
using Windows.System;
using Windows.UI.Core;

namespace HexEditor.WinUI.ContentView;

internal sealed class AsciiContentView : Canvas
{
	public AsciiContentView(IGraphicalHexView view, VisualTheme theme) : base()
	{
		_theme = theme;
		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
		this.IsTabStop = true;
		this.MinWidth = IHexViewRow.GetTotalVisualWidthOfAsciiRow(view.Columns, theme.FontWidth, theme.AsciiView?.PrimaryGrouping ?? 0, theme.AsciiView?.SecondaryGrouping ?? 0);
		this.Background = theme.AsciiView?.Background ?? new SolidColorBrush(Colors.Transparent);

		_canvas = this;

		_view = view;
		_view.VisibleRowsChanged += OnViewVisibleRowsChanged;

		this.KeyDown += OnKeyDown;
		this.PointerPressed += OnPointerPressed;
		this.PointerMoved += OnPointerMoved;
		this.PointerReleased += OnPointerReleased;
		this.CharacterReceived += OnCharacterReceived;
	}

	private readonly Canvas _canvas;

	private readonly IGraphicalHexView _view;
	private readonly VisualTheme _theme;

	private SnapshotPoint? _anchorPoint;

	private void OnViewVisibleRowsChanged(object? sender, VisibleRowsChangedEventArgs e)
	{
		DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
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

				foreach (var run in row.AsciiRuns)
				{
					var hexTextBlock = new TextBlock()
					{
						Text = run.Text,
						FontFamily = _theme.FontFamily,
						FontSize = _theme.FontSize,
						Foreground = _theme.AsciiView?.Foreground ?? _theme.Foreground,
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

			_anchorPoint = _view.MapFromVisualAscii(pointerPoint.Position.ToVector2());
			e.Handled = true;
		}

		_view.Caret.ChangeView(ActiveView.Ascii);
	}

	private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
	{
		if (_anchorPoint is SnapshotPoint anchorPoint)
		{
			var activePoint = _view.MapFromVisualAscii(e.GetCurrentPoint(this).Position.ToVector2());
			_view.Selection.Select(anchorPoint, activePoint);
			e.Handled = true;
		}
	}

	private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
	{
		var pointerPoint = e.GetCurrentPoint(this);
		if (pointerPoint.Properties.IsLeftButtonPressed == false)
		{
			var activePoint = _view.MapFromVisualAscii(pointerPoint.Position.ToVector2());
			if (_anchorPoint == activePoint)
			{
				_view.Caret.MoveTo(activePoint);
			}

			_anchorPoint = null;
			e.Handled = true;
		}
	}
	#endregion

	#region Keyboard
	private void OnKeyDown(object sender, KeyRoutedEventArgs e)
	{
		var isControlDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
		var isShiftDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
		var isAltDown = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu).HasFlag(CoreVirtualKeyStates.Down);

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
					_view.Selection.MoveActivePointToEnd();
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

	private void OnCharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
	{
		var ch = args.Character;
		ReadOnlySpan<byte> data = Char.IsAscii(ch) ?[(byte)ch] : Encoding.UTF8.GetBytes(ch.ToString());
		_view.Selection.Replace(data);
		args.Handled = true;
	}
	#endregion
}
