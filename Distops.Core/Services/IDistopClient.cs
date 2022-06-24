using Distops.Core.Model;

namespace Distops.Core.Services;

public interface IDistopClient
{
    /// <summary>
    /// Submit the distributed operation and wait it is completed. If a value is to be returned, it will be retried.
    /// </summary>
    /// <param name="distopContext">The operation context of the distributed operation.</param>
    /// <returns>A task that when awaited will return the returned result of the distributed operation.</returns>
    Task<object?> Call(DistopContext distopContext, CancellationToken? cancellationToken = default);

    /// <summary>
    /// Submit the distributed operation and wait until it's accepted
    /// </summary>
    /// <param name="distopContext">The operation context of the distributed operation.</param>
    /// <returns>A task that completes when the distributed operation has been submitted/Accepted.</returns>
    Task FireAndForget(DistopContext distopContext, CancellationToken? cancellationToken = default);
}