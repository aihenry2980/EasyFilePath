<img width="1451" height="44" alt="image" src="https://github.com/user-attachments/assets/f7874de8-ed4c-4789-87df-93bd5c45998b" />
<img width="1441" height="41" alt="image" src="https://github.com/user-attachments/assets/450ce2c8-7eb2-499b-8213-9b47195b9e91" />
<img width="1438" height="36" alt="image" src="https://github.com/user-attachments/assets/57ea0c92-1374-4016-8b31-c696644e68a0" />

# EasyFilePath

EasyFilePath is a Visual Studio extension that shows the current document's absolute file path directly inside the text editor.

It is designed for developers who often work in similar folder structures, generated source trees, cloned repositories, or large solutions where the document tab does not show enough context.

## Features

- Shows the absolute file path as overlapping rounded segments in the editor margin.
- Uses pastel backgrounds with black text for normal path segments.
- Right-clicking a folder cycles through ten dark highlight colors with white text.
- Includes an optional classic separator-based text style.
- Supports top or bottom placement.
- Keeps the path outside the code area so it does not overlap source text.
- Lets you choose the path separator, such as ` > `, `/`, `|`, or `->`.
- Lets you highlight important folder names with solid background colors.
- Right-click a folder segment to cycle through preset accent colors.
- Ctrl+right-click a highlighted folder to remove its highlight.
- Double-click a folder segment to open that folder in File Explorer.
- Click the file name to copy only the file name.
- Double-click the file name to copy the full absolute path.
- Provides a dedicated `Copy` button beside the path for copying the file name.
- Provides a gear button beside the path for quick access to settings.
- Lets you customize font family, font size, path background color, font color, opacity, and accent colors.

## Compatibility

- Visual Studio 2022
- Visual Studio 2026

The extension targets Visual Studio 17.0 and later on amd64.

## Installation

1. Build the solution in Visual Studio or with MSBuild.
2. Install the generated VSIX from:

   ```text
   bin\Debug\EasyFilePath.vsix
   ```

3. Restart Visual Studio if prompted.

For marketplace installation, install EasyFilePath from the Visual Studio Marketplace once it is published.

## Settings

Open settings from either:

- `Tools > Options > Easy File Path > General`
- The gear button shown beside the file path in the editor

Available settings:

- `Enabled`: show or hide the path line.
- `Placement`: choose top or bottom editor placement.
- `Path style`: choose rounded segments or the classic text path.
- `Default pastel colors`: configure the palette used by normal segments.
- `Highlight colors`: configure the dark palette cycled by right-clicking folders.
- `Separator`: customize text between path segments.
- `Font family`: choose the path font.
- `Font size`: choose the path font size.
- `Opacity percent`: control the path line opacity.
- `Background color`: choose the path line background.
- `Font color`: choose the normal path text color, or use `Auto`.
- `Highlighted folders`: configure folder highlight colors.
- `Accent colors`: configure the colors used when cycling folder highlights.

## Mouse Actions

| Target | Action | Result |
| --- | --- | --- |
| File name | Click | Copy file name |
| File name | Double-click | Copy full absolute path |
| Folder name | Double-click | Open folder in File Explorer |
| Folder name | Right-click | Cycle highlight color |
| Folder name | Ctrl+right-click | Remove folder highlight |
| Copy button | Click | Copy file name |
| Gear button | Click | Open EasyFilePath settings |

## Build

From a Developer PowerShell or a terminal with Visual Studio MSBuild available:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe' EasyFilePath.sln /t:Clean,Build /p:Configuration=Debug /p:Platform='Any CPU' /p:VisualStudioVersion=17.0 /p:DeployExtension=false
```

The VSIX is generated at:

```text
bin\Debug\EasyFilePath.vsix
```

## Notes

- Settings are persisted by Visual Studio and are applied when Visual Studio starts.
- Highlighted folder names are matched case-insensitively.
- Invalid color values fall back to safe defaults.




