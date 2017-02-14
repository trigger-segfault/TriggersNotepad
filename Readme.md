# TriggersNotepad
My take on a minimal (yet bulky) replacement for Windows Notepad. Trigger's Notepad improves most of the broken features in Windows Notepad and also adds plenty of new and useful features. This tool is not designed to replace programs with syntax highlighting like Notepad++. Its main goal is to be a basic text editor designed with one-window-per-file.

## Pros and Cons
There are some tradeoffs to using Trigger's PC so keep that in mind when deciding if you'd like to use it.

### Pros
* Word wrap no longer saves the wrapped text with new lines.
* Undo is limited to one use only.
* Save As now has a hotkey of Ctrl+Shift+S.
* Selected text can now be dragged like in most text editors.
* Spell Check is now available, so there's no need to copy the word somewhere else to check.
* Hyperlinks can now be viewed and clicked. No more copying them into the browser.
* Visually pleasing UI and Icons for every menu item.
* View the total lines and characters alongside current line and column on the status bar.
* Finding, replacing, and goto are no longer separate windows but built into the UI.
* Finding can be done in real-time while typing.
* Optional smart indentation can remember the last line's indentation when creating a new line.
* Optional line numbers can be shown in the left margin.
* A selection can be transitioned to all uppercase or lowercase characters. Includes hotkeys.
* The file's encoding can now be changed without having to save as.
* Installer includes optional file association for a few text files.
* Optional dark mode to invert background and foreground colors in the editor.

### Cons (for Windows Notepad)
* Can take around 10x as much memory due to it being built in WPF.
* Can start to get really slow with extremely large files.
* Because of WPF font rendering, ASCII art using full-block characters won't look right due to them not connecting together.
* Print and Page Setup were removed. I don't expect people really use those in basic text editors.
* Insert Date/Time was never implemented. If there is a demand to implement it them I can easily do that.

### Cons (for Other Editors)
* Dragging selected text is a little glitchy with the cursor. It also doesn't allow you to scroll while dragging. Blame AvalonEdit for this.
* No syntadx highlighting. This tool was never designed to replace programs like Notepad++.
