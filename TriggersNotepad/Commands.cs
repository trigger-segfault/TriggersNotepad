using System;
using System.Windows.Input;

namespace TriggersNotepad {
	public static class Commands {

		public static readonly RoutedUICommand ToUppercase = new RoutedUICommand(
			"Uppercase", "ToUppercase", typeof(Commands),
			new InputGestureCollection() { new KeyGesture(Key.U, ModifierKeys.Control) }
		);
		public static readonly RoutedUICommand ToLowercase = new RoutedUICommand(
			"Lowercase", "ToLowercase", typeof(Commands),
			new InputGestureCollection() { new KeyGesture(Key.L, ModifierKeys.Control) }
		);
		public static readonly RoutedUICommand Goto = new RoutedUICommand(
			"Goto Line", "Goto", typeof(Commands),
			new InputGestureCollection() { new KeyGesture(Key.G, ModifierKeys.Control) }
		);
		public static readonly RoutedUICommand MySaveAs = new RoutedUICommand(
			"Save As...", "MySaveAs", typeof(Commands),
			new InputGestureCollection() { new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift) }
		);
		public static readonly RoutedUICommand Exit = new RoutedUICommand(
			"Exit", "Exit", typeof(Commands),
			new InputGestureCollection() { new KeyGesture(Key.W, ModifierKeys.Control) }
		);
		public static readonly RoutedUICommand ZoomIn = new RoutedUICommand(
			"Zoom In", "ZoomIn", typeof(Commands),
			new InputGestureCollection() { new KeyGesture(Key.Add, ModifierKeys.Control) }
		);
		public static readonly RoutedUICommand ZoomOut = new RoutedUICommand(
			"Zoom Out", "ZoomOut", typeof(Commands),
			new InputGestureCollection() { new KeyGesture(Key.Subtract, ModifierKeys.Control) }
		);
		public static readonly RoutedUICommand ResetZoom = new RoutedUICommand(
			"Reset Zoom", "ResetZoom", typeof(Commands),
			new InputGestureCollection() { new KeyGesture(Key.B, ModifierKeys.Control) }
		);
		public static readonly RoutedUICommand TabsToSpaces = new RoutedUICommand(
			"Tabs to Spaces", "TabsToSpaces", typeof(Commands)
		);
		public static readonly RoutedUICommand SpacesToTabs = new RoutedUICommand(
			"Spaces to Tabs", "SpacesToTabs", typeof(Commands)
		);
		public static readonly RoutedUICommand PreviewMarkdown = new RoutedUICommand(
			"Preview Markdown", "PreviewMarkdown", typeof(Commands),
			new InputGestureCollection() { new KeyGesture(Key.Q, ModifierKeys.Control) }
		);

	}
}
