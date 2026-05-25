using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SituationCenter;

public sealed class MainForm : Form
{
    private const int SensorPollingSeconds = 10;

    private readonly IDataStore _fileDatabase;
    private readonly ISensor _sensor = new VirtualSensor();
    private readonly System.Windows.Forms.Timer _timer = new();
    private SituationCenterDatabase _database;
    private int _secondsUntilNextReading = SensorPollingSeconds;

    private readonly Label _currentValueLabel = new();
    private readonly Label _statusLabel = new();
    private readonly Label _lastUpdateLabel = new();
    private readonly Label _counterLabel = new();
    private readonly DataGridView _readingsGrid = new();
    private readonly DataGridView _tasksGrid = new();
    private readonly DataGridView _employeesGrid = new();
    private readonly Button _pollButton = new();
    private readonly Button _toggleAutoButton = new();
    private readonly Button _completeTaskButton = new();
    private readonly TextBox _taskTitleBox = new();
    private readonly TextBox _taskDescriptionBox = new();
    private readonly ComboBox _taskPriorityBox = new();
    private readonly ComboBox _taskEmployeeBox = new();
    private readonly ComboBox _taskSortBox = new();
    private readonly ComboBox _taskSortDirectionBox = new();
    private readonly Button _addTaskButton = new();
    private readonly TextBox _employeeNameBox = new();
    private readonly TextBox _employeePositionBox = new();
    private readonly TextBox _employeeDepartmentBox = new();
    private readonly TextBox _employeePhoneBox = new();
    private readonly Button _addEmployeeButton = new();

