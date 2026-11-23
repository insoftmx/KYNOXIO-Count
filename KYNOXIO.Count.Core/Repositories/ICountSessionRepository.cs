using KYNOXIO.Count.Core.Models;

namespace KYNOXIO.Count.Core.Repositories;

/// <summary>
/// Contrato de persistencia para las sesiones de conteo.
/// </summary>
public interface ICountSessionRepository
{
    Task AddAsync(CountSession session);
    Task UpdateAsync(CountSession session);
    Task<IReadOnlyList<CountSession>> GetAllAsync();
    Task<CountSession?> GetByIdAsync(string id);
}