using HexEditor.Core.ViewModel;
using HexEditor.WinUI.Theming;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using Windows.UI;

namespace HexEditor.WinUI.Outlining;

internal sealed class AsciiOutliningHighlightLayer : Canvas
{
	public AsciiOutliningHighlightLayer(IGraphicalHexView view, OutliningMargin outliningMargin, VisualTheme theme) : base()
	{
		_view = view;
		_theme = theme;

		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;
		this.ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
		this.MinWidth = view.Columns * theme.FontWidth;
		this.IsHitTestVisible = false;

		_canvas = this;

		outliningMargin.OutliningRegionSelectionRequested += OnOutliningRegionSelectionRequested;
		outliningMargin.OutliningRegionDismissRequested += OnDismissed;
	}


	private readonly Canvas _canvas;

	private readonly IGraphicalHexView _view;
	private readonly VisualTheme _theme;

	private Path? _regionPath;

	private void OnOutliningRegionSelectionRequested(object? sender, OutliningRegionSelectionRequestedEventArgs e)
	{
		if (_regionPath == null)
		{
			_regionPath = new Path()
			{
				Data = new PathGeometry()
				{
					Figures = [new PathFigure()
					{
						IsFilled = true,
						IsClosed = true,
					}],
				},
				IsHitTestVisible = false,
			};
			if ((_theme.AsciiView?.OutliningRegionHighlight ?? _theme.OutliningRegionHighlight) is { } style)
			{
				_regionPath.Apply(style);
			}

			Canvas.SetZIndex(_regionPath, -1);
			_canvas.Children.Add(_regionPath);
		}

		var points = _view.MapToVisualAscii(e.Span.Span);
		var figure = ((PathGeometry)_regionPath.Data).Figures[0];
		figure.Fill(points);
		this.Visibility = Visibility.Visible;
	}

	private void OnDismissed(object? sender, EventArgs e)
	{
		this.Visibility = Visibility.Collapsed;
	}
}
