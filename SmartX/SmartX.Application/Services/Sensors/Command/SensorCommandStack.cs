using System.Collections.Concurrent;

namespace SmartX.Application.Services.Sensors.Command;

public sealed class SensorCommandStack
{
    private readonly ConcurrentDictionary<Guid, ManualStack<SensorCommand>>
        _stacks = new();

    private readonly ConcurrentDictionary<Guid, object>
        _locks = new();

    public void Push(SensorCommand command)
    {
        lock (GetLock(command.SensorId))
        {
            GetStack(command.SensorId).Push(command);
        }
    }

    public bool TryPeek(Guid sensorId, out SensorCommand? command)
    {
        lock (GetLock(sensorId))
        {
            return GetStack(sensorId).TryPeek(out command);
        }
    }

    public bool TryPop(Guid sensorId, out SensorCommand? command)
    {
        lock (GetLock(sensorId))
        {
            return GetStack(sensorId).TryPop(out command);
        }
    }

    public void Clear(Guid sensorId)
    {
        lock (GetLock(sensorId))
        {
            GetStack(sensorId).Clear();
        }
    }

    private ManualStack<SensorCommand> GetStack(Guid sensorId)
    {
        return _stacks.GetOrAdd(
            sensorId,
            _ => new ManualStack<SensorCommand>());
    }

    private object GetLock(Guid sensorId)
    {
        return _locks.GetOrAdd(
            sensorId,
            _ => new object());
    }
}