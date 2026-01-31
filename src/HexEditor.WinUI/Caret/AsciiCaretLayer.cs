using HexEditor.Core.Caret;
using HexEditor.Core.ViewModel;
using HexEditor.WinUI.Theming;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;

namespace HexEditor.WinUI.Caret;

internal sealed class AsciiCaretLayer : Canvas
{
	public AsciiCaretLayer(IGraphicalHexView view, VisualTheme theme) : base()
	{
		_view = view;
		_theme = theme;

		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
		this.MinWidth = view.Columns * theme.FontWidth;
		this.IsHitTestVisible = false;

		_canvas = this;
		_caret = CreateCaret();
		_canvas.Children.Add(_caret);

		_view.Caret.CaretPositionChanged += OnCaretPositionChanged;
		_view.Caret.ActiveViewChanged += OnCaretActiveViewChanged;
		this.Visibility = _view.Caret.ActiveView == ActiveView.Hex ? Visibility.Visible : Visibility.Collapsed;
	}

	private readonly Canvas _canvas;
	private readonly IGraphicalHexView _view;
	private readonly VisualTheme _theme;
	private readonly Line _caret;
	private ScalarKeyFrameAnimation? _caretAnimation;

	private Line CreateCaret()
	{
		var caret = new Line()
		{
			StrokeThickness = 1,
			IsHitTestVisible = false,
			X1 = 0,
			Y1 = 0,
			X2 = 0,
			Y2 = _theme.RowHeight,
		};

		if ((_theme.AsciiView?.Caret ?? _theme.Caret) is { } style)
		{
			caret.Stroke = style.Stroke;
			caret.StrokeThickness = style.StrokeThickness ?? 1d;
		}

		var compositor = CompositionTarget.GetCompositorForCurrentThread();
		var animation = compositor.CreateScalarKeyFrameAnimation();
		var easing = CompositionEasingFunction.CreateStepEasingFunction(compositor);
		animation.InsertKeyFrame(0, 1f, easing);
		animation.InsertKeyFrame(1, 0f, easing);
		animation.Duration = TimeSpan.FromMilliseconds(500);
		animation.IterationBehavior = AnimationIterationBehavior.Forever;
		animation.Direction = AnimationDirection.Alternate;
		animation.Target = nameof(Line.Opacity);
		caret.OpacityTransition = null;
		_caretAnimation = animation;
		caret.StartAnimation(animation);
		return caret;
	}

	private void OnCaretPositionChanged(object? sender, CaretPositionChangedEventArgs e)
	{
		if (_view.Caret.ActiveView != ActiveView.Ascii)
		{
			return;
		}

		Invalidate(e.CaretPosition);
	}

	private void Invalidate(CaretPosition position)
	{
		var visualPosition = _view.MapToVisualAscii(position.Point);
		Canvas.SetLeft(_caret, Math.Round(visualPosition.Left));
		Canvas.SetTop(_caret, Math.Round(visualPosition.Top));
		_caret.StartAnimation(_caretAnimation);
	}

	private void OnCaretActiveViewChanged(object? sender, ActiveViewChangedEventArgs e)
	{
		if (e.ActiveView == ActiveView.Ascii)
		{
			Invalidate(_view.Caret.Position);
			this.Visibility = Visibility.Visible;
		}
		else
		{
			this.Visibility = Visibility.Collapsed;
		}
	}
}
