using HexEditor.Model;
using HexEditor.ViewModel;
using System.CommandLine;
using System.Text.Json;

#if DEBUG
if (args is [])
{
	args = [@"E:\rock2.mid", "--interactive"];
}
#endif

using var cts = new CancellationTokenSource();
var cancellationToken = cts.Token;

Console.CancelKeyPress += (sender, eventArgs) =>
{
	eventArgs.Cancel = true;
	cts.Cancel();
};

var root = new RootCommand();
var filePathArgument = new Argument<string>("path");
root.Add(filePathArgument);
var columnsOption = new Option<int?>("--columns", "-c");
root.Add(columnsOption);
var rowsOption = new Option<int?>("--rows", "-r");
root.Add(rowsOption);
var interactiveOption = new Option<bool>("--interactive", "-i");
root.Add(interactiveOption);
var themeOption = new Option<string?>("--theme", "-t");
root.Add(themeOption);
root.SetAction(async (context, ct) =>
{
	var path = context.GetRequiredValue(filePathArgument);

	var columns = context.GetValue(columnsOption);
	var rows = context.GetValue(rowsOption);

	var interactive = context.GetValue(interactiveOption);

	// load theme
	var themePath = context.GetValue(themeOption);
	ConsoleTheme? theme = null;
	if (themePath != null)
	{
		await using var themeStream = File.OpenRead(themePath);
		theme = await JsonSerializer.DeserializeAsync<ConsoleTheme>(themeStream, ConsoleThemeJsonSerializerContext.Default.ConsoleTheme, ct);
	}

	// open file
	using var handle = File.OpenHandle(path);
	await using var binaryBuffer = new SafeFileHandleBinaryBuffer(handle);
	var viewBuffer = new LazyViewBuffer(binaryBuffer);
	var view = new ConsoleHexView(viewBuffer);

	// resize view
	if (columns != null && rows != null)
	{
		await view.ResizeAsync(columns.Value, rows.Value, cancellationToken);
	}
	else
	{
		await view.ResizeWindowAsync(Console.WindowWidth, Console.WindowHeight, ct);
	}

	// apply theme
	if (theme != null)
	{
		await view.ApplyThemeAsync(theme, ct);
	}

	if (!interactive)
	{
		return;
	}

	while (true)
	{
		var key = Console.ReadKey(intercept: true);
		switch (key.Key)
		{
			case ConsoleKey.PageUp:
				await view.PageUpAsync(ct);
				break;

			case ConsoleKey.PageDown:
				await view.PageDownAsync(ct);
				break;

			case ConsoleKey.Home when key.Modifiers is ConsoleModifiers.Control:
				await view.GoToFirstPageAsync(ct);
				break;

			case ConsoleKey.End when key.Modifiers is ConsoleModifiers.Control:
				await view.GoToLastPageAsync(ct);
				break;

			case ConsoleKey.DownArrow when key.Modifiers is ConsoleModifiers.Control:
				await view.ScrollDownAsync(ct);
				break;

			case ConsoleKey.UpArrow when key.Modifiers is ConsoleModifiers.Control:
				await view.ScrollUpAsync(ct);
				break;

			case ConsoleKey.Escape:
				cts.Cancel();
				return;
		}
	}
});

await root.Parse(args).InvokeAsync(cancellationToken: cancellationToken);
