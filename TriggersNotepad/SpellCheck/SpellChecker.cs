using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using NHunspell;

namespace Martin
{
    public class SpellChecker
    {
        static Lazy<SpellChecker> defaultInstance = new Lazy<SpellChecker>(() => new SpellChecker());
        public static SpellChecker Default { get { return defaultInstance.Value; } }

        public Hunspell HunspellInstance { get; set; }

        public class Word
        {
            public int Index { get; set; }
            public string Value { get; set; }
        }

        static IEnumerable<Word> FindWords(string text)
        {
            foreach (Match m in new Regex(@"\w+").Matches(text))
            {
                yield return new Word() { Index = m.Index, Value = m.Value };
            }
        }

        public IEnumerable<Word> FindSpellingErrors(string text)
        {
            foreach (var word in FindWords(text))
            {
                if (!Spell(word.Value))
                {
                    yield return word;
                }
            }
        }

        public bool Spell(string word)
        {
            return HunspellInstance.Spell(word);
        }

        public List<string> Suggest(string word)
        {
            return HunspellInstance.Suggest(word);
        }
    }

    public class SpellCheckerBehavior : Behavior<TextEditor>
    {
        TextEditor textEditor;
        List<Control> originalItems;

        protected override void OnAttached()
        {
            textEditor = AssociatedObject;
            if (textEditor != null)
            {
                textEditor.ContextMenuOpening += new ContextMenuEventHandler(TextEditorContextMenuOpening);
                textEditor.TextArea.MouseRightButtonDown += new System.Windows.Input.MouseButtonEventHandler(TextAreaMouseRightButtonDown);
                originalItems = textEditor.ContextMenu.Items.OfType<Control>().ToList();
            }
            base.OnAttached();
        }

        protected override void OnDetaching()
        {
            if (textEditor != null)
            {
                textEditor.ContextMenuOpening -= new ContextMenuEventHandler(TextEditorContextMenuOpening);
                textEditor.TextArea.MouseRightButtonDown -= new System.Windows.Input.MouseButtonEventHandler(TextAreaMouseRightButtonDown);
                originalItems = null;
                textEditor = null;
            }
            base.OnDetaching();
        }

        void TextAreaMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var position = textEditor.GetPositionFromPoint(e.GetPosition(textEditor));
            if (position.HasValue)
            {
                textEditor.TextArea.Caret.Position = position.Value;
            }
        }

        void TextEditorContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            foreach (Control item in textEditor.ContextMenu.Items.OfType<Control>().ToList())
            {
                if (originalItems.Contains(item)) { continue; }
                textEditor.ContextMenu.Items.Remove(item);
            }
            var position = textEditor.TextArea.Caret.Position;
            Match word = null;
            Regex r = new Regex(@"\w+");
            var line = textEditor.Document.GetText(textEditor.Document.GetLineByNumber(position.Line));
            foreach (Match m in r.Matches(line))
            {
                if (m.Index >= position.VisualColumn) { break; }
                word = m;
            }
            if (null == word ||
                position.Column > word.Index + word.Value.Length ||
                SpellChecker.Default.Spell(word.Value))
            {
                return;
            }
			var separator = new Separator();
			separator.Margin = new Thickness(0);
			textEditor.ContextMenu.Items.Insert(0, separator);
            var suggestions = SpellChecker.Default.Suggest(word.Value);
            if (0 == suggestions.Count)
            {
                textEditor.ContextMenu.Items.Insert(0,
                    new MenuItem() { Header = "<No suggestions found>", IsEnabled = false });
                return;
            }
            foreach (string suggestion in suggestions)
            {
                var item = new MenuItem { Header = suggestion, FontWeight = FontWeights.Bold };
                item.Tag =
                    new Tuple<int, int>(
                        textEditor.Document.GetOffset(position.Line, word.Index + 1),
                        word.Value.Length);
                item.Click += ItemClick;
                textEditor.ContextMenu.Items.Insert(0, item);
            }
        }

        private void ItemClick(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            if (null == item) { return; }
            var segment = item.Tag as Tuple<int, int>;
            if (null == segment) { return; }
            textEditor.Document.Replace(segment.Item1, segment.Item2, item.Header.ToString());
        }
    }

    public class SpellCheckerColorizer : DocumentColorizingTransformer
    {
        private readonly TextDecorationCollection textDecorationCollection;

        public SpellCheckerColorizer()
        {
            textDecorationCollection = new TextDecorationCollection();
            textDecorationCollection.Add(new TextDecoration()
            {
                Pen = new Pen { Thickness = 1, DashStyle = DashStyles.Dash, Brush = new SolidColorBrush(Colors.Red) },
                PenThicknessUnit = TextDecorationUnit.Pixel
            });
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            var lineText = CurrentContext.Document.Text
                .Substring(line.Offset, line.Length);
            foreach (var error in SpellChecker.Default.FindSpellingErrors(lineText))
            {
                base.ChangeLinePart(line.Offset + error.Index, line.Offset + error.Index + error.Value.Length,
                            (VisualLineElement element) => element.TextRunProperties.SetTextDecorations(textDecorationCollection));
            }
        }
    }
}