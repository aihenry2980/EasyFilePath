# EasyFilePath

Show the current document's absolute file path directly inside the Visual Studio text editor.

## Short Description

Display and customize the current file's absolute path in the Visual Studio editor, with folder highlighting and quick copy actions.

## Overview

EasyFilePath adds a compact, solid path line to the Visual Studio text editor so you can always see exactly where the current file is located.

This is useful when working with large solutions, duplicated folder names, generated files, multiple cloned repositories, or source trees where the document tab does not provide enough context.

The path line is rendered as an editor margin, so it does not cover or overlap your code.

## Key Features

- Show the absolute path for the current document.
- Place the path at the top or bottom of the editor.
- Customize the separator between path segments.
- Highlight important folder names with solid background colors.
- Right-click folder names to cycle highlight colors.
- Ctrl+right-click highlighted folder names to remove highlights.
- Double-click folder names to open them in File Explorer.
- Click the file name to copy only the file name.
- Double-click the file name to copy the full path.
- Use the Copy button beside the path to copy the file name.
- Use the gear button beside the path to open settings quickly.
- Customize font family, font size, background color, font color, opacity, and accent colors.

## Configuration

Open:

```text
Tools > Options > Easy File Path > General
```

You can also click the gear button beside the path line in the editor.

Available options include:

- Enabled
- Placement
- Separator
- Font family
- Font size
- Opacity percent
- Background color
- Font color
- Highlighted folders
- Accent colors

## Mouse Actions

| Target | Action | Result |
| --- | --- | --- |
| File name | Click | Copy file name |
| File name | Double-click | Copy full absolute path |
| Folder name | Double-click | Open folder in File Explorer |
| Folder name | Right-click | Cycle highlight color |
| Folder name | Ctrl+right-click | Remove highlight |
| Copy button | Click | Copy file name |
| Gear button | Click | Open settings |

## Compatibility

- Visual Studio 2022
- Visual Studio 2026

Requires Visual Studio 17.0 or later on amd64.

## Privacy

EasyFilePath does not collect telemetry and does not send file paths outside Visual Studio.

## Suggested Tags

```text
file path, editor, productivity, navigation, visual studio, vsix
```

