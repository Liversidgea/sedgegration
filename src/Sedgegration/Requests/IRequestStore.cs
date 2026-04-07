using Sedgegration.Models;
using Sedgegration.Requests;

namespace Sedgegration.Requests;

public interface IRequestStore
{
    Task SaveAsync(PersistedRequest request);
    Task<IReadOnlyList<PersistedRequest>> GetAllAsync();
    Task<PersistedRequest?> GetAsync(string id);
}
