using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace HexEditor.WinUI;

public record class WinUITextRunStyle(
	Brush? Foreground = null,
	Brush? Background = null,
	FontWeight? FontWeight = null,
	double? Opacity = null,
	bool IsUnderline = false,
	bool IsStrikethrough = false,
	bool IsItalic = false
)
{
	public static readonly WinUITextRunStyle None = new WinUITextRunStyle();

	public static WinUITextRunStyle Merge(WinUITextRunStyle left, WinUITextRunStyle right) => new WinUITextRunStyle(
		Foreground: right.Foreground ?? left.Foreground,
		Background: right.Background ?? left.Background,
		FontWeight: right.FontWeight ?? left.FontWeight,
		Opacity: right.Opacity ?? left.Opacity,
		IsUnderline: left.IsUnderline || right.IsUnderline,
		IsStrikethrough: left.IsStrikethrough || right.IsStrikethrough,
		IsItalic: left.IsItalic || right.IsItalic
	);
}
