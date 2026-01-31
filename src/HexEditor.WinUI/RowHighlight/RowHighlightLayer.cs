using HexEditor.Core.Caret;
using HexEditor.Core.ViewModel;
using HexEditor.Model;
using HexEditor.WinUI.Theming;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;

namespace HexEditor.WinUI.RowHighlight;

internal sealed class RowHighlightLayer : Canvas
{
	public RowHighlightLayer(IGraphicalHexView view, VisualTheme theme) : base()
	{
		_view = view;
		_theme = theme;

		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.IsHitTestVisible = false;

		_view.Caret.CaretPositionChanged += OnCaretPositionChanged;
	}

	private readonly IGraphicalHexView _view;
	private readonly VisualTheme _theme;

	private Rectangle? _rowHighlight;

	private void OnCaretPositionChanged(object? sender, CaretPositionChangedEventArgs e)
	{
		Invalidate(e.CaretPosition);
	}

	private void Invalidate(CaretPosition position)
	{
		var row = _view.GetContainingRow(position.Point);
		var visualBounds = _view.MapToVisualHex(row.Start);

		if (_rowHighlight == null)
		{
			var style = _theme.RowHighlight!;

			_rowHighlight = new Rectangle()
			{
				Fill = style.Background,
				IsHitTestVisible = false,
				Opacity = style.Opacity ?? 1.0,
			};

			if (style.BorderBrush != null && (style.BorderThickness ?? 0) > 0)
			{
				_rowHighlight.Stroke = style.BorderBrush;
				_rowHighlight.StrokeThickness = style.BorderThickness ?? 0;
			}

			this.Children.Add(_rowHighlight);
		}

		Canvas.SetLeft(_rowHighlight, 0);
		Canvas.SetTop(_rowHighlight, visualBounds.Top);
		_rowHighlight.Width = this.ActualWidth;
		_rowHighlight.Height = _theme.RowHeight;
	}
}
