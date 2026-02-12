using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Interfaces;

public interface IDrawingStorageService
{
    Task<RawDrawing> GetDrawingAsync(
        string drawingReference,
        CancellationToken cancellationToken = default);
}
