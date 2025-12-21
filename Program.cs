using HexEditor.Model;
using HexEditor.ViewModel;
using System.CommandLine;

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
var columnsOption = new Option<int?>("--columns");
root.Add(columnsOption);
var rowsOption = new Option<int?>("--rows");
root.Add(rowsOption);
var interactiveOption = new Option<bool>("--interactive");
root.Add(interactiveOption);
root.SetAction(async (context, ct) =>
{
	var path = context.GetRequiredValue(filePathArgument);

	var columns = context.GetValue(columnsOption);
	var rows = context.GetValue(rowsOption);

	var interactive = context.GetValue(interactiveOption);

	using var handle = File.OpenHandle(path);
	await using var binaryBuffer = new SafeFileHandleBinaryBuffer(handle);
	var viewBuffer = new LazyViewBuffer(binaryBuffer);
	var view = new ConsoleHexView(viewBuffer);

	if (columns != null && rows != null)
	{
		await view.ResizeAsync(columns.Value, rows.Value, cancellationToken);
	}
	else
	{
		await view.ResizeWindowAsync(Console.WindowWidth, Console.WindowHeight - 1, ct);
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

			case ConsoleKey.Home:
				await view.GoToFirstPageAsync(ct);
				break;

			case ConsoleKey.End:
				await view.GoToLastPageAsync(ct);
				break;

			case ConsoleKey.DownArrow:
				await view.ScrollDownAsync(ct);
				break;

			case ConsoleKey.UpArrow:
				await view.ScrollUpAsync(ct);
				break;

			case ConsoleKey.Escape:
				cts.Cancel();
				return;
		}
	}
});

await root.Parse(args).InvokeAsync(cancellationToken: cancellationToken);
