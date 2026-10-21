using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Collections.Generic;
using DiaryApp.Models;


namespace DiaryApp1
{
    public static class DataExporter
    {
        public static void ExportNotesToCsv(List<Note> notes, string filePath)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Id,Title,LastModified,Content");

            foreach (var note in notes)
            {
                string safeContent = note.Content.Replace("\"", "\"\"");
                string row = $"\"{note.Id}\",\"{note.Title}\",\"{note.LastModified:yyyy-MM-ddTHH:mm:ssZ}\",\"{safeContent}\"";
                csv.AppendLine(row);
            }

            File.WriteAllText(filePath, csv.ToString(), Encoding.UTF8);
        }

    }
}
