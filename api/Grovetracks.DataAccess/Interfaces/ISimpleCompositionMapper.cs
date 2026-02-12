using Grovetracks.DataAccess.Entities;
using Grovetracks.DataAccess.Models;

namespace Grovetracks.DataAccess.Interfaces;

public interface ISimpleCompositionMapper
{
    Composition MapToComposition(QuickdrawSimpleDoodle doodle);
}
