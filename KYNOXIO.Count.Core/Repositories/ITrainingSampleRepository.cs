using KYNOXIO.Count.Core.Models;

namespace KYNOXIO.Count.Core.Repositories;

/// <summary>Persistencia aislada de muestras destinadas al entrenamiento de IA.</summary>
public interface ITrainingSampleRepository
{
    Task AddAsync(TrainingSample sample);
    Task UpdateAsync(TrainingSample sample);
    Task<IReadOnlyList<TrainingSample>> GetAllAsync();
    Task<TrainingSample?> GetByIdAsync(string id);
}