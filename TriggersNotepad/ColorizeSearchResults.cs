using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Windows.Media;

namespace TriggersNotepad {
	public class ColorizeSearchResults : DocumentColorizingTransformer {

		public ColorizeSearchResults() : base() {
			SearchTerm = "";
			MatchCase = false;
			Foreground = Brushes.Magenta;
		}

		public string SearchTerm { get; set; }
		public bool MatchCase { get; set; }
		public Brush Foreground { get; set; }

		protected override void ColorizeLine(DocumentLine line) {
			if (SearchTerm.Length == 0)
				return;

			int lineStartOffset = line.Offset;
			string text = CurrentContext.Document.GetText(line);
			int count = 0;
			int start = 0;
			int index;
			while ((index = text.IndexOf(SearchTerm, start, MatchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase)) >= 0) {
				base.ChangeLinePart(
					lineStartOffset + index,
					lineStartOffset + index + SearchTerm.Length,
					(VisualLineElement element) => {
						element.TextRunProperties.SetForegroundBrush(Foreground);
						//element.TextRunProperties.SetForegroundBrush(Brushes.White);
						//element.TextRunProperties.SetBackgroundBrush(Brushes.Magenta);
					});
				start = index + 1;
				count++;
			}
		}
	}
}
