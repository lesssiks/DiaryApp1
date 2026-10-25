using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DiaryApp.Data;
using DiaryApp.Models;
using DiaryApp.Utils;

namespace DiaryApp
{
    public partial class MainWindow : Window
    {
        private readonly DataRepository _repository = new();
        private List<Note> _allNotes = new();
        private List<CalendarTask> _allTasks = new();
        private Note? _currentEditingNote;

        public MainWindow()
        {
            InitializeComponent();
            LoadData();
            RefreshNotesGrid();
            MainCalendar.SelectedDate = DateTime.Today;
        }

        private void LoadData()
        {
            _allNotes = _repository.LoadNotes();
            _allTasks = _repository.LoadTasks();
        }

        private void SaveData()
        {
            _repository.SaveNotes(_allNotes);
            _repository.SaveTasks(_allTasks);
        }

        private void RefreshNotesGrid()
        {
            NotesItemsControl.ItemsSource = null;
            NotesItemsControl.ItemsSource = _allNotes.Where(n => !n.IsInTrash).ToList();
        }

        private void BtnCreateNote_Click(object sender, RoutedEventArgs e)
        {
            var newNote = new Note
            {
                Title = "Новая заметка",
                Content = "# Заголовок\nТекст заметки...",
                LastModified = DateTime.Now
            };
            _allNotes.Add(newNote);
            SaveData();
            RefreshNotesGrid();
            OpenNoteForEditing(newNote);
        }

        private void OpenNoteForEditing(Note note)
        {
            _currentEditingNote = note;
            TxtNoteTitle.Text = note.Title;
            TxtNoteTags.Text = string.Join(", ", note.Tags);
            TxtNoteContent.Text = note.Content;
            NoteEditPanel.Visibility = Visibility.Visible;
        }

        private void BtnEditNote_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid id)
            {
                var note = _allNotes.FirstOrDefault(n => n.Id == id);
                if (note != null) OpenNoteForEditing(note);
            }
        }

        private void BtnDeleteNote_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid id)
            {
                var note = _allNotes.FirstOrDefault(n => n.Id == id);
                if (note != null)
                {
                    note.IsInTrash = true;
                    if (_currentEditingNote?.Id == id) NoteEditPanel.Visibility = Visibility.Collapsed;
                    SaveData();
                    RefreshNotesGrid();
                }
            }
        }

        private void BtnClearTrash_Click(object sender, RoutedEventArgs e)
        {
            _allNotes.RemoveAll(n => n.IsInTrash);
            SaveData();
            RefreshNotesGrid();
            MessageBox.Show("Корзина очищена безвозвратно.", "Успех");
        }

        private void BtnSaveNote_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEditingNote != null)
            {
                _currentEditingNote.Title = TxtNoteTitle.Text;
                _currentEditingNote.Content = TxtNoteContent.Text;
                _currentEditingNote.Tags = TxtNoteTags.Text.Split(',').Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                _currentEditingNote.LastModified = DateTime.Now;
                SaveData();
                RefreshNotesGrid();
                NoteEditPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void TxtNoteContent_TextChanged(object sender, TextChangedEventArgs e)
        {
            RtbMarkdownPreview.Document = MarkdownParser.Parse(TxtNoteContent.Text);
        }
        private void BtnCut_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtNoteContent.SelectedText))
            {
                try
                {
                    Clipboard.SetText(TxtNoteContent.SelectedText);
                    int selectionStart = TxtNoteContent.SelectionStart;     
                    TxtNoteContent.Text = TxtNoteContent.Text.Remove(selectionStart, TxtNoteContent.SelectionLength);
                    TxtNoteContent.Focus();
                    TxtNoteContent.SelectionStart = selectionStart;
                }
                catch (Exception)
                {           
                    int selectionStart = TxtNoteContent.SelectionStart;
                    TxtNoteContent.Text = TxtNoteContent.Text.Remove(selectionStart, TxtNoteContent.SelectionLength);
                    TxtNoteContent.Focus();
                    TxtNoteContent.SelectionStart = selectionStart;
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, сначала выделите мышкой текст в поле заметки, который хотите вырезать.", "Внимание");
            }
        }
        private void MainCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshTasksList();
        }

        private void RefreshTasksList()
        {
            if (MainCalendar.SelectedDate is DateTime selectedDate)
            {
                TxtSelectedDateHeader.Text = $"Задачи на: {selectedDate:dd.MM.yyyy}";

                var filtered = _allTasks.Where(t =>
                    t.Deadline.Date == selectedDate.Date ||
                    (t.Recurrence == "Ежедневно" && t.Deadline.Date <= selectedDate.Date) ||
                    (t.Recurrence == "Еженедельно" && t.Deadline.Date <= selectedDate.Date && (selectedDate - t.Deadline).Days % 7 == 0)
                ).ToList();

                LbxTasks.ItemsSource = null;
                LbxTasks.ItemsSource = filtered;
            }
        }

        private void BtnAddTask_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNewTaskTitle.Text) || !MainCalendar.SelectedDate.HasValue) return;

            var newTask = new CalendarTask
            {
                Title = TxtNewTaskTitle.Text,
                Deadline = MainCalendar.SelectedDate.Value.AddHours(12),
                Recurrence = (CbxRecurrence.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Нет"
            };

            _allTasks.Add(newTask);
            TxtNewTaskTitle.Clear();
            SaveData();
            RefreshTasksList();
        }

        private void TaskCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Tag is Guid id)
            {
                var task = _allTasks.FirstOrDefault(t => t.Id == id);
                if (task != null)
                {
                    task.IsCompleted = cb.IsChecked ?? false;
                    SaveData();
                    RefreshTasksList();
                }
            }
        }

        private void BtnDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid id)
            {
                _allTasks.RemoveAll(t => t.Id == id);
                SaveData();
                RefreshTasksList();
            }
        }

        private void BtnExportCSV_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv",
                FileName = "Экспорт_Заметок"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                DataExporter.ExportNotesToCsv(_allNotes.Where(n => !n.IsInTrash).ToList(), saveFileDialog.FileName);
                MessageBox.Show("Экспорт в UTF-8 успешно выполнен!", "Успех");
            }
        }

    }
}
