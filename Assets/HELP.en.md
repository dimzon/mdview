# mdview usage

**mdview** is a lightweight Markdown viewer for Windows (.NET 8 + WPF).
Markdown is converted into a standard `FlowDocument` and rendered with native WPF controls.
Here is everything you need to know.

## Opening files

| Way | How |
|---|---|
| Menu | File → Open… (Ctrl+O) |
| Drag & drop | drag a `.md` file into the window |
| Command line | `mdview.exe readme.md` — several files are accepted (first opens) |
| Refresh | F5 re-reads the current file from disk |

Supported extensions: `.md`, `.markdown`, `.mdown`, `.mkd`, `.txt`.

## Supported markup

- Headings h1–h6, paragraphs, lists (bulleted, ordered, nested)
- Task lists `- [x]` / `- [ ]` with a unified checkbox style
- Tables with column alignment (`:---`, `:---:`, `---:`)
- Code blocks with a **Copy button** (icon) and **syntax highlighting**
- Blockquotes `>`, horizontal rules `---`
- Formatting: **bold**, *italic*, ~~strikethrough~~, `inline code`,
  superscript/subscript (`x^2^`, `H~2~O`)
- Links: external URLs open in the browser, **relative `.md` links open in this window**
- Images: local (relative to the file's folder) and over http/https

### Syntax highlighting

- Enabled by default; toggle in View → Code highlighting.
- The palette follows the theme: **VS Code Light+** / **VS Code Dark+**.
- Highlighted: comments, strings, numbers, keywords, function calls.
- Long lines do not wrap — a horizontal scrollbar appears instead.

## Table of contents

Ctrl+T toggles the sidebar with document headings. Click a heading to jump to it.

## Themes and language

- View → Theme: **Auto** (follows Windows), **Light**, **Dark**. The choice is remembered.
- View → Language: **System default**, English, Русский. Switching is instant.
- Dark theme also themes the menus, scrollbars, and the window title bar.

## Print and export

- **Ctrl+P** — print the current document (standard print dialog).
- **Ctrl+S** — save the document as an XPS file.

## Keyboard shortcuts

| Keys | Action |
|---|---|
| Ctrl+O | Open file |
| F5 | Refresh current file |
| Ctrl+T | Table of contents |
| Ctrl+P | Print |
| Ctrl+S | Save as XPS |
| Ctrl+mouse wheel | Zoom |
| Ctrl + / Ctrl − | Zoom |
| Ctrl+0 | Reset zoom |
| Alt+F4 | Exit |

## Repository

Source code, BSD-2-Clause license and ready-made builds:
Help → Open GitHub repository.
