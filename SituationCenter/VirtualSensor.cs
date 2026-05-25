namespace SituationCenter;

public sealed class VirtualSensor : ISensor
{
    private readonly Random _random = new();

    public string Name => "Виртуальный датчик температуры Т-01";
    public string Unit => "°C";
    public double MinNormalValue => 18.0;
    public double MaxNormalValue => 32.0;

    public SensorReading GetReading(int id)
    {
        var baseValue = 25 + (_random.NextDouble() * 8 - 4);
        var abnormalChance = _random.Next(100);

        var value = abnormalChance switch
        {
            < 12 => 34 + _random.NextDouble() * 9,
            > 92 => 10 + _random.NextDouble() * 6,
            _ => baseValue
        };

        value = Math.Round(value, 1);
        var isOk = value >= MinNormalValue && value <= MaxNormalValue;

        return new SensorReading
        {
            Id = id,
            Timestamp = DateTime.Now,
            SensorName = Name,
            Unit = Unit,
            Value = value,
            Status = isOk ? "ОК" : "НЕ НОРМА",
            Comment = isOk
                ? "Показатель находится в допустимом диапазоне."
                : $"Показатель вне нормы. Допустимый диапазон: {MinNormalValue}-{MaxNormalValue} {Unit}."
        };
    }
}
