using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiaryApp.Models
{
    public class Note
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "Новая заметка";
        public string Content { get; set; } = string.Empty;
        public DateTime LastModified { get; set; } = DateTime.Now;
        public List<string> Tags { get; set; } = new List<string>();
        public bool IsInTrash { get; set; } = false;

        public string PreviewText => Content.Length <= 60 ? Content : Content.Substring(0, 60) + "...";
    }

    public class CalendarTask
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = "Новая задача";
        public DateTime Deadline { get; set; } = DateTime.Now;
        public bool IsCompleted { get; set; } = false;
        public string Recurrence { get; set; } = "Нет";

        public bool IsOverdue => !IsCompleted && Deadline < DateTime.Now;
    }
}
