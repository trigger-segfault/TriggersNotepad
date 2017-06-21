using ICSharpCode.AvalonEdit.Indentation;
using Martin;
using Microsoft.Win32;
using NHunspell;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Reflection;
using TriggersNotepad.Properties;
using Markdown.Xaml;

using IOPath = System.IO.Path;
using System.Windows.Documents;
using System.Diagnostics;
using ICSharpCode.AvalonEdit;
using System.Windows.Threading;
using System.Globalization;

namespace TriggersNotepad {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		#region Members

		// Settings
		bool spellCheck;
		bool showStatusBar;
		bool realTimeFind;
		bool smartIndent;
		bool darkMode;
		bool closeGoto;
		bool closeOptions;
		bool autoPreviewMarkdown;
		string encodingDefault;
		int tabSize;

		// Zoom
		double fontSize;
		int fontSizeDelta;

		// Markdown
		bool viewingMarkdown;

		// File Info
		string lastDirectory;
		string filePath;
		bool hasSaved;
		bool newFile;
		Encoding encoding;

		// Used to prevent certain events from firing
		bool locked;
		bool loading;

		// Spell Checker
		SpellCheckerColorizer spellChecker;

		// Searching & Find Bar
		bool searching;
		bool matchCase;
		bool searchFocus;
		int updateSearch;
		ColorizeSearchResults searchColorizer;
		Storyboard findBarStoryboard;
		Storyboard gotoBarStoryboard;

		static readonly string[] FileTypes = {
			"Text Document", "*.txt",
			"Log File", "*.log",
			"Configuration Settings", "*.ini",
			"Setup Information", "*.inf",
			"Markdown File", "*.md"
		};

		#endregion

		#region Constructor

		public MainWindow() {
			locked = true;
			loading = true;

			hasSaved = true;
			newFile = true;
			findBarStoryboard = new Storyboard();
			gotoBarStoryboard = new Storyboard();
			findBarStoryboard.Completed += OnFindBarStoryboardCompleted;
			gotoBarStoryboard.Completed += OnGotoBarStoryboardCompleted;
			searchColorizer = new ColorizeSearchResults();

			searching = false;
			matchCase = false;
			updateSearch = 0;
			fontSizeDelta = 0;
			tabSize = 4;


			InitializeComponent();

			labelMatches.Content = "";
			findBar.Height = 0;
			gotoBar.Height = 0;

			// Spell Check
			string affPath = IOPath.Combine(
					IOPath.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
					@"Dictionaries\en_US.aff"
				);
			string dicPath = IOPath.Combine(
					IOPath.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
					@"Dictionaries\en_US.dic"
				);
			SpellChecker.Default.HunspellInstance = new Hunspell(affPath, dicPath);
			spellChecker = new SpellCheckerColorizer();
			textEditor.TextArea.Caret.PositionChanged += OnTextEditorCaretMoved;

			// Searching
			textEditor.TextArea.TextView.LineTransformers.Insert(0, searchColorizer);

			// Misc
			textEditor.TextArea.Margin = new Thickness(4, 4, 0, 4);
			textEditor.TextArea.TextView.NonPrintableCharacterBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
			textEditor.TextArea.TextView.Options.AllowScrollBelowDocument = true;

			// Selection Style
			//textEditor.TextArea.Cursor = Cursors.IBeam;
			textEditor.TextArea.SelectionCornerRadius = 0;
			textEditor.TextArea.SelectionBorder = null;

			// Load Settings
			textEditor.FontFamily = (FontFamily)new FontFamilyConverter().ConvertFromString(Settings.Default.FontFamily);
			textEditor.FontSize = Settings.Default.FontSize;
			fontSize = textEditor.FontSize;
			textEditor.FontWeight = (FontWeight)new FontWeightConverter().ConvertFromString(Settings.Default.FontWeight);
			textEditor.FontStyle = (FontStyle)new FontStyleConverter().ConvertFromString(Settings.Default.FontStyle);
			textEditor.FontStretch = (FontStretch)new FontStretchConverter().ConvertFromString(Settings.Default.FontStretch);

			textEditor.WordWrap = Settings.Default.WordWrap;
			spellCheck = Settings.Default.SpellCheck;
			textEditor.Options.EnableHyperlinks = Settings.Default.Hyperlinks;
			showStatusBar = Settings.Default.StatusBar;
			textEditor.ShowLineNumbers = Settings.Default.LineNumbers;
			realTimeFind = Settings.Default.RealTimeFind;
			smartIndent = Settings.Default.SmartIndent;
			textEditor.Options.EnableTextDragDrop = Settings.Default.TextDragging;
			darkMode = Settings.Default.DarkMode;
			closeGoto = Settings.Default.CloseGoto;
			closeOptions = Settings.Default.CloseOptions;
			encodingDefault = Settings.Default.EncodingDefault;
			autoPreviewMarkdown = Settings.Default.AutoPreviewMarkdown;

			this.Width = Settings.Default.WindowWidth;
			this.Height = Settings.Default.WindowHeight;

			// Init Settings
			menuItemWordWrap.IsChecked = textEditor.WordWrap;
			if (spellCheck) textEditor.TextArea.TextView.LineTransformers.Add(spellChecker);
			menuItemSpellCheck.IsChecked = spellCheck;
			menuItemHyperlinks.IsChecked = textEditor.Options.EnableHyperlinks;
			textEditor.Options.EnableEmailHyperlinks = textEditor.Options.EnableHyperlinks;
			menuItemStatusBar.IsChecked = showStatusBar;
			statusBar.Visibility = (showStatusBar ? Visibility.Visible : Visibility.Collapsed);
			menuItemLineNumbers.IsChecked = textEditor.ShowLineNumbers;
			menuItemRealTimeFind.IsChecked = realTimeFind;
			menuItemSmartIndent.IsChecked = smartIndent;
			if (!smartIndent)
				textEditor.TextArea.IndentationStrategy = new NoIndentationStrategy();
			menuItemTextDragging.IsChecked = textEditor.Options.EnableTextDragDrop;
			menuItemDarkMode.IsChecked = darkMode;
			if (darkMode)
				EnableDarkMode();
			menuItemAutoCloseGoto.IsChecked = closeGoto;
			menuItemCloseOptions.IsChecked = closeOptions;
			menuItemAutoPreviewMarkdown.IsChecked = autoPreviewMarkdown;

			SetEncoding(Encoding.Default);
			
			// Enable hyperlinks in Markdown
			CommandBindings.Add(new CommandBinding(NavigationCommands.GoToPage, (sender, e) => Process.Start((string)e.Parameter)));

			// Hackish fix for taking control of ctrl+scroll zoom
			markdownViewer.MaxZoom = 100;
			markdownViewer.MinZoom = 100;

			// Fix flow document slow scroll speed
			FlowDocumentExtensions.SetScrollSpeed(markdownViewer, 2.2);

			// Load a file if specified\
			string[] arguments = Environment.GetCommandLineArgs();
			if (arguments.Length >= 2) {
				if (arguments[1] == "/t" && arguments.Length >= 3) {
					LoadText(arguments[2]);
				}
				else if (arguments[1] == "/md" && arguments.Length >= 3) {
					Load(arguments[2], true);
				}
				else {
					Load(arguments[1]);
				}
			}
			else {
				SetEncodingToDefault();
			}

			locked = false;
			loading = false;

			textEditor.Focus();
		}

