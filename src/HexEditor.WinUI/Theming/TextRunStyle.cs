using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;

namespace HexEditor.WinUI.Theming;

public record class TextRunStyle(
	Brush? Foreground = null,
	Brush? Background = null,
	FontWeight? FontWeight = null,
	double? Opacity = null,
	bool Underline = false,
	bool Strikethrough = false,
	bool Italic = false
)
{
	public static readonly TextRunStyle None = new TextRunStyle();

	public static TextRunStyle Merge(TextRunStyle left, TextRunStyle right) => new TextRunStyle(
		Foreground: right.Foreground ?? left.Foreground,
		Background: right.Background ?? left.Background,
		FontWeight: right.FontWeight ?? left.FontWeight,
		Opacity: right.Opacity ?? left.Opacity,
		Underline: left.Underline || right.Underline,
		Strikethrough: left.Strikethrough || right.Strikethrough,
		Italic: left.Italic || right.Italic
	);
}
