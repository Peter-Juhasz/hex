using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace HexEditor.WinUI;

public record class WinUITextRunStyle(
	Brush? Foreground = null,
	Brush? Background = null,
	FontWeight? FontWeight = null,
	double? Opacity = null
);