		#endregion

		#region Short Commands

		private void Redraw() {
			textEditor.TextArea.TextView.Redraw();
		}
		private StringComparison MatchComparison {
			get { return (matchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase); }
		}

		#endregion

		#region Window

		private async void OnWindowLoaded(object sender, RoutedEventArgs e) {
			//await Task.Run(() => RunTextSearcher());
		}

		private void OnClosing(object sender, CancelEventArgs e) {
			if (!hasSaved) {
				e.Cancel = !SaveChanges();
			}
			if (!e.Cancel && closeOptions) {
				SaveOptions();
			}
		}

		private void OnExitClicked(object sender, ExecutedRoutedEventArgs e) {
			Close();
		}

		private void UpdateTitle() {
			string name = (newFile ? "Untitled" : IOPath.GetFileName(filePath)) + (hasSaved ? "" : "*");
			this.Title = name + " - Trigger's Notepad";
		}
		private void OnAboutClicked(object sender, RoutedEventArgs e) {
			AboutWindow about = new AboutWindow();
			about.Owner = this;
			about.ShowDialog();
		}

		#endregion

		#region File IO

		/*private void CanExecuteSave(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = true;
			e.Handled = true;
		}*/

		private void OnNew(object sender, ExecutedRoutedEventArgs e) {
			if (SaveChanges()) {
				locked = true;
				textEditor.Clear();
				IncrementSearchUpdate();
				locked = false;
				filePath = "";
				newFile = true;
				hasSaved = true;
				encoding = Encoding.Default;
				UpdateTitle();
				UpdateStatusBarTextEditor();
				SetEncodingToDefault();
			}
		}
		private void OnOpen(object sender, ExecutedRoutedEventArgs e) {
			if (SaveChanges()) {
				OpenFileDialog fileDialog = new OpenFileDialog();
				fileDialog.Title = "Open";
				fileDialog.AddExtension = true;
				fileDialog.DefaultExt = ".txt";
				fileDialog.FilterIndex = 0;
				//fileDialog.FileName = (newFile ? "*.txt" : IOPath.GetFileName(filePath));
				fileDialog.InitialDirectory = lastDirectory;
				string filter = "";
				string allTextFilers = "All Text Files|";
				for (int i = 0; i < FileTypes.Length / 2; i++) {
					filter += FileTypes[i * 2] + " (" + FileTypes[i * 2 + 1] + ")|" + FileTypes[i * 2 + 1] + "|";
					allTextFilers += FileTypes[i * 2 + 1] + (i + 1 < FileTypes.Length / 2 ? ";" : "|");
				}
				fileDialog.Filter = allTextFilers + filter + "All Files (*.*)|*.*";
				var result = fileDialog.ShowDialog(this);
				if (result.HasValue && result.Value) {
					Load(fileDialog.FileName);
				}
			}
		}
		private bool Load(string path, bool previewMarkdown = false) {
			try {
				locked = true;
				textEditor.Load(path);
				IncrementSearchUpdate();
				locked = false;
				filePath = path;
				lastDirectory = IOPath.GetDirectoryName(filePath);
				hasSaved = true;
				newFile = false;
				menuItemNotepad.IsEnabled = true;
				SetEncoding(GetEncoding(filePath));
				UpdateTitle();
				UpdateStatusBarTextEditor();
				if ((IOPath.GetExtension(path) == ".md" && autoPreviewMarkdown) || previewMarkdown) {
					PreviewMarkdown();
				}
				else if (viewingMarkdown) {
					LeaveMarkdown();
				}
				return true;
			}
			catch (FileNotFoundException) {
				TriggerMessageBox.Show(loading ? null : this, MessageIcon.Warning, "Could not open the specified file. File not found!", "Open File");
				locked = false;
			}
			catch (Exception exc) {
				TriggerMessageBox.Show(loading ? null : this, MessageIcon.Warning, "Could not open the specified file!\n" + exc.Message, "Open File");
				locked = false;
			}
			return false;
		}

		private void LoadText(string text) {
			locked = true;
			int oldSizeLimit = textEditor.Document.UndoStack.SizeLimit;
			textEditor.Document.UndoStack.SizeLimit = 0;
			textEditor.Text = text;
			textEditor.Document.UndoStack.SizeLimit = oldSizeLimit;
			IncrementSearchUpdate();
			locked = false;

			filePath = "";
			newFile = true;
			hasSaved = true;
			encoding = Encoding.Default;
			UpdateTitle();
			UpdateStatusBarTextEditor();
			SetEncodingToDefault();
		}

		private void OnSave(object sender, ExecutedRoutedEventArgs e) {
			Save();
		}
		private void OnSaveAs(object sender, ExecutedRoutedEventArgs e) {
			SaveAs();
		}

		private bool SaveChanges() {
			if (!hasSaved) {
				var result = TriggerMessageBox.Show(this, MessageIcon.Question, "Do you want to save changes to " + (newFile ? "Untitled" : filePath) + "?", "Unsaved Changes", MessageBoxButton.YesNoCancel);
				if (result != MessageBoxResult.Cancel) {
					return (result == MessageBoxResult.No || Save());
				}
				return false;
			}
			return true;
		}

		private bool Save() {
			if (newFile) {
				return SaveAs();
			}
			else {
				return SaveEncoding();
			}
		}

