using HexEditor.Core.Caret;
using HexEditor.Core.ViewModel;
using HexEditor.WinUI.Theming;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using System;

namespace HexEditor.WinUI.Caret;

internal sealed class HexCaretLayer : Canvas
{
	public HexCaretLayer(IGraphicalHexView view, VisualTheme theme) : base()
	{
		_view = view;
		_theme = theme;

		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
		this.MinWidth = theme.Columns * theme.FontWidth;

		_canvas = this;
		_caret = CreateCaret();
		_canvas.Children.Add(_caret);

		_view.Caret.CaretPositionChanged += OnCaretPositionChanged;
		_view.Caret.ActiveViewChanged += OnCaretActiveViewChanged;
	}

	private readonly Canvas _canvas;
	private readonly IGraphicalHexView _view;
	private readonly VisualTheme _theme;
	private readonly Line _caret;
	private Storyboard? _caretStoryboard;
	private readonly Brush _caretStroke = new SolidColorBrush(Colors.Black);

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
			Visibility = _view.Caret.ActiveView is ActiveView.Hex ? Visibility.Visible : Visibility.Collapsed,
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
		var visualPosition = _view.MapToVisualHex(e.CaretPosition.Point);
		Canvas.SetLeft(_caret, Math.Round(visualPosition.Left));
		Canvas.SetTop(_caret, Math.Round(visualPosition.Top));
		_caretStoryboard!.Seek(TimeSpan.Zero);
	}

	private void OnCaretActiveViewChanged(object? sender, ActiveViewChangedEventArgs e)
	{
		_caret.Visibility = e.ActiveView is ActiveView.Hex ? Visibility.Visible : Visibility.Collapsed;
	}
}
