# Trigger's Notepad ![Icon](App.png)

* **Author:** Robert Jordan (trigger_death)
* **GitHub:** [https://github.com/trigger-death/TriggersNotepad](https://github.com/trigger-death/TriggersNotepad)
* **Version:** 1.0.1.0 (2/15/17)

## Features

#### Open in Notepad:
* Legacy support for quickly opening your current file in Windows Notepad.
* The file cannot be untitled and it will open the last saved version of it.
Open New Notepad:
* Opens a blank Windows Notepad.

#### Uppercase:
* Turns all of the characters in the selection into uppercase letters.
Lowercase:
* Turns all of the characters in the selection into lowercase letters.
Tabs to Spaces:
* Converts all tabs into spaces with a tab size of four spaces.
* Can only be used with monospace fonts.
Spaces to Tabs:
* Converts all spaces making up a tab at the start of a newline into tabs.
* Can only be used with monospace fonts.

#### Find:
* Opens the find bar at the bottom of the window.
* Press Enter to find the next result while editing the find text.
* The Real-Time find feature in options will automatically goto the next valid
   result while typing.
#### Replace:
* Opens the replace bar at the bottom of the window.
* Press Enter while editing the replace text to either navigate to a result or
   replace the current find result.
#### Goto Line:
* Opens the goto bar at the top of the window.
* Press enter to goto the current line.

## Format

Format is the encoding the file is saved in.

#### ANSI:
* Has no support for anything outside the first 255 characters so you'll
   need to use a UTF encoding if you want to use any special unicode characters.
#### UTF-8:
* Stands for Variable-length Unicode. This form of unicode takes up the least
   amount of space over the UTF-16's.
#### UTF-16LE:
* Stands for Unicode Little Endian. If you don't know what this means
   and you're not a programmer then the Little Endian term shouldn't be important
   to you. Use this one over UTF-16BE if you're not sure what to use.
#### UTF-16BE:
* Stands for Unicode Big Endian. Again, the term Big Endian shouldn't be
   important unless you know what it means are are a programmer.
#### Set to Default:
* Sets the default encoding to use when opening a new file or loading text

## Options

Options are saved for Trigger's Notepad when you close a window. After closing,
the settings of the closed window will be saved for all newly opened windows.

#### Smart Indentation:
* When you press enter to create a new line, the indentation at the beginning of
   the previous line will be copied over to the new line and your cursor will be
   placed at the end of it.
#### Hyperlinks:
* Hyperlinks will be highlighted in the editor. Ctrl+Click on a link to open it.

#### Auto-View Markdown:
* When opening an "md" (Markdown) file, it will start in the markdown view.
#### Real-Time Find:
* When typing in the find text box, the editor will automatically try to navigate
   to the next valid result.
#### Auto-Close Goto:
* After going to a specific line, the goto bar will automatically hide itself.

#### Save Options on Close:
* Disabling this will allow changes to be made to options without keeping them as 
   defaults
#### Save Options:
* Saves the options for use with all newly opened windows. This way you don't
   need to clsoe a window to save them.
* Best used with "Save Options on Close" disabled.


## File Association

The following file types will be associated with Trigger's Notepad unless you opt
out during the installation.

* **.txt** | Text Documents
* **.log** | Log Files
* **.ini** | Configuration Settings
* **.inf** | Setup Information
* **.md** | Markdown Documents


## Hotkeys

#### File:
* **New** | Ctrl+N
* **Open** | Ctrl+O
* **Save** | Ctrl+S
* **Save As** | Ctrl+Shift+S
* **Exit** | Ctrl+W

#### Edit:
* **Undo** | Ctrl+Z
* **Redo** | Ctrl+Y
* **Cut** | Ctrl+X
* **Copy** | Ctrl+C
* **Paste** | Ctrl+V
* **Delete** | Delete
* **Uppercase** | Ctrl+U
* **Lowercase** | Ctrl+L
* **Fin** | Ctrl+F
* **Replace** | Ctrl+H
* **Goto Line** | Ctrl+G
* **Select All** | Ctrl+A

#### View:
* **Zoom In** | Ctrl+Num+/Ctrl+Wheel Up
* **Zoom Out** | Ctrl+Num-/Ctrl+Wheel Down
* **Reset Zoom** | Ctrl+B


## Command Line

* **TriggersNotepad.exe %1** | Open %1 as a text file.

* **TriggersNotepad.exe /md %1** | Opens %1 as a text file and previews it in Markdown.

* **TriggersNotepad.exe /t %1** | Load the text of %1.
