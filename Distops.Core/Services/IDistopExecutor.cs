using Distops.Core.Model;

namespace Distops.Core.Services;

public interface IDistopExecutor
{
    Task<Result<object?, Exception>> ExecuteDistop(DistopContext distopContext);
}