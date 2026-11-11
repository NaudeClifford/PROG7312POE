using SmartX.Domain.Entities;

namespace SmartX.Application.Services.Sensors;

public sealed class SensorRegistry
{
    private readonly Dictionary<string, Sensor> _sensorsByMac = new(
        StringComparer.OrdinalIgnoreCase);

    public int Count => _sensorsByMac.Count;

    // REGISTER SENSOR
    public void Register(Sensor sensor)
    {
        ArgumentNullException.ThrowIfNull(sensor);

        if (string.IsNullOrWhiteSpace(sensor.DeviceIdentifier))
            return;

        _sensorsByMac[sensor.DeviceIdentifier] = sensor;
    }

    // FIND SENSOR BY MAC ADDRESS
    public bool TryGetSensor(
        string macAddress,
        out Sensor? sensor)
    {
        return _sensorsByMac.TryGetValue(
            macAddress,
            out sensor);
    }

    // REMOVE SENSOR
    public bool Remove(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            return false;

        return _sensorsByMac.Remove(macAddress);
    }

    // CLEAR REGISTRY
    public void Clear()
    {
        _sensorsByMac.Clear();
    }

    // GET ALL REGISTERED SENSORS
    public IEnumerable<Sensor> GetAll()
    {
        return _sensorsByMac.Values.ToList();
    }

    public Sensor? GetSensor(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            return null;

        _sensorsByMac.TryGetValue(
            macAddress,
            out var sensor);

        return sensor;
    }
}