namespace SituationCenter;

public interface IDataStore
{
    SituationCenterDatabase Load();
    void Save(SituationCenterDatabase database);
}

public interface ISensor
{
    string Name { get; }
    string Unit { get; }
    double MinNormalValue { get; }
    double MaxNormalValue { get; }
    SensorReading GetReading(int id);
}