    public MainForm()
    {
        Text = "Ситуационный центр";
        MinimumSize = new Size(1040, 720);
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
            Padding = new Padding(18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildTabs(), 0, 1);
        root.Controls.Add(BuildFooter(), 0, 2);

        Controls.Add(root);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            BackColor = Color.White,
            Padding = new Padding(12)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14));

        var titlePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        titlePanel.Controls.Add(new Label
        {
            Text = "Ситуационный центр",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
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
        _counterLabel.Text = $"До опроса: {_secondsUntilNextReading} сек.";
        _counterLabel.AutoSize = true;
        _counterLabel.ForeColor = Color.FromArgb(88, 101, 118);

        _pollButton.Text = "Получить показание";
        _pollButton.Dock = DockStyle.Top;
        _pollButton.Height = 36;
        _pollButton.Click += (_, _) =>
        {
            PollSensor();
            ResetCountdown();
        };

        _toggleAutoButton.Text = "Авто: вкл";
        _toggleAutoButton.Dock = DockStyle.Top;
        _toggleAutoButton.Height = 36;
        _toggleAutoButton.Click += (_, _) => ToggleAutoPolling();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown };
        buttons.Controls.Add(_counterLabel);
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
        label.Font = new Font("Segoe UI", 13, FontStyle.Bold);
        label.ForeColor = Color.FromArgb(25, 42, 68);
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.AutoEllipsis = true;
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
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 2,
            Padding = new Padding(8, 14, 8, 16)
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        ConfigureTextInput(_taskTitleBox, "Название задачи");
        ConfigureTextInput(_taskDescriptionBox, "Описание");

        _taskPriorityBox.Dock = DockStyle.Bottom;
        _taskPriorityBox.Height = 30;
        _taskPriorityBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _taskPriorityBox.Items.AddRange(["Низкий", "Средний", "Высокий"]);
        _taskPriorityBox.SelectedItem = "Средний";

        _taskEmployeeBox.Dock = DockStyle.Bottom;
        _taskEmployeeBox.Height = 30;
        _taskEmployeeBox.DropDownStyle = ComboBoxStyle.DropDownList;

        _addTaskButton.Text = "Добавить";
        _addTaskButton.Dock = DockStyle.Fill;
        _addTaskButton.Margin = new Padding(8, 6, 8, 6);
        _addTaskButton.Height = 34;
        _addTaskButton.Click += (_, _) => AddTask();

        AddFormField(form, "Задача", _taskTitleBox, 0);
        AddFormField(form, "Описание", _taskDescriptionBox, 1);
        AddFormField(form, "Приоритет", _taskPriorityBox, 2);
        AddFormField(form, "Сотрудник", _taskEmployeeBox, 3);
        form.Controls.Add(_addTaskButton, 4, 0);
        form.SetRowSpan(_addTaskButton, 2);

        var sortPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            Padding = new Padding(8, 4, 8, 8)
        };
        sortPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        sortPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        sortPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        sortPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));

        _taskSortBox.Dock = DockStyle.Fill;
        _taskSortBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _taskSortBox.Items.AddRange(["Дата", "Приоритет", "Сотрудник"]);
        _taskSortBox.SelectedItem = "Дата";
        _taskSortBox.SelectedIndexChanged += (_, _) => RefreshAll();

        _taskSortDirectionBox.Dock = DockStyle.Fill;
        _taskSortDirectionBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _taskSortDirectionBox.Items.AddRange(["По убыванию", "По возрастанию"]);
        _taskSortDirectionBox.SelectedItem = "По убыванию";
        _taskSortDirectionBox.SelectedIndexChanged += (_, _) => RefreshAll();

        sortPanel.Controls.Add(new Label { Text = "Сортировать:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        sortPanel.Controls.Add(_taskSortBox, 1, 0);
        sortPanel.Controls.Add(new Label { Text = "Порядок:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, 0);
        sortPanel.Controls.Add(_taskSortDirectionBox, 3, 0);

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

        layout.Controls.Add(form, 0, 0);
        layout.Controls.Add(sortPanel, 0, 1);
        layout.Controls.Add(_tasksGrid, 0, 2);
        layout.Controls.Add(bottom, 0, 3);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildEmployeesPage()
    {
        var page = new TabPage("Сотрудники");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 2,
            Padding = new Padding(8, 14, 8, 16)
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 23));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        ConfigureTextInput(_employeeNameBox, "ФИО");
        ConfigureTextInput(_employeePositionBox, "Должность");
        ConfigureTextInput(_employeeDepartmentBox, "Подразделение");
        ConfigureTextInput(_employeePhoneBox, "Телефон");

        _addEmployeeButton.Text = "Добавить";
        _addEmployeeButton.Dock = DockStyle.Fill;
        _addEmployeeButton.Margin = new Padding(8, 6, 8, 6);
        _addEmployeeButton.Height = 34;
        _addEmployeeButton.Click += (_, _) => AddEmployee();

        AddFormField(form, "ФИО", _employeeNameBox, 0);
        AddFormField(form, "Должность", _employeePositionBox, 1);
        AddFormField(form, "Подразделение", _employeeDepartmentBox, 2);
        AddFormField(form, "Телефон", _employeePhoneBox, 3);
        form.Controls.Add(_addEmployeeButton, 4, 0);
        form.SetRowSpan(_addEmployeeButton, 2);

        ConfigureGrid(_employeesGrid);
        layout.Controls.Add(form, 0, 0);
        layout.Controls.Add(_employeesGrid, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    private static void ConfigureTextInput(TextBox input, string placeholder)
    {
        input.Dock = DockStyle.Fill;
        input.Height = 30;
        input.BorderStyle = BorderStyle.FixedSingle;
        input.BackColor = Color.White;
        input.PlaceholderText = placeholder;
    }

    private static void AddFormField(TableLayoutPanel form, string labelText, Control input, int column)
    {
        var label = new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(91, 105, 125),
            Margin = new Padding(0, 0, 8, 0)
        };

        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(0, 4, 8, 6);

        form.Controls.Add(label, column, 0);
        form.Controls.Add(input, column, 1);
    }

    private static Control BuildInputPanel(string labelText, TextBox input)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
        panel.Controls.Add(input);
        panel.Controls.Add(new Label
        {
            Text = labelText,
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.FromArgb(91, 105, 125)
        });
        return panel;
    }

    private static Control BuildComboPanel(string labelText, ComboBox input)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 8, 0) };
        panel.Controls.Add(input);
        panel.Controls.Add(new Label
        {
            Text = labelText,
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = Color.FromArgb(91, 105, 125)
        });
        return panel;
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
        grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.RowHeadersVisible = false;
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.TopLeft;
        grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.CellDoubleClick += ShowFullCellText;
        grid.CellToolTipTextNeeded += ShowCellToolTip;
    }

    private static void ShowFullCellText(object? sender, DataGridViewCellEventArgs e)
    {
        if (sender is not DataGridView grid || e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        var text = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var title = grid.Columns[e.ColumnIndex].HeaderText;
        MessageBox.Show(text, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void ShowCellToolTip(object? sender, DataGridViewCellToolTipTextNeededEventArgs e)
    {
        if (sender is not DataGridView grid || e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        e.ToolTipText = grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
    }

    private void ConfigureTimer()
    {
        _timer.Interval = 1000;
        _timer.Tick += (_, _) => CountdownSensorPolling();
        _timer.Start();
    }

    private void ToggleAutoPolling()
    {
        _timer.Enabled = !_timer.Enabled;
        _toggleAutoButton.Text = _timer.Enabled ? "Авто: вкл" : "Авто: выкл";
        UpdateCounterLabel();
    }

    private void CountdownSensorPolling()
    {
        _secondsUntilNextReading--;
        if (_secondsUntilNextReading <= 0)
        {
            PollSensor();
            ResetCountdown();
            return;
        }

        UpdateCounterLabel();
    }

    private void ResetCountdown()
    {
        _secondsUntilNextReading = SensorPollingSeconds;
        UpdateCounterLabel();
    }

    private void UpdateCounterLabel()
    {
        _counterLabel.Text = _timer.Enabled
            ? $"До опроса: {_secondsUntilNextReading} сек."
            : "Автоопрос остановлен";
    }

    private void AddEmployee()
    {
        var fullName = _employeeNameBox.Text.Trim();
        var position = _employeePositionBox.Text.Trim();
        var department = _employeeDepartmentBox.Text.Trim();
        var phone = _employeePhoneBox.Text.Trim();

        if (fullName.Length == 0 || position.Length == 0 || department.Length == 0 || phone.Length == 0)
        {
            MessageBox.Show("Заполните все поля сотрудника.", "Добавление сотрудника", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var nextEmployeeId = _database.Employees.Count == 0 ? 1 : _database.Employees.Max(e => e.Id) + 1;
        _database.Employees.Add(new Employee
        {
            Id = nextEmployeeId,
            FullName = fullName,
            Position = position,
            Department = department,
            Phone = phone
        });

        _employeeNameBox.Clear();
        _employeePositionBox.Clear();
        _employeeDepartmentBox.Clear();
        _employeePhoneBox.Clear();

        _fileDatabase.Save(_database);
        RefreshAll();
    }

    private void AddTask()
    {
        var title = _taskTitleBox.Text.Trim();
        var description = _taskDescriptionBox.Text.Trim();

        if (title.Length == 0 || description.Length == 0)
        {
            MessageBox.Show("Заполните название и описание задачи.", "Добавление задачи", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_taskEmployeeBox.SelectedItem is not Employee employee)
        {
            MessageBox.Show("Выберите сотрудника для задачи.", "Добавление задачи", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var nextTaskId = _database.Tasks.Count == 0 ? 1 : _database.Tasks.Max(t => t.Id) + 1;
        _database.Tasks.Add(new WorkTask
        {
            Id = nextTaskId,
            CreatedAt = DateTime.Now,
            SourceReadingId = 0,
            Title = title,
            Description = description,
            Priority = _taskPriorityBox.SelectedItem?.ToString() ?? "Средний",
            Status = "Новая",
            AssignedEmployeeId = employee.Id,
            AssignedEmployeeName = employee.FullName,
            Department = employee.Department,
            ContactPhone = employee.Phone
        });

        _taskTitleBox.Clear();
        _taskDescriptionBox.Clear();
        _taskPriorityBox.SelectedItem = "Средний";

        _fileDatabase.Save(_database);
        RefreshAll();
    }

    private void PollSensor()
    {
        var nextReadingId = _database.Readings.Count == 0 ? 1 : _database.Readings.Max(r => r.Id) + 1;
        var reading = _sensor.GetReading(nextReadingId);
        _database.Readings.Add(reading);

        if (reading.Status != "ОК")
        {
            var responsible = AskResponsibleEmployee(reading);
            if (responsible is not null)
            {
                _database.Tasks.Add(CreateTaskForReading(reading, responsible));
            }
        }

        _fileDatabase.Save(_database);
        RefreshAll();
    }

    private Employee? AskResponsibleEmployee(SensorReading reading)
    {
        if (_database.Employees.Count == 0)
        {
            MessageBox.Show("Сначала добавьте хотя бы одного сотрудника.", "Ответственный за задачу", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        var defaultEmployee = reading.Value > _sensor.MaxNormalValue
            ? FindEmployee("Эксплуатация датчиков")
            : FindEmployee("Ремонтная служба");

        using var dialog = new Form
        {
            Text = "Выбор ответственного",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(500, 230)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            Padding = new Padding(14)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

        var message = new Label
        {
            Dock = DockStyle.Fill,
            Text = $"Показание {reading.Value} {reading.Unit} вне нормы. Выберите ответственного сотрудника:",
            AutoEllipsis = false
        };

        var employeeBox = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = nameof(Employee.FullName)
        };
        foreach (var employee in _database.Employees)
        {
            employeeBox.Items.Add(employee);
        }
        employeeBox.SelectedItem = defaultEmployee;

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 12, 0, 0),
            WrapContents = false
        };
        var okButton = new Button { Text = "Создать задачу", Width = 150, Height = 34, DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "Отмена", Width = 100, Height = 34, DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(okButton);
        buttons.Controls.Add(cancelButton);

        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;
        layout.Controls.Add(message, 0, 0);
        layout.Controls.Add(new Label { Text = "Ответственный:", Dock = DockStyle.Fill }, 0, 1);
        layout.Controls.Add(employeeBox, 0, 2);
        layout.Controls.Add(buttons, 0, 3);
        dialog.Controls.Add(layout);

        var wasTimerEnabled = _timer.Enabled;
        _timer.Enabled = false;
        var result = dialog.ShowDialog(this);
        _timer.Enabled = wasTimerEnabled;
        ResetCountdown();

        return result == DialogResult.OK
            ? employeeBox.SelectedItem as Employee
            : null;
    }

    private WorkTask CreateTaskForReading(SensorReading reading, Employee employee)
    {
        var nextTaskId = _database.Tasks.Count == 0 ? 1 : _database.Tasks.Max(t => t.Id) + 1;
        return new WorkTask
        {
            Id = nextTaskId,
            CreatedAt = DateTime.Now,
            SourceReadingId = reading.Id,
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
        if (_tasksGrid.CurrentRow?.DataBoundItem is not WorkTask row)
        {
            return;
        }

        var taskIndex = _database.Tasks.FindIndex(t => t.Id == row.Id);
        if (taskIndex < 0)
        {
            return;
        }

        var task = _database.Tasks[taskIndex];
        task.Status = "Выполнена";
        _database.Tasks[taskIndex] = task;

        _fileDatabase.Save(_database);
        RefreshAll();
    }

    private void RefreshAll()
    {
        RefreshEmployeeSelector();

        var latest = _database.Readings.LastOrDefault();
        if (latest.Id == 0)
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

        _readingsGrid.DataSource = new BindingList<SensorReading>(_database.Readings
            .OrderByDescending(r => r.Timestamp)
            .ToList());
        SetHeader(_readingsGrid, nameof(SensorReading.Id), "N");
        SetHeader(_readingsGrid, nameof(SensorReading.Timestamp), "Время");
        SetHeader(_readingsGrid, nameof(SensorReading.SensorName), "Датчик");
        SetHeader(_readingsGrid, nameof(SensorReading.Value), "Значение");
        SetHeader(_readingsGrid, nameof(SensorReading.Unit), "Ед.");
        SetHeader(_readingsGrid, nameof(SensorReading.Status), "Статус");
        SetHeader(_readingsGrid, nameof(SensorReading.Comment), "Комментарий");
        SetColumnWidth(_readingsGrid, nameof(SensorReading.Id), 45);
        SetColumnWidth(_readingsGrid, nameof(SensorReading.Timestamp), 130);
        SetColumnWidth(_readingsGrid, nameof(SensorReading.Value), 80);
        SetColumnWidth(_readingsGrid, nameof(SensorReading.Unit), 55);
        SetColumnWidth(_readingsGrid, nameof(SensorReading.Status), 95);
        SetColumnFillWeight(_readingsGrid, nameof(SensorReading.SensorName), 140);
        SetColumnFillWeight(_readingsGrid, nameof(SensorReading.Comment), 220);

        _tasksGrid.DataSource = new BindingList<WorkTask>(GetSortedTasks().ToList());
        SetHeader(_tasksGrid, nameof(WorkTask.Id), "N");
        SetHeader(_tasksGrid, nameof(WorkTask.CreatedAt), "Создана");
        SetHeader(_tasksGrid, nameof(WorkTask.SourceReadingId), "Показание");
        SetHeader(_tasksGrid, nameof(WorkTask.Title), "Задача");
        SetHeader(_tasksGrid, nameof(WorkTask.Description), "Описание");
        SetHeader(_tasksGrid, nameof(WorkTask.Priority), "Приоритет");
        SetHeader(_tasksGrid, nameof(WorkTask.Status), "Статус");
        SetHeader(_tasksGrid, nameof(WorkTask.AssignedEmployeeId), "ID сотрудника");
        SetHeader(_tasksGrid, nameof(WorkTask.AssignedEmployeeName), "Сотрудник");
        SetHeader(_tasksGrid, nameof(WorkTask.Department), "Подразделение");
        SetHeader(_tasksGrid, nameof(WorkTask.ContactPhone), "Телефон");
        SetColumnWidth(_tasksGrid, nameof(WorkTask.Id), 45);
        SetColumnWidth(_tasksGrid, nameof(WorkTask.CreatedAt), 130);
        SetColumnWidth(_tasksGrid, nameof(WorkTask.SourceReadingId), 90);
        SetColumnWidth(_tasksGrid, nameof(WorkTask.Priority), 95);
        SetColumnWidth(_tasksGrid, nameof(WorkTask.Status), 100);
        SetColumnWidth(_tasksGrid, nameof(WorkTask.AssignedEmployeeId), 110);
        SetColumnFillWeight(_tasksGrid, nameof(WorkTask.Title), 140);
        SetColumnFillWeight(_tasksGrid, nameof(WorkTask.Description), 240);

        _employeesGrid.DataSource = new BindingList<Employee>(_database.Employees.ToList());
        SetHeader(_employeesGrid, nameof(Employee.Id), "N");
        SetHeader(_employeesGrid, nameof(Employee.FullName), "ФИО");
        SetHeader(_employeesGrid, nameof(Employee.Position), "Должность");
        SetHeader(_employeesGrid, nameof(Employee.Department), "Подразделение");
        SetHeader(_employeesGrid, nameof(Employee.Phone), "Телефон");
    }

    private void RefreshEmployeeSelector()
    {
        var selectedEmployeeId = _taskEmployeeBox.SelectedItem is Employee selectedEmployee
            ? selectedEmployee.Id
            : 0;

        _taskEmployeeBox.Items.Clear();
        foreach (var employee in _database.Employees)
        {
            _taskEmployeeBox.Items.Add(employee);
        }

        _taskEmployeeBox.DisplayMember = nameof(Employee.FullName);
        _taskEmployeeBox.ValueMember = nameof(Employee.Id);

        var employeeToSelect = _database.Employees.FirstOrDefault(e => e.Id == selectedEmployeeId)
            ?? _database.Employees.FirstOrDefault();
        if (employeeToSelect is not null)
        {
            _taskEmployeeBox.SelectedItem = employeeToSelect;
        }
    }

    private IEnumerable<WorkTask> GetSortedTasks()
    {
        var sortBy = _taskSortBox.SelectedItem?.ToString() ?? "Дата";
        var ascending = _taskSortDirectionBox.SelectedItem?.ToString() == "По возрастанию";

        return sortBy switch
        {
            "Приоритет" => ascending
                ? _database.Tasks.OrderBy(t => GetPriorityRank(t.Priority)).ThenBy(t => t.CreatedAt)
                : _database.Tasks.OrderByDescending(t => GetPriorityRank(t.Priority)).ThenByDescending(t => t.CreatedAt),
            "Сотрудник" => ascending
                ? _database.Tasks.OrderBy(t => t.AssignedEmployeeName).ThenByDescending(t => t.CreatedAt)
                : _database.Tasks.OrderByDescending(t => t.AssignedEmployeeName).ThenByDescending(t => t.CreatedAt),
            _ => ascending
                ? _database.Tasks.OrderBy(t => t.CreatedAt)
                : _database.Tasks.OrderByDescending(t => t.CreatedAt)
        };
    }

    private static int GetPriorityRank(string priority)
    {
        return priority switch
        {
            "Низкий" => 1,
            "Средний" => 2,
            "Высокий" => 3,
            _ => 0
        };
    }

    private static void SetHeader(DataGridView grid, string propertyName, string header)
    {
        if (grid.Columns.Contains(propertyName))
        {
            grid.Columns[propertyName].HeaderText = header;
        }
    }

    private static void SetColumnWidth(DataGridView grid, string propertyName, int width)
    {
        if (grid.Columns.Contains(propertyName))
        {
            grid.Columns[propertyName].MinimumWidth = width;
            grid.Columns[propertyName].FillWeight = width;
        }
    }

    private static void SetColumnFillWeight(DataGridView grid, string propertyName, int weight)
    {
        if (grid.Columns.Contains(propertyName))
        {
            grid.Columns[propertyName].FillWeight = weight;
        }
    }
}
