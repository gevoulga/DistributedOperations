using Distops.Core.Model;

namespace Distops.Core.Services;

public interface IDistopExecutor
{
    // TODO support cancellation token?
    Task<Result<object?, Exception>> ExecuteDistop(DistopContext distopContext);
}