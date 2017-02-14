using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Indentation;
using System;

namespace TriggersNotepad {
	public class NoIndentationStrategy : IIndentationStrategy {
		public void IndentLine(TextDocument document, DocumentLine line) {

		}

		public void IndentLines(TextDocument document, int beginLine, int endLine) {

		}
	}
}
