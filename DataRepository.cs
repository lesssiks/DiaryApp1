using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using DiaryApp.Models;

namespace DiaryApp.Data
{
    public class DataRepository
    {
        private readonly string _notesPath;
        private readonly string _tasksPath;

        public DataRepository()
        {
            string appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiaryWpfApp");
            Directory.CreateDirectory(appDir);
            _notesPath = Path.Combine(appDir, "notes.json");
            _tasksPath = Path.Combine(appDir, "tasks.json");
        }

        public List<Note> LoadNotes()
        {
            if (!File.Exists(_notesPath)) return new List<Note>();
            return JsonSerializer.Deserialize<List<Note>>(File.ReadAllText(_notesPath)) ?? new List<Note>();
        }

        public void SaveNotes(List<Note> notes) =>
            File.WriteAllText(_notesPath, JsonSerializer.Serialize(notes));

        public List<CalendarTask> LoadTasks()
        {
            if (!File.Exists(_tasksPath)) return new List<CalendarTask>();
            return JsonSerializer.Deserialize<List<CalendarTask>>(File.ReadAllText(_tasksPath)) ?? new List<CalendarTask>();
        }

        public void SaveTasks(List<CalendarTask> tasks) =>
            File.WriteAllText(_tasksPath, JsonSerializer.Serialize(tasks));
    }
}

