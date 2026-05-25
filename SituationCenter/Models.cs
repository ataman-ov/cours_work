namespace SituationCenter;

public sealed class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Position { get; set; } = "";
    public string Department { get; set; } = "";
    public string Phone { get; set; } = "";
}

public struct SensorReading
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string SensorName { get; set; }
    public double Value { get; set; }
    public string Unit { get; set; }
    public string Status { get; set; }
    public string Comment { get; set; }
}

public struct WorkTask
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public int SourceReadingId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Priority { get; set; }
    public string Status { get; set; }
    public int AssignedEmployeeId { get; set; }
    public string AssignedEmployeeName { get; set; }
    public string Department { get; set; }
    public string ContactPhone { get; set; }
}

public sealed class SituationCenterDatabase
{
    public List<Employee> Employees { get; set; } = [];
    public List<SensorReading> Readings { get; set; } = [];
    public List<WorkTask> Tasks { get; set; } = [];
}
