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

        // Ссылка на заметку, которая сейчас открыта на панели редактирования
        private Note? _currentEditingNote;

        public MainWindow()
        {
            InitializeComponent();
            LoadData();
            RefreshNotesGrid();

            // Устанавливаем текущую дату в календаре по умолчанию
            MainCalendar.SelectedDate = DateTime.Today;
        }

        // Загрузка списков заметок и задач из локального репозитория
        private void LoadData()
        {
            _allNotes = _repository.LoadNotes();
            _allTasks = _repository.LoadTasks();
        }

        // Сохранение текущих списков в локальный репозиторий
        private void SaveData()
        {
            _repository.SaveNotes(_allNotes);
            _repository.SaveTasks(_allTasks);
        }

        // Обновление интерфейса: выводим только те заметки, которые не удалены в корзину
        private void RefreshNotesGrid()
        {
            NotesItemsControl.ItemsSource = null;
            NotesItemsControl.ItemsSource = _allNotes.Where(n => !n.IsInTrash).ToList();
        }

        // Создание новой заметки со стандартным шаблоном и открытие её для редактирования
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

        // Заполнение полей формы данными выбранной заметки и вывод панели на экран
        private void OpenNoteForEditing(Note note)
        {
            _currentEditingNote = note;
            TxtNoteTitle.Text = note.Title;

            // Объединяем список тегов в одну строку через запятую для отображения
            TxtNoteTags.Text = string.Join(", ", note.Tags);
            TxtNoteContent.Text = note.Content;
            NoteEditPanel.Visibility = Visibility.Visible;
        }

        // Обработчик кнопки редактирования: находим заметку по Guid из свойства Tag кнопки
        private void BtnEditNote_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid id)
            {
                var note = _allNotes.FirstOrDefault(n => n.Id == id);
                if (note != null) OpenNoteForEditing(note);
            }
        }

        // Мягкое удаление заметки — отправка в корзину (IsInTrash = true)
        private void BtnDeleteNote_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid id)
            {
                var note = _allNotes.FirstOrDefault(n => n.Id == id);
                if (note != null)
                {
                    note.IsInTrash = true;

                    // Если удаляемая заметка была открыта в редакторе — скрываем панель
                    if (_currentEditingNote?.Id == id) NoteEditPanel.Visibility = Visibility.Collapsed;

                    SaveData();
                    RefreshNotesGrid();
                }
            }
        }

        // Жесткое удаление: навсегда стираем из памяти все заметки, находящиеся в корзине
        private void BtnClearTrash_Click(object sender, RoutedEventArgs e)
        {
            _allNotes.RemoveAll(n => n.IsInTrash);
            SaveData();
            RefreshNotesGrid();
            MessageBox.Show("Корзина очищена безвозвратно.", "Успех");
        }

        // Сохранение отредактированного текста и тегов из полей UI обратно в объект заметки
        private void BtnSaveNote_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEditingNote != null)
            {
                _currentEditingNote.Title = TxtNoteTitle.Text;
                _currentEditingNote.Content = TxtNoteContent.Text;

                // Преобразуем строку тегов обратно в список: делим по запятой, зачищаем пробелы, убираем пустые
                _currentEditingNote.Tags = TxtNoteTags.Text
                    .Split(',')
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToList();

                _currentEditingNote.LastModified = DateTime.Now;
                SaveData();
                RefreshNotesGrid();
                NoteEditPanel.Visibility = Visibility.Collapsed;
            }
        }

        // Живой рендеринг Markdown-разметки в RichTextBox при любом изменении текста заметки
        private void TxtNoteContent_TextChanged(object sender, TextChangedEventArgs e)
        {
            RtbMarkdownPreview.Document = MarkdownParser.Parse(TxtNoteContent.Text);
        }

        // Вырезание выделенного текста в буфер обмена системы
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
                    // На случай, если доступ к Clipboard заблокирован ОС или другой программой, просто удаляем текст
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

        // При смене даты в календаре обновляем список задач под этот день
        private void MainCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshTasksList();
        }

        // Фильтрация и отображение задач на выбранную дату с учетом регулярных повторений
        private void RefreshTasksList()
        {
            if (MainCalendar.SelectedDate is DateTime selectedDate)
            {
                TxtSelectedDateHeader.Text = $"Задачи на: {selectedDate:dd.MM.yyyy}";

                // Комплексная проверка: обычная дата, ежедневный повтор или еженедельный (разница дней кратна 7)
                var filtered = _allTasks.Where(t =>
                    t.Deadline.Date == selectedDate.Date ||
                    (t.Recurrence == "Ежедневно" && t.Deadline.Date <= selectedDate.Date) ||
                    (t.Recurrence == "Еженедельно" && t.Deadline.Date <= selectedDate.Date && (selectedDate - t.Deadline).Days % 7 == 0)
                ).ToList();

                LbxTasks.ItemsSource = null;
                LbxTasks.ItemsSource = filtered;
            }
        }

        // Добавление новой задачи на выбранный день календаря
        private void BtnAddTask_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNewTaskTitle.Text) || !MainCalendar.SelectedDate.HasValue) return;

            var newTask = new CalendarTask
            {
                Title = TxtNewTaskTitle.Text,
                Deadline = MainCalendar.SelectedDate.Value.AddHours(12), // Ставим дефолтное время выполнения на 12:00
                Recurrence = (CbxRecurrence.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Нет"
            };
            _allTasks.Add(newTask);
            TxtNewTaskTitle.Clear();
            SaveData();
            RefreshTasksList();
        }

        // Переключение флага выполнения задачи (IsCompleted) при клике на чекбокс
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

        // Удаление задачи из общего списка по ее уникальному Id
        private void BtnDeleteTask_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid id)
            {
                _allTasks.RemoveAll(t => t.Id == id);
                SaveData();
                RefreshTasksList();
            }
        }

        // Экспорт всех активных заметок (не из корзины) в CSV-файл
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
            }
        }
    }
}
