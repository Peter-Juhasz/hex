# HexEditor

A fast and efficient hex viewer for binary files, built with .NET.


## CLI

- **Peak** into binary files with ease or **Interactive** viewer
- **Virtualization**: open **arbitrary large files** files, it loads only parts of the file into memory that are visible on screen
- **Theming** support for a customizable look

### Usage

Peak into a file:

```sh
hex <path>
```

Use interactive mode:

```sh
hex <path> --interactive
```

Navigation in interactive mode:

- `PageUp`/`PageDown`: Scroll by one page up/down
- `Ctrl` + `Up`/`Down`: Scroll by one line up/down
- `Ctrl` + `Home`/`End`: Go to the beginning/end of the file

### Build

Prerequisites:
- .NET 10 SDK or later

The `HexEditor.Cli` project can be built using standard .NET tools:

```sh
dotnet build
```
