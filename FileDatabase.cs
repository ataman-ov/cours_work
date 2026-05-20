using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace SituationCenter;

public sealed class FileDatabase
{
    private readonly string _path;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    public FileDatabase(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
    }

    public SituationCenterDatabase Load()
    {
        if (!File.Exists(_path))
        {
            var created = CreateInitialDatabase();
            Save(created);
            return created;
        }

        var json = File.ReadAllText(_path);
        return JsonSerializer.Deserialize<SituationCenterDatabase>(json, _jsonOptions) ?? CreateInitialDatabase();
    }

    public void Save(SituationCenterDatabase database)
    {
        var json = JsonSerializer.Serialize(database, _jsonOptions);
        File.WriteAllText(_path, json);
    }

    public static SituationCenterDatabase CreateInitialDatabase()
    {
        return new SituationCenterDatabase
        {
            Employees =
            [
                new Employee { Id = 1, FullName = "Алексей Морозов", Position = "Инженер КИПиА", Department = "Эксплуатация датчиков", Phone = "+7 (900) 111-20-31" },
                new Employee { Id = 2, FullName = "Марина Кузнецова", Position = "Дежурный диспетчер", Department = "Ситуационный центр", Phone = "+7 (900) 222-14-88" },
                new Employee { Id = 3, FullName = "Игорь Павлов", Position = "Техник аварийной группы", Department = "Ремонтная служба", Phone = "+7 (900) 333-45-19" },
                new Employee { Id = 4, FullName = "Светлана Орлова", Position = "Аналитик мониторинга", Department = "Аналитический отдел", Phone = "+7 (900) 444-77-02" },
                new Employee { Id = 5, FullName = "Дмитрий Соколов", Position = "Начальник смены", Department = "Оперативное управление", Phone = "+7 (900) 555-63-40" },
                new Employee { Id = 6, FullName = "Наталья Белова", Position = "Специалист по охране труда", Department = "Безопасность", Phone = "+7 (900) 666-90-12" }
            ]
        };
    }
}