		private bool SaveAs() {
			SaveFileDialog fileDialog = new SaveFileDialog();
			fileDialog.Title = "Save As";
			fileDialog.AddExtension = true;
			fileDialog.DefaultExt = IOPath.GetExtension(filePath);
			if (fileDialog.DefaultExt.Length == 0)
				fileDialog.DefaultExt = ".txt";
			fileDialog.FileName = IOPath.GetFileName(filePath);
			fileDialog.InitialDirectory = lastDirectory;
			string filter = "";
			for (int i = 0; i < FileTypes.Length / 2; i++) {
				filter += FileTypes[i * 2] + " (" + FileTypes[i * 2 + 1] + ")|" + FileTypes[i * 2 + 1] + "|";
			}
			fileDialog.Filter = filter + "All Files (*.*)|*.*";
			var result = fileDialog.ShowDialog(this);
			if (result.HasValue && result.Value) {
				filePath = fileDialog.FileName;
				if (SaveEncoding()) {

					if (newFile) {
						this.Title = IOPath.GetFileName(filePath) + " - Trigger's Notepad";
						menuItemNotepad.IsEnabled = true;
						newFile = false;
					}
					return true;
				}
			}
			return false;
		}

		private bool SaveEncoding() {
			string text = textEditor.Text;
			MessageBoxResult result = MessageBoxResult.OK;
			if (encoding == Encoding.Default) {
				foreach (char c in text) {
					if (c > 255) {
						result = TriggerMessageBox.Show(this, MessageIcon.Warning,
							"This file contains characters in Unicode format which will " +
							"be lost if you save this file as an ANSI encoded text file. " +
							"To keep Unicode information, change the encoding in the menu to one of the UTF encodings.",
							"Encoding Issue", MessageBoxButton.OKCancel
						);
						break;
					}
				}
			}
			if (result == MessageBoxResult.OK) {
				SaveFinal();
				return true;
			}
			return false;
		}

		private void SaveFinal() {
			textEditor.Save(filePath);
			hasSaved = true;
			UpdateTitle();
		}

		#endregion

		#region Find Bar

		private void OnOpenFindBarExecuted(object sender, ExecutedRoutedEventArgs e) {
			if (viewingMarkdown)
				return;

			if (!menuItemFind.IsChecked) {
				menuItemFind.IsChecked = true;
				menuItemReplace.IsChecked = false;
				textBoxFind.IsEnabled = true;
				textBoxReplace.IsEnabled = false;
				searching = true;
				IncrementSearchUpdate();

				findBarStoryboard.Children.Clear();
				DoubleAnimation anim = new DoubleAnimation(findBar.Height, 31, TimeSpan.FromSeconds(0.1));
				anim.BeginTime = TimeSpan.Zero;
				Storyboard.SetTarget(anim, findBar);
				Storyboard.SetTargetProperty(anim, new PropertyPath(Border.HeightProperty));
				findBarStoryboard.Children.Add(anim);
				findBarStoryboard.Begin(this, true);

			}
			textBoxFind.SelectAll();
			textBoxFind.Focus();
		}
		private void OnOpenReplaceBarExecuted(object sender, ExecutedRoutedEventArgs e) {
			if (viewingMarkdown)
				return;

			if (!menuItemReplace.IsChecked) {
				menuItemReplace.IsChecked = true;
				menuItemFind.IsChecked = false;
				textBoxFind.IsEnabled = true;
				textBoxReplace.IsEnabled = true;
				searching = true;
				IncrementSearchUpdate();

				findBarStoryboard.Children.Clear();
				DoubleAnimation anim = new DoubleAnimation(findBar.Height, 56, TimeSpan.FromSeconds(0.1));
				anim.BeginTime = TimeSpan.Zero;
				Storyboard.SetTarget(anim, findBar);
				Storyboard.SetTargetProperty(anim, new PropertyPath(Border.HeightProperty));
				findBarStoryboard.Children.Add(anim);
				findBarStoryboard.Begin(this, true);
			}
			textBoxFind.SelectAll();
			textBoxFind.Focus();
		}
		private void OnCloseFindBarPressed(object sender, RoutedEventArgs e) {
			menuItemFind.IsChecked = false;
			menuItemReplace.IsChecked = false;
			textBoxFind.IsEnabled = false;
			textBoxReplace.IsEnabled = false;
			textBoxFind.Text = "";
			textBoxReplace.Text = "";
			searchColorizer.SearchTerm = "";
			searching = false;

			findBarStoryboard.Children.Clear();
			DoubleAnimation anim = new DoubleAnimation(findBar.Height, 0, TimeSpan.FromSeconds(0.1));
			anim.BeginTime = TimeSpan.Zero;
			Storyboard.SetTarget(anim, findBar);
			Storyboard.SetTargetProperty(anim, new PropertyPath(Border.HeightProperty));
			findBarStoryboard.Children.Add(anim);
			findBarStoryboard.Begin(this, true);
		}
		private void OnFindBarStoryboardCompleted(object sender, EventArgs e) {
			if (!searching) {
				Keyboard.ClearFocus();
				textEditor.Focus();
			}
		}

		#endregion

		#region Settings
		
