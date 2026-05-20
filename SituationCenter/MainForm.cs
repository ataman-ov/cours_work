using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SituationCenter;

public sealed class MainForm : Form
{
    private readonly FileDatabase _fileDatabase;
    private readonly VirtualSensor _sensor = new();
    private readonly System.Windows.Forms.Timer _timer = new();
    private SituationCenterDatabase _database;

    private readonly Label _currentValueLabel = new();
    private readonly Label _statusLabel = new();
    private readonly Label _lastUpdateLabel = new();
    private readonly DataGridView _readingsGrid = new();
    private readonly DataGridView _tasksGrid = new();
    private readonly DataGridView _employeesGrid = new();
    private readonly Button _pollButton = new();
    private readonly Button _toggleAutoButton = new();
    private readonly Button _completeTaskButton = new();

    public MainForm()
    {
        Text = "Ситуационный центр";
        MinimumSize = new Size(1120, 720);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);

        var databasePath = Path.Combine(AppContext.BaseDirectory, "data", "situation-center-db.json");
        _fileDatabase = new FileDatabase(databasePath);
        _database = _fileDatabase.Load();

        BuildInterface();
        ConfigureTimer();
        RefreshAll();
    }

    private void BuildInterface()
    {
        BackColor = Color.FromArgb(245, 247, 250);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(18),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        var header = BuildHeader();
        var tabs = BuildTabs();
        var footer = BuildFooter();

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(tabs, 0, 1);
        root.Controls.Add(footer, 0, 2);

        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            BackColor = Color.White,
            Padding = new Padding(16),
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12));

        var titlePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        titlePanel.Controls.Add(new Label
        {
            Text = "Ситуационный центр",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(24, 34, 49)
        });
        titlePanel.Controls.Add(new Label
        {
            Text = _sensor.Name,
            AutoSize = true,
            ForeColor = Color.FromArgb(88, 101, 118)
        });

        ConfigureMetricLabel(_currentValueLabel, "нет данных");
        ConfigureMetricLabel(_statusLabel, "ожидание");
        ConfigureMetricLabel(_lastUpdateLabel, "-");

        _pollButton.Text = "Получить показание";
        _pollButton.Dock = DockStyle.Top;
        _pollButton.Height = 36;
        _pollButton.Click += (_, _) => PollSensor();

        _toggleAutoButton.Text = "Авто: выкл";
        _toggleAutoButton.Dock = DockStyle.Top;
        _toggleAutoButton.Height = 36;
        _toggleAutoButton.Click += (_, _) => ToggleAutoPolling();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
        buttons.Controls.Add(_pollButton);
        buttons.Controls.Add(_toggleAutoButton);

        header.Controls.Add(titlePanel, 0, 0);
        header.Controls.Add(BuildMetricPanel("Текущее значение", _currentValueLabel), 1, 0);
        header.Controls.Add(BuildMetricPanel("Статус", _statusLabel), 2, 0);
        header.Controls.Add(BuildMetricPanel("Последнее обновление", _lastUpdateLabel), 3, 0);
        header.Controls.Add(buttons, 4, 0);

        return header;
    }

    private static Panel BuildMetricPanel(string caption, Label valueLabel)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6, 0, 6, 0) };
        var captionLabel = new Label
        {
            Text = caption,
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.FromArgb(91, 105, 125)
        };
        valueLabel.Dock = DockStyle.Fill;
        panel.Controls.Add(valueLabel);
        panel.Controls.Add(captionLabel);
        return panel;
    }

    private static void ConfigureMetricLabel(Label label, string text)
    {
        label.Text = text;
        label.Font = new Font("Segoe UI", 15, FontStyle.Bold);
        label.ForeColor = Color.FromArgb(25, 42, 68);
        label.TextAlign = ContentAlignment.MiddleLeft;
    }

    private Control BuildTabs()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(16, 8)
        };

        tabs.TabPages.Add(BuildReadingsPage());
        tabs.TabPages.Add(BuildTasksPage());
        tabs.TabPages.Add(BuildEmployeesPage());

        return tabs;
    }

    private TabPage BuildReadingsPage()
    {
        var page = new TabPage("Показания датчика");
        ConfigureGrid(_readingsGrid);
        page.Controls.Add(_readingsGrid);
        return page;
    }

    private TabPage BuildTasksPage()
    {
        var page = new TabPage("Задачи сотрудникам");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        ConfigureGrid(_tasksGrid);
        _completeTaskButton.Text = "Отметить выбранную задачу выполненной";
        _completeTaskButton.Width = 320;
        _completeTaskButton.Height = 34;
        _completeTaskButton.Click += (_, _) => CompleteSelectedTask();

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 7, 0, 0)
        };
        bottom.Controls.Add(_completeTaskButton);

        layout.Controls.Add(_tasksGrid, 0, 0);
        layout.Controls.Add(bottom, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildEmployeesPage()
    {
        var page = new TabPage("Сотрудники");
        ConfigureGrid(_employeesGrid);
        page.Controls.Add(_employeesGrid);
        return page;
    }

    private Control BuildFooter()
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Text = "Файловая база данных: data/situation-center-db.json",
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(92, 102, 115)
        };
    }

    private static void ConfigureGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;
    }

    private void ConfigureTimer()
    {
        _timer.Interval = 5000;
        _timer.Tick += (_, _) => PollSensor();
    }

    private void ToggleAutoPolling()
    {
        _timer.Enabled = !_timer.Enabled;
        _toggleAutoButton.Text = _timer.Enabled ? "Авто: вкл" : "Авто: выкл";
    }

    private void PollSensor()
    {
        var nextReadingId = _database.Readings.Count == 0 ? 1 : _database.Readings.Max(r => r.Id) + 1;
        var reading = _sensor.GetReading(nextReadingId);
        _database.Readings.Add(reading);

        if (reading.Status != "ОК")
        {
            _database.Tasks.Add(CreateTaskForReading(reading));
        }

        _fileDatabase.Save(_database);
        RefreshAll();
    }

    private WorkTask CreateTaskForReading(SensorReading reading)
    {
        var employee = reading.Value > _sensor.MaxNormalValue
            ? FindEmployee("Эксплуатация датчиков")
            : FindEmployee("Ремонтная служба");

        var nextTaskId = _database.Tasks.Count == 0 ? 1 : _database.Tasks.Max(t => t.Id) + 1;
        return new WorkTask
        {
            Id = nextTaskId,
            CreatedAt = DateTime.Now,
            Title = reading.Value > _sensor.MaxNormalValue
                ? "Проверить перегрев датчика"
                : "Проверить пониженное значение датчика",
            Description = $"{reading.SensorName}: зафиксировано {reading.Value} {reading.Unit}. {reading.Comment}",
            Priority = reading.Value > 38 || reading.Value < 14 ? "Высокий" : "Средний",
            Status = "Новая",
            AssignedEmployeeId = employee.Id,
            AssignedEmployeeName = employee.FullName,
            Department = employee.Department,
            ContactPhone = employee.Phone
        };
    }

    private Employee FindEmployee(string department)
    {
        return _database.Employees.FirstOrDefault(e => e.Department == department)
            ?? _database.Employees.First();
    }

    private void CompleteSelectedTask()
    {
        if (_tasksGrid.CurrentRow?.DataBoundItem is not TaskRow row)
        {
            return;
        }

        var task = _database.Tasks.FirstOrDefault(t => t.Id == row.Id);
        if (task is null)
        {
            return;
        }

        task.Status = "Выполнена";
        _fileDatabase.Save(_database);
        RefreshAll();
    }

    private void RefreshAll()
    {
        var latest = _database.Readings.LastOrDefault();
        if (latest is null)
        {
            _currentValueLabel.Text = "нет данных";
            _statusLabel.Text = "ожидание";
            _statusLabel.ForeColor = Color.FromArgb(25, 42, 68);
            _lastUpdateLabel.Text = "-";
        }
        else
        {
            _currentValueLabel.Text = $"{latest.Value} {latest.Unit}";
            _statusLabel.Text = latest.Status;
            _statusLabel.ForeColor = latest.Status == "ОК"
                ? Color.FromArgb(27, 127, 83)
                : Color.FromArgb(190, 67, 67);
            _lastUpdateLabel.Text = latest.Timestamp.ToString("dd.MM.yyyy HH:mm:ss");
        }

        _readingsGrid.DataSource = new BindingList<ReadingRow>(_database.Readings
            .OrderByDescending(r => r.Timestamp)
            .Select(r => new ReadingRow(r))
            .ToList());
        SetHeader(_readingsGrid, "Id", "N");
        SetHeader(_readingsGrid, "Time", "Время");
        SetHeader(_readingsGrid, "Sensor", "Датчик");
        SetHeader(_readingsGrid, "Value", "Значение");
        SetHeader(_readingsGrid, "Status", "Статус");
        SetHeader(_readingsGrid, "Comment", "Комментарий");

        _tasksGrid.DataSource = new BindingList<TaskRow>(_database.Tasks
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TaskRow(t))
            .ToList());
        SetHeader(_tasksGrid, "Id", "N");
        SetHeader(_tasksGrid, "Created", "Создана");
        SetHeader(_tasksGrid, "Title", "Задача");
        SetHeader(_tasksGrid, "Priority", "Приоритет");
        SetHeader(_tasksGrid, "Status", "Статус");
        SetHeader(_tasksGrid, "Employee", "Сотрудник");
        SetHeader(_tasksGrid, "Department", "Подразделение");
        SetHeader(_tasksGrid, "Phone", "Телефон");

        _employeesGrid.DataSource = new BindingList<EmployeeRow>(_database.Employees
            .Select(e => new EmployeeRow(e))
            .ToList());
        SetHeader(_employeesGrid, "Id", "N");
        SetHeader(_employeesGrid, "FullName", "ФИО");
        SetHeader(_employeesGrid, "Position", "Должность");
        SetHeader(_employeesGrid, "Department", "Подразделение");
        SetHeader(_employeesGrid, "Phone", "Телефон");
    }

    private static void SetHeader(DataGridView grid, string propertyName, string header)
    {
        if (grid.Columns.Contains(propertyName))
        {
            grid.Columns[propertyName].HeaderText = header;
        }
    }

    private sealed class ReadingRow
    {
        public ReadingRow(SensorReading reading)
        {
            Id = reading.Id;
            Time = reading.Timestamp.ToString("dd.MM.yyyy HH:mm:ss");
            Sensor = reading.SensorName;
            Value = $"{reading.Value} {reading.Unit}";
            Status = reading.Status;
            Comment = reading.Comment;
        }

        public int Id { get; }
        public string Time { get; }
        public string Sensor { get; }
        public string Value { get; }
        public string Status { get; }
        public string Comment { get; }
    }

    private sealed class TaskRow
    {
        public TaskRow(WorkTask task)
        {
            Id = task.Id;
            Created = task.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss");
            Title = task.Title;
            Priority = task.Priority;
            Status = task.Status;
            Employee = task.AssignedEmployeeName;
            Department = task.Department;
            Phone = task.ContactPhone;
        }

        public int Id { get; }
        public string Created { get; }
        public string Title { get; }
        public string Priority { get; }
        public string Status { get; }
        public string Employee { get; }
        public string Department { get; }
        public string Phone { get; }
    }

    private sealed class EmployeeRow
    {
        public EmployeeRow(Employee employee)
        {
            Id = employee.Id;
            FullName = employee.FullName;
            Position = employee.Position;
            Department = employee.Department;
            Phone = employee.Phone;
        }

        public int Id { get; }
        public string FullName { get; }
        public string Position { get; }
        public string Department { get; }
        public string Phone { get; }
    }
}
