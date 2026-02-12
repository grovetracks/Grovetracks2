using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Interfaces;

public interface ICompositionMapper
{
    Task<Composition> MapToCompositionAsync(
        QuickdrawDoodle doodle,
        CancellationToken cancellationToken = default);
}