		private void SaveOptions() {
			Settings.Default.FontFamily = textEditor.FontFamily.ToString();
			Settings.Default.FontSize = fontSize;
			Settings.Default.FontWeight = textEditor.FontWeight.ToString();
			Settings.Default.FontStyle = textEditor.FontStyle.ToString();
			Settings.Default.FontStretch = textEditor.FontStretch.ToString();

			Settings.Default.WordWrap = textEditor.WordWrap;
			Settings.Default.LineNumbers = textEditor.ShowLineNumbers;
			Settings.Default.SmartIndent = smartIndent;
			Settings.Default.TextDragging = textEditor.Options.EnableTextDragDrop;
			Settings.Default.SpellCheck = spellCheck;
			Settings.Default.Hyperlinks = textEditor.Options.EnableHyperlinks;

			Settings.Default.AutoPreviewMarkdown = autoPreviewMarkdown;
			Settings.Default.RealTimeFind = realTimeFind;
			Settings.Default.CloseGoto = closeGoto;
			Settings.Default.DarkMode = darkMode;
			Settings.Default.StatusBar = showStatusBar;

			Settings.Default.CloseOptions = closeOptions;

			Settings.Default.EncodingDefault = encodingDefault;

			Settings.Default.WindowWidth = (int)this.Width;
			Settings.Default.WindowHeight = (int)this.Height;
			Settings.Default.Save();
		}
		private void OnWordWrapClicked(object sender, RoutedEventArgs e) {
			textEditor.WordWrap = (sender as MenuItem).IsChecked;
		}
		private void OnSpellCheckClicked(object sender, RoutedEventArgs e) {
			spellCheck = (sender as MenuItem).IsChecked;
			if (spellCheck)
				textEditor.TextArea.TextView.LineTransformers.Add(spellChecker);
			else
				textEditor.TextArea.TextView.LineTransformers.Remove(spellChecker);
		}
		private void OnEnableHyperlinksClicked(object sender, RoutedEventArgs e) {
			textEditor.Options.EnableHyperlinks = (sender as MenuItem).IsChecked;
			textEditor.Options.EnableEmailHyperlinks = textEditor.Options.EnableHyperlinks;
		}
		private void OnStatusBarClicked(object sender, RoutedEventArgs e) {
			showStatusBar = (sender as MenuItem).IsChecked;
			statusBar.Visibility = (showStatusBar ? Visibility.Visible : Visibility.Collapsed);
		}
		private void OnFontClicked(object sender, RoutedEventArgs e) {
			var fontDialog = new FontDialogSample.FontChooser();
			fontDialog.SelectedFontFamily = textEditor.FontFamily;
			fontDialog.SelectedFontSize = fontSize;
			fontDialog.SelectedFontWeight = textEditor.FontWeight;
			fontDialog.SelectedFontStyle = textEditor.FontStyle;
			fontDialog.SelectedFontStretch = textEditor.FontStretch;
			//fontDialog.SelectedTextDecorations = textEditor.TextDecorations;

			var result = fontDialog.ShowDialog();
			if (result.HasValue && result.Value) {
				fontDialog.ApplyPropertiesToObject(textEditor);
				fontSize = textEditor.FontSize;
				SetFontSizeDelta(0);
			}
		}
		private void OnLineNumbersClicked(object sender, RoutedEventArgs e) {
			textEditor.ShowLineNumbers = (sender as MenuItem).IsChecked;
		}
		private void OnRealTimeFindClicked(object sender, RoutedEventArgs e) {
			realTimeFind = (sender as MenuItem).IsChecked;
		}
		private void OnSmartIndentationClicked(object sender, RoutedEventArgs e) {
			bool enabled = (sender as MenuItem).IsChecked;
			if (enabled)
				textEditor.TextArea.IndentationStrategy = new DefaultIndentationStrategy();
			else
				textEditor.TextArea.IndentationStrategy = new NoIndentationStrategy();
		}
		private void OnTextDraggingClicked(object sender, RoutedEventArgs e) {
			textEditor.Options.EnableTextDragDrop = (sender as MenuItem).IsChecked;
		}
		private void OnDarkModeClicked(object sender, RoutedEventArgs e) {
			darkMode = (sender as MenuItem).IsChecked;

			if (darkMode)
				EnableDarkMode();
			else
				EnableLightMode();

			Redraw();
		}
		private void EnableDarkMode() {
			textEditor.TextArea.SelectionForeground = new SolidColorBrush(Color.FromRgb(112, 183, 255));
			textEditor.TextArea.SelectionBrush = Brushes.White;
			textEditor.Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200));
			textEditor.Background = new SolidColorBrush(Color.FromRgb(24, 24, 24));
			textEditor.TextArea.TextView.LinkTextForegroundBrush = new SolidColorBrush(Color.FromRgb(80, 200, 230));
			textEditor.LineNumbersForeground = new SolidColorBrush(Color.FromRgb(140, 140, 140));
			searchColorizer.Foreground = new SolidColorBrush(Color.FromRgb(255, 150, 255));
		}
		private void EnableLightMode() {
			textEditor.TextArea.SelectionForeground = Brushes.White;
			textEditor.TextArea.SelectionBrush = new SolidColorBrush(Color.FromRgb(112, 183, 255));
			textEditor.Foreground = Brushes.Black;
			textEditor.Background = Brushes.White;
			textEditor.TextArea.TextView.LinkTextForegroundBrush = Brushes.Blue;
			textEditor.LineNumbersForeground = Brushes.Gray;
			searchColorizer.Foreground = Brushes.Magenta;
		}

		private void OnAutoCloseGotoClicked(object sender, RoutedEventArgs e) {
			closeGoto = (sender as MenuItem).IsChecked;
		}
		private void OnResetOptionsClicked(object sender, RoutedEventArgs e) {
			var result = TriggerMessageBox.Show(this, MessageIcon.Question, "Are you sure you want to reset all options to their defaults?", "Reset Options", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.Yes) {
				Settings.Default.Reset();

				textEditor.WordWrap = Settings.Default.WordWrap;
				menuItemWordWrap.IsChecked = textEditor.WordWrap;

				textEditor.ShowLineNumbers = Settings.Default.LineNumbers;
				menuItemLineNumbers.IsChecked = textEditor.ShowLineNumbers;

				smartIndent = Settings.Default.SmartIndent;
				menuItemSmartIndent.IsChecked = smartIndent;
				if (smartIndent)
					textEditor.TextArea.IndentationStrategy = new NoIndentationStrategy();
				else
					textEditor.TextArea.IndentationStrategy = new DefaultIndentationStrategy();

				textEditor.Options.EnableTextDragDrop = Settings.Default.TextDragging;
				menuItemTextDragging.IsChecked = textEditor.Options.EnableTextDragDrop;

				spellCheck = Settings.Default.SpellCheck;
				menuItemSpellCheck.IsChecked = spellCheck;
				if (!spellCheck && textEditor.TextArea.TextView.LineTransformers.Contains(spellChecker))
					textEditor.TextArea.TextView.LineTransformers.Remove(spellChecker);
				else if (spellCheck && !textEditor.TextArea.TextView.LineTransformers.Contains(spellChecker))
					textEditor.TextArea.TextView.LineTransformers.Add(spellChecker);

				textEditor.Options.EnableHyperlinks = Settings.Default.Hyperlinks;
				menuItemHyperlinks.IsChecked = textEditor.Options.EnableHyperlinks;
				textEditor.Options.EnableEmailHyperlinks = textEditor.Options.EnableHyperlinks;

				textEditor.FontFamily = (FontFamily)new FontFamilyConverter().ConvertFromString(Settings.Default.FontFamily);
				fontSize = textEditor.FontSize;
				SetFontSizeDelta(0);
				textEditor.FontSize = Settings.Default.FontSize;
				textEditor.FontWeight = (FontWeight)new FontWeightConverter().ConvertFromString(Settings.Default.FontWeight);
				textEditor.FontStyle = (FontStyle)new FontStyleConverter().ConvertFromString(Settings.Default.FontStyle);
				textEditor.FontStretch = (FontStretch)new FontStretchConverter().ConvertFromString(Settings.Default.FontStretch);

				autoPreviewMarkdown = Settings.Default.AutoPreviewMarkdown;
				menuItemAutoPreviewMarkdown.IsChecked = autoPreviewMarkdown;

				realTimeFind = Settings.Default.RealTimeFind;
				menuItemRealTimeFind.IsChecked = realTimeFind;

				closeGoto = Settings.Default.CloseGoto;
				menuItemAutoCloseGoto.IsChecked = closeGoto;

				darkMode = Settings.Default.DarkMode;
				menuItemDarkMode.IsChecked = darkMode;
				if (darkMode)
					EnableDarkMode();
				else
					EnableLightMode();

				showStatusBar = Settings.Default.StatusBar;
				menuItemStatusBar.IsChecked = showStatusBar;

				encodingDefault = Settings.Default.EncodingDefault;

				UpdateTextEditorFontSizeDelta();

				Redraw();
			}
		}

		private void OnSaveOptionsClicked(object sender, RoutedEventArgs e) {
			SaveOptions();
		}
		private void OnCloseOptionsClicked(object sender, RoutedEventArgs e) {
			closeOptions = (sender as MenuItem).IsChecked;
		}
		private void OnSetEncodingDefaultClicked(object sender, RoutedEventArgs e) {
			for (int i = 0; i < 4; i++) {
				MenuItem menuItem = menuFormat.Items[i] as MenuItem;
				if (menuItem.IsChecked) {
					encodingDefault = menuItem.Header as string;
					break;
				}
			}
		}
		private void SetEncodingToDefault() {
			for (int i = 0; i < 4; i++) {
				MenuItem menuItem = menuFormat.Items[i] as MenuItem;
				if (menuItem.Header as string == encodingDefault)
					menuItem.IsChecked = true;
				else
					menuItem.IsChecked = false;
			}
		}

		#endregion

		#region Status Bar

		private void UpdateStatusBarTextEditor() {
			textBlockTotals.Text = "Lines " + textEditor.LineCount + ", Chars " + textEditor.Text.Length;
			textBlockPosition.Text = "Line " + textEditor.TextArea.Caret.Position.Line + ", Col " + textEditor.TextArea.Caret.Position.Column;
		}
		private void UpdateStatusBarZoom() {
			int zoom = (int)Math.Floor(GetFontZoom(fontSizeDelta) * 100);
			if (viewingMarkdown)
				zoom = (int)markdownViewer.Zoom;
			textBlockZoom.Text = "View " + zoom + "%";
		}

		#endregion

		#region Legacy Notepad Support

		private void OnOpenInNotepadClicked(object sender, RoutedEventArgs e) {
			System.Diagnostics.Process.Start("Notepad.exe", filePath);
		}

		private void OnOpenNewNotepadClicked(object sender, RoutedEventArgs e) {
			System.Diagnostics.Process.Start("Notepad.exe");
		}

		#endregion

		#region Encoding

		private void OnANSIEncodingClicked(object sender, RoutedEventArgs e) {
			SetEncoding(Encoding.Default);
		}
		private void OnUTF7EncodingClicked(object sender, RoutedEventArgs e) {
			SetEncoding(Encoding.UTF7);
		}
		private void OnUTF8EncodingClicked(object sender, RoutedEventArgs e) {
			SetEncoding(Encoding.UTF8);
		}
		private void OnUTF16LEEncodingClicked(object sender, RoutedEventArgs e) {
			SetEncoding(Encoding.Unicode);
		}
		private void OnUTF16BEEncodingClicked(object sender, RoutedEventArgs e) {
			SetEncoding(Encoding.BigEndianUnicode);
		}

		private void SetEncoding(Encoding encoding) {
			this.encoding = encoding;
			textEditor.Encoding = encoding;
			encodingANSI.IsChecked = (encoding == Encoding.Default);
			encodingUTF8.IsChecked = (encoding == Encoding.UTF8);
			//encodingUTF7.IsChecked = (encoding == Encoding.UTF7);
			encodingUTF16LE.IsChecked = (encoding == Encoding.Unicode);
			encodingUTF16BE.IsChecked = (encoding == Encoding.BigEndianUnicode);
		}
		public Encoding GetEncoding(string filename) {
			// Read the BOM
			var bom = new byte[4];
			using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read)) {
				file.Read(bom, 0, 4);
			}

			// Analyze the BOM
			//if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Encoding.UTF7;
			if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Encoding.UTF8;
			if (bom[0] == 0xff && bom[1] == 0xfe) return Encoding.Unicode; //UTF-16LE
			if (bom[0] == 0xfe && bom[1] == 0xff) return Encoding.BigEndianUnicode; //UTF-16BE
																					//if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Encoding.UTF32;
			return Encoding.Default;
		}

		#endregion

		#region Replacing

		private void OnReplacePreviewKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter) {
				OnReplaceClicked(null, null);
			}
		}
		private void OnReplaceClicked(object sender, RoutedEventArgs e) {
			if (!searchFocus) {
				FindOffset(true, textEditor.SelectionStart + textEditor.SelectionLength);
			}
			//else if (searchCount > 0) {
			else {// if (textEditor.Text.IndexOf(textBoxFind.Text, MatchComparison) != -1) {
				string search = textBoxFind.Text;
				string replace = textBoxReplace.Text;
				string text = textEditor.Text;
				locked = true;
				int start = textEditor.SelectionStart;
				//text = text.Remove(start, search.Length);
				//text = text.Insert(start, replace);
				textEditor.Document.Replace(start, search.Length, replace);
				//textEditor.Text = text;
				textEditor.CaretOffset = start + replace.Length;
				locked = false;
				searchFocus = false;
				FindOffset(true, textEditor.CaretOffset + textBoxReplace.Text.Length);
				IncrementSearchUpdate();
			}
		}
		private void OnReplaceAllClicked(object sender, RoutedEventArgs e) {
			string search = textBoxFind.Text;
			string replace = textBoxReplace.Text;
			string text = textEditor.Text;
			int count = 0;
			int start = 0;
			int index;
			while ((index = text.IndexOf(search, start, MatchComparison)) >= 0) {
				count++;
				start = index + search.Length;
			}
			if (count > 0) {
				var result = TriggerMessageBox.Show(this, MessageIcon.Question,
					"Are you sure you want to replace " + count + " instance" +
					(count > 1 ? "s" : "") + " of the search term?",
					"Replace All", MessageBoxButton.YesNo
				);
				if (result == MessageBoxResult.Yes) {
					if (!matchCase) {
						Regex regex = new Regex(search, RegexOptions.IgnoreCase);
						//textEditor.Text = regex.Replace(text, replace);
						textEditor.Document.Replace(0, textEditor.Text.Length, regex.Replace(text, replace));
					}
					else {
						//textEditor.Text = text.Replace(search, replace);
						textEditor.Document.Replace(0, textEditor.Text.Length, text.Replace(search, replace));
					}
					searchFocus = false;
					IncrementSearchUpdate();
				}
			}
		}

		#endregion

		#region Editor Changes

		private void OnTextChanged(object sender, EventArgs e) {
			if (locked)
				return;

			if (hasSaved) {
				hasSaved = false;
				UpdateTitle();
			}
			searchFocus = false;
			UpdateStatusBarTextEditor();
			IncrementSearchUpdate();
		}
		private void OnTextInput(object sender, TextCompositionEventArgs e) {

		}
		private void OnTextEditorCaretMoved(object sender, EventArgs e) {
			if (locked)
				return;

			searchFocus = false;
			UpdateStatusBarTextEditor();
		}

		#endregion

		#region Finding

		private void OnMatchCaseClicked(object sender, RoutedEventArgs e) {
			matchCase = ((sender as ToggleButton).IsChecked.HasValue && (sender as ToggleButton).IsChecked.Value);
			searchColorizer.MatchCase = matchCase;
			Redraw();
			IncrementSearchUpdate();
		}
		private void OnFindTextBoxChanged(object sender, TextChangedEventArgs e) {
			searchColorizer.SearchTerm = textBoxFind.Text;
			Redraw();
			IncrementSearchUpdate();
			locked = true;
			textEditor.SelectionLength = 0;
			locked = false;
			searchFocus = false;
			if (realTimeFind) {
				OnFindNextClicked(null, null);
			}
		}
		private void OnFindPreviewKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter) {
				OnFindNextClicked(null, null);
			}
		}
		private void OnFindNextClicked(object sender, RoutedEventArgs e) {
			FindOffset(true, textEditor.SelectionStart + textEditor.SelectionLength);
		}
		private void OnFindPreviousClicked(object sender, RoutedEventArgs e) {
			FindOffset(false, textEditor.SelectionStart);
		}

		private void FindOffset(bool next, int startIndex) {
			string text = textEditor.Text;
			string search = textBoxFind.Text;

			// We're finding wrap index now so we don't have to do it again later.
			// Wrap index will also tell us if any search results exist.
			int wrapIndex = (next ? text.IndexOf(search, MatchComparison) : text.LastIndexOf(search, MatchComparison));
			if (wrapIndex != -1) {
				searchFocus = true;
				int index = -1;
				if (next) {
					if (startIndex < text.Length)
						index = text.IndexOf(search, startIndex, MatchComparison);
					if (index == -1)
						index = wrapIndex;
				}
				else {
					if (startIndex > 0)
						index = text.LastIndexOf(search, startIndex - 1, MatchComparison);
					if (index == -1)
						index = wrapIndex;
				}
				locked = true;
				textEditor.Select(index, search.Length);
				textEditor.TextArea.Caret.BringCaretToView();
				locked = false;
			}
			else {
				textEditor.SelectionLength = 0;
			}
			UpdateStatusBarTextEditor();
		}

		#endregion

		#region Async Text Searching

		private async void IncrementSearchUpdate() {
			if (searching) {
				updateSearch++;
				if (updateSearch == 1) {
					await Task.Run(() => RunTextSearcher());
				}
			}
		}
		private void RunTextSearcher() {
			while (searching && updateSearch > 0) {
				int updateCount = updateSearch;
				string search = "";
				string text = "";
				Dispatcher.Invoke(() => {
					search = textBoxFind.Text;
					text = textEditor.Text;
				});
				int start = 0;
				int index;
				int count = 0;
				if (search.Length > 0) {
					while ((index = text.IndexOf(search, start, MatchComparison)) >= 0) {
						start = index + 1;
						count++;
					}
				}
				Dispatcher.Invoke(() => {
					if (search.Length == 0)
						labelMatches.Content = "";
					else if (count == 0)
						labelMatches.Content = "No matches found";
					else
						labelMatches.Content = "Found " + count + " match" + (count != 1 ? "es" : "");

					if (count > 0 || search.Length == 0) {
						textBoxFind.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
						textBoxFind.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
						textBoxFind.CaretBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
					}
					else {
						textBoxFind.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
						textBoxFind.Background = new SolidColorBrush(Color.FromRgb(255, 102, 102));
						textBoxFind.CaretBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
					}
				});
				updateSearch -= updateCount;
			}
			updateSearch = 0;
		}

		#endregion

		#region Editing

		private void CanExecuteToUppercase(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = textEditor.SelectionLength > 0;
		}
		private void CanExecuteToLowercase(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = textEditor.SelectionLength > 0;
		}

		private void ToUppercase(object sender, ExecutedRoutedEventArgs e) {
			int start = textEditor.SelectionStart;
			string str = textEditor.Text.Substring(start, textEditor.SelectionLength);
			//string text = textEditor.Text.Remove(start, textEditor.SelectionLength);
			//textEditor.Text = text.Insert(start, str.ToUpper());
			textEditor.Document.Replace(start, textEditor.SelectionLength, str.ToUpper());
			textEditor.Select(start, str.Length);
		}
		private void ToLowercase(object sender, ExecutedRoutedEventArgs e) {
			int start = textEditor.SelectionStart;
			string str = textEditor.Text.Substring(start, textEditor.SelectionLength);
			string text = textEditor.Text.Remove(start, textEditor.SelectionLength);
			//textEditor.Text = text.Insert(start, str.ToLower());
			textEditor.Document.Replace(start, textEditor.SelectionLength, str.ToLower());
			textEditor.Select(start, str.Length);
		}

		#endregion

		#region Goto

		private void OnCloseGotoBarPressed(object sender, RoutedEventArgs e) {
			menuItemGoto.IsChecked = false;
			textBoxGoto.IsEnabled = false;
			textBoxGoto.Text = "";
			
			gotoBarStoryboard.Children.Clear();
			DoubleAnimation anim = new DoubleAnimation(gotoBar.Height, 0, TimeSpan.FromSeconds(0.1));
			anim.BeginTime = TimeSpan.Zero;
			Storyboard.SetTarget(anim, gotoBar);
			Storyboard.SetTargetProperty(anim, new PropertyPath(Border.HeightProperty));
			gotoBarStoryboard.Children.Add(anim);
			gotoBarStoryboard.Begin(this, true);
		}

		private void OnGotoClicked(object sender, RoutedEventArgs e) {
			try {
				int line = int.Parse(textBoxGoto.Text);
				if (line > 0 && line <= textEditor.LineCount) {
					textEditor.CaretOffset = textEditor.Document.GetLineByNumber(line).Offset;
					searchFocus = false;
					textEditor.TextArea.Caret.BringCaretToView();
					UpdateStatusBarTextEditor();

					if (closeGoto)
						OnCloseGotoBarPressed(null, null);
				}
			}
			catch {

			}
		}

		private void OnGotoPreviewKeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Enter) {
				OnGotoClicked(null, null);
			}
		}
		private void OnGotoBarStoryboardCompleted(object sender, EventArgs e) {
			if (!menuItemGoto.IsChecked) {
				Keyboard.ClearFocus();
				textEditor.Focus();
			}
		}
		private void OnGoto(object sender, ExecutedRoutedEventArgs e) {
			if (!menuItemGoto.IsChecked) {
				menuItemGoto.IsChecked = true;
				textBoxGoto.IsEnabled = true;
				
				gotoBarStoryboard.Children.Clear();
				DoubleAnimation anim = new DoubleAnimation(gotoBar.Height, 31, TimeSpan.FromSeconds(0.1));
				anim.BeginTime = TimeSpan.Zero;
				Storyboard.SetTarget(anim, gotoBar);
				Storyboard.SetTargetProperty(anim, new PropertyPath(Border.HeightProperty));
				gotoBarStoryboard.Children.Add(anim);
				gotoBarStoryboard.Begin(this, true);

			}
			textBoxGoto.SelectAll();
			textBoxGoto.Focus();
		}

		private void OnGotoTextChanged(object sender, TextChangedEventArgs e) {
			bool valid = false;
			string str = textBoxGoto.Text;
			try {
				int line = int.Parse(textBoxGoto.Text);
				valid = (line > 0 && line <= textEditor.LineCount);
			}
			catch { }
			if (valid || str.Length == 0) {
				textBoxGoto.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
				textBoxGoto.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));
				textBoxGoto.CaretBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
			}
			else {
				textBoxGoto.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
				textBoxGoto.Background = new SolidColorBrush(Color.FromRgb(255, 102, 102));
				textBoxGoto.CaretBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));
			}
		}

		#endregion

		#region Font Size Delta
		
		private double GetFontZoom(int delta) {
			return Math.Pow(1.15, delta);
		}
		private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) {
			if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
				e.Handled = true;
				ChangeFontSizeDelta(Math.Sign(e.Delta));
			}
		}
		private void SetFontSizeDelta(int value) {
			fontSizeDelta = value;
			UpdateTextEditorFontSizeDelta();
			UpdateStatusBarZoom();
		}
		private void ChangeFontSizeDelta(int difference) {
			double zoom = GetFontZoom(fontSizeDelta + difference);
			if (fontSize * zoom >= 1 && zoom <= 10) {
				fontSizeDelta += difference;
				UpdateTextEditorFontSizeDelta();
				UpdateStatusBarZoom();
			}
		}
		private void UpdateTextEditorFontSizeDelta() {
			if (fontSizeDelta != 0) {
				textEditor.FontSize = fontSize * GetFontZoom(fontSizeDelta);
			}
			else {
				textEditor.FontSize = fontSize;
			}
		}

		private void ExecuteZoomIn(object sender, ExecutedRoutedEventArgs e) {
			ChangeFontSizeDelta(1);
		}
		private void ExecuteZoomOut(object sender, ExecutedRoutedEventArgs e) {
			ChangeFontSizeDelta(-1);
		}
		private void ExecuteResetZoom(object sender, ExecutedRoutedEventArgs e) {
			SetFontSizeDelta(0);
			UpdateTextEditorFontSizeDelta();
		}

		private void CanExecuteZoomIn(object sender, CanExecuteRoutedEventArgs e) {
			double zoom = GetFontZoom(fontSizeDelta + 1);
			e.CanExecute = (zoom <= 10);
		}
		private void CanExecuteZoomOut(object sender, CanExecuteRoutedEventArgs e) {
			double zoom = GetFontZoom(fontSizeDelta - 1);
			e.CanExecute = (fontSize * zoom >= 1);
		}
		private void CanExecuteResetZoom(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = (fontSizeDelta != 0);
		}

		#endregion

		#region Help

		private void OnCreditsClicked(object sender, RoutedEventArgs e) {
			CreditsWindow credits = new CreditsWindow();
			credits.Owner = this;
			credits.ShowDialog();
		}

		private void OnGithubClicked(object sender, RoutedEventArgs e) {
			System.Diagnostics.Process.Start("https://github.com/trigger-death/TriggersNotepad");
		}

		private void OnOpenReadmeClicked(object sender, RoutedEventArgs e) {
			string path = IOPath.Combine(
					IOPath.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
					"Readme.md"
				);

			if (File.Exists(path)) {
				System.Diagnostics.Process.Start(Assembly.GetExecutingAssembly().Location, "/md \"" + path + "\"");
			}
			else {
				TriggerMessageBox.Show(this, MessageIcon.Error, "Readme.txt is missing from the application directory!", "Missing File");
			}
		}

		private void OnOpenChangelogClicked(object sender, RoutedEventArgs e) {
			string path = IOPath.Combine(
					IOPath.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
					"Changelog.md"
				);

			if (File.Exists(path)) {
				System.Diagnostics.Process.Start(Assembly.GetExecutingAssembly().Location, "/md \"" + path + "\"");
			}
			else {
				TriggerMessageBox.Show(this, MessageIcon.Error, "Changelog.txt is missing from the application directory!", "Missing File");
			}
		}

		#endregion

		#region Markdown

		private void OnPreviewMarkdown(object sender, ExecutedRoutedEventArgs e) {
			viewingMarkdown = !viewingMarkdown;
			if (!viewingMarkdown) {
				LeaveMarkdown();
			}
			else {
				PreviewMarkdown();
			}
		}

		private void LeaveMarkdown() {
			viewingMarkdown = false;
			menuItemMarkdown.IsChecked = false;
			textEditor.Visibility = Visibility.Visible;
			markdownViewer.Visibility = Visibility.Hidden;
			markdownViewer.Zoom = 100;
			markdownViewer.Document = null;
			UpdateStatusBarZoom();
		}

		private void PreviewMarkdown() {
			viewingMarkdown = true;
			menuItemMarkdown.IsChecked = true;
			textEditor.Visibility = Visibility.Hidden;
			markdownViewer.Visibility = Visibility.Visible;
			var converter = new Markdown.Xaml.TextToFlowDocumentConverter();
			converter.Markdown = (Markdown.Xaml.Markdown)this.Resources["Markdown"];
			converter.Markdown.AssetPathRoot = lastDirectory;
			markdownViewer.Document = (FlowDocument)converter.Convert(textEditor.Text, null, null, null);
			textEditor.SelectionLength = 0;
			if (menuItemFind.IsChecked || menuItemReplace.IsChecked)
				OnCloseFindBarPressed(null, null);
			if (menuItemGoto.IsChecked)
				OnCloseGotoBarPressed(null, null);
			UpdateStatusBarZoom();
			PrintVisualTree(markdownViewer);
		}

		private void CanExecutePreviewMarkdown(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = true;
		}

		private void OnPreviewMarkdownPreviewScroll(object sender, MouseWheelEventArgs e) {
			// A hackish workaround to keep track of the flowdocument zoom. MouseWheel events are a pain.
			if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) {
				if (e.Delta > 0) {
					if (markdownViewer.Zoom < 200) {
						markdownViewer.MaxZoom += 10;
						markdownViewer.MinZoom += 10;
						UpdateStatusBarZoom();
					}
				}
				else {
					if (markdownViewer.Zoom > 70) {
						markdownViewer.MaxZoom -= 10;
						markdownViewer.MinZoom -= 10;
						UpdateStatusBarZoom();
					}
				}
				e.Handled = true;
			}
		}

		private void OnAutoPreviewMarkdownClicked(object sender, RoutedEventArgs e) {
			autoPreviewMarkdown = (sender as MenuItem).IsChecked;
		}

		public void PrintVisualTree(Visual visual, int level = 0) {
			Console.WriteLine(new string(' ', level) + visual.GetType().ToString());

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++) {
				PrintVisualTree((Visual)VisualTreeHelper.GetChild(visual, i), level + 1);
			}
		}

		#endregion

		private void CanExecuteTabsToSpaces(object sender, CanExecuteRoutedEventArgs e) {
			Typeface typeface = new Typeface(textEditor.FontFamily, textEditor.FontStyle, textEditor.FontWeight, textEditor.FontStretch);
			FormattedText ftHi = new FormattedText("Hi", CultureInfo.InvariantCulture, System.Windows.FlowDirection.LeftToRight, typeface, 10, Brushes.Black);
			FormattedText ftHH = new FormattedText("HH", CultureInfo.InvariantCulture, System.Windows.FlowDirection.LeftToRight, typeface, 10, Brushes.Black);
			e.CanExecute = (ftHi.Width == ftHH.Width);
		}

		private void ExecuteTabsToSpaces(object sender, ExecutedRoutedEventArgs e) {
			textEditor.Document.Replace(0, textEditor.Text.Length, ProcessTabsToSpaces(textEditor.Text, tabSize));
		}
		private void ExecuteSpacesToTabs(object sender, ExecutedRoutedEventArgs e) {
			textEditor.Document.Replace(0, textEditor.Text.Length, ProcessSpacesToTabs(textEditor.Text, tabSize));
		}

		static int GetNearestTabStop(int currentPosition, int tabLength) {
			// if already at the tab stop, jump to the next tab stop.
			if ((currentPosition % tabLength) == 1)
				currentPosition += tabLength;
			else {
				// if in the middle of two tab stops, move forward to the nearest.
				for (int i = 0; i < tabLength; i++, currentPosition++)
					if ((currentPosition % tabLength) == 1)
						break;
			}

			return currentPosition;
		}

		public static string ProcessTabsToSpaces(string input, int tabLength) {
			if (string.IsNullOrEmpty(input))
				return input;

			StringBuilder output = new StringBuilder();

			int positionInOutput = 1;
			foreach (var c in input) {
				switch (c) {
					case '\t':
						int spacesToAdd = GetNearestTabStop(positionInOutput, tabLength) - positionInOutput;
						output.Append(new string(' ', spacesToAdd));
						positionInOutput += spacesToAdd;
						break;

					case '\n':
						output.Append(c);
						positionInOutput = 1;
						break;

					default:
						output.Append(c);
						positionInOutput++;
						break;
				}
			}
			return output.ToString();
		}

		public static string ProcessSpacesToTabs(string input, int tabLength) {
			if (string.IsNullOrEmpty(input))
				return input;

			StringBuilder output = new StringBuilder();

			int positionInOutput = 1;
			bool newLine = true;
			int spaceCount = 0;
			foreach (var c in input) {
				switch (c) {
					case ' ':
						if (newLine)
							spaceCount++;
						else
							output.Append(c);
						break;

					case '\n':
						if (spaceCount > 0) {
							if (spaceCount >= tabLength)
								output.Append(new string('\t', spaceCount / 4));
							if (spaceCount % 4 > 0)
								output.Append(new string(' ', spaceCount % 4));
							spaceCount = 0;
						}
						output.Append(c);
						positionInOutput = 1;
						newLine = true;
						break;

					default:
						if (spaceCount > 0) {
							if (spaceCount >= tabLength)
								output.Append(new string('\t', spaceCount / 4));
							if (spaceCount % 4 > 0)
								output.Append(new string(' ', spaceCount % 4));
							spaceCount = 0;
						}
						output.Append(c);
						positionInOutput++;
						break;
				}
			}
			return output.ToString();
		}
	}
}
