using System;
using System.Windows;
using System.Windows.Documents;

namespace DiaryApp.Utils
{
    public static class MarkdownParser
    {
        public static FlowDocument Parse(string text)
        {
            var doc = new FlowDocument();
            if (string.IsNullOrEmpty(text)) return doc;
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                var paragraph = new Paragraph();

                if (line.StartsWith("# "))
                {
                    paragraph.FontSize = 18;
                    paragraph.FontWeight = FontWeights.Bold;
                    paragraph.Inlines.Add(new Run(line.Substring(2)));
                    doc.Blocks.Add(paragraph);
                    continue;
                }

                if (line.StartsWith("* "))
                {
                    paragraph.Margin = new Thickness(20, 0, 0, 0);
                    paragraph.Inlines.Add(new Run("• " + line.Substring(2)));
                    doc.Blocks.Add(paragraph);
                    continue;
                }
                string currentStr = line;
                if (currentStr.Contains("**"))
                {
                    string[] parts = currentStr.Split(new[] { "**" }, StringSplitOptions.None);
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (i % 2 == 1)
                            paragraph.Inlines.Add(new Run(parts[i]) { FontWeight = FontWeights.Bold });
                        else
                            paragraph.Inlines.Add(new Run(parts[i]));
                    }
                }
                else
                {
                    paragraph.Inlines.Add(new Run(currentStr));
                }

                doc.Blocks.Add(paragraph);
            }
            return doc;
        }
    }
}
