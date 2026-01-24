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
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Numerics;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;

namespace HexEditor.WinUI.ContentView;

internal sealed class AsciiContentView : Canvas
{
	public AsciiContentView(WinUIHexView view, VisualTheme theme) : base()
	{
		_theme = theme;
		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
		this.IsTabStop = true;
		this.MinWidth = theme.Columns * theme.FontWidth;
		this.Background = new SolidColorBrush(Colors.Transparent);

		_canvas = this;

		_view = view;
		_view.VisibleRowsChanged += OnViewVisibleRowsChanged;

		_view.Selection.SelectionChanged += OnSelectionChanged;
		_view.Caret.CaretPositionChanged += OnCaretPositionChanged;
		_view.Caret.ActiveViewChanged += OnCaretActiveViewChanged;

		_caret = CreateCaret();
		_canvas.Children.Add(_caret);

		this.KeyDown += OnKeyDown;
		this.PointerPressed += OnPointerPressed;
		this.PointerMoved += OnPointerMoved;
		this.PointerReleased += OnPointerReleased;
	}

	private readonly Canvas _canvas;

	private readonly Brush _editorForegroundBrush = new SolidColorBrush(Colors.Black);
	private readonly WinUIHexView _view;
	private readonly VisualTheme _theme;

	private Path? _selectionPath;
	private readonly Brush _selectionBackground = new SolidColorBrush(Color.FromArgb(255, 153, 201, 239));
	private SnapshotPoint? _anchorPoint;

	private readonly Line _caret;
	private Storyboard? _caretStoryboard;
	private readonly Brush _caretStroke = new SolidColorBrush(Colors.Black);

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
						Foreground = _editorForegroundBrush,
						IsTextSelectionEnabled = false,
						IsHitTestVisible = false,
						TextWrapping = TextWrapping.NoWrap,
						TextTrimming = TextTrimming.None,
						TextAlignment = TextAlignment.Left,
						Tag = run,
					};
					if (run.Style is WinUITextRunStyle style)
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
						if (style.Foreground is not null)
						{
							hexTextBlock.Foreground = style.Foreground;
						}
						if (style.FontWeight is not null)
						{
							hexTextBlock.FontWeight = style.FontWeight.Value;
						}
						if (style.Opacity is not null)
						{
							hexTextBlock.Opacity = style.Opacity.Value;
						}
					}
					Canvas.SetLeft(hexTextBlock, Math.Round(run.LeftPosition));
					rowCanvas.Children.Add(hexTextBlock);
				}

				_canvas.Children.Add(rowCanvas);
			}
		});
	}

	#region Selection
	private void OnSelectionChanged(object? sender, Core.Selection.SelectionChangedEventArgs e)
	{
		if (e.Selection == null)
		{
			_selectionPath?.Visibility = Visibility.Collapsed;
			return;
		}

		if (_selectionPath == null)
		{
			_selectionPath = new Path()
			{
				Data = new PathGeometry()
				{
					Figures = [new PathFigure()
					{
						IsFilled = true,
						IsClosed = true,
					}],
				},
				Fill = _selectionBackground,
				IsHitTestVisible = false,
			};
			Canvas.SetZIndex(_selectionPath, -1);
			_canvas.Children.Add(_selectionPath);
		}

		var points = _view.MapToVisualAscii(e.Selection.Span);
		var figure = ((PathGeometry)_selectionPath.Data).Figures[0];
		figure.Fill(points);
		_selectionPath.Visibility = Visibility.Visible;
	}

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

	#region Caret
	private Line CreateCaret()
	{
		var caret = new Line()
		{
			Stroke = _caretStroke,
			StrokeThickness = 1,
			IsHitTestVisible = false,
			X1 = 0,
			Y1 = 0,
			X2 = 0,
			Y2 = _theme.RowHeight,
			Visibility = _view.Caret.ActiveView is ActiveView.Ascii ? Visibility.Visible : Visibility.Collapsed,
		};

		var animation = new DoubleAnimationUsingKeyFrames()
		{
			Duration = new Duration(TimeSpan.FromMilliseconds(500)),
			AutoReverse = true,
			RepeatBehavior = RepeatBehavior.Forever,
		};
		animation.KeyFrames.Add(new DiscreteDoubleKeyFrame()
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0)),
			Value = 1,
		});
		animation.KeyFrames.Add(new DiscreteDoubleKeyFrame()
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(250)),
			Value = 0,
		});

		var storyboard = new Storyboard();
		storyboard.Children.Add(animation);
		Storyboard.SetTarget(animation, caret);
		Storyboard.SetTargetProperty(animation, nameof(caret.Opacity));
		storyboard.Begin();
		_caretStoryboard = storyboard;
		return caret;
	}

	private void OnCaretPositionChanged(object? sender, CaretPositionChangedEventArgs e)
	{
		var visualPosition = _view.MapToVisualAscii(e.CaretPosition.Point);
		Canvas.SetLeft(_caret, Math.Round(visualPosition.Left));
		Canvas.SetTop(_caret, Math.Round(visualPosition.Top));
		_caretStoryboard!.Seek(TimeSpan.Zero);
	}

	private void OnCaretActiveViewChanged(object? sender, ActiveViewChangedEventArgs e)
	{
		_caret.Visibility = e.ActiveView is ActiveView.Ascii ? Visibility.Visible : Visibility.Collapsed;
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
					_view.Selection.MoveActivePointLeft();
					e.Handled = true;
					break;

				case VirtualKey.Right when !isControlDown:
					_view.Selection.MoveActivePointRight();
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

				case VirtualKey.Up when !isControlDown:
					_view.Caret.MoveUpByRow();
					e.Handled = true;
					break;

				case VirtualKey.Down when !isControlDown:
					_view.Caret.MoveDownByRow();
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
