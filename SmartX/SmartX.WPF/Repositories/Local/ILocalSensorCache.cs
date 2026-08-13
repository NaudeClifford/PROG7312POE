using SmartX.Domain.Entities;

namespace SmartX.WPF.Repositories.Local
{
    public interface ILocalSensorCache
    {

        Task<Sensor?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Sensor>> GetAllAsync(
            CancellationToken cancellationToken = default);

        Task UpdateAsync(
            Sensor sensor, CancellationToken cancellationToken = default);
    }
}
