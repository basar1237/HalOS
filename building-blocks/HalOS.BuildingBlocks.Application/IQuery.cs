using HalOS.BuildingBlocks.Domain;
using MediatR;

namespace HalOS.BuildingBlocks.Application;

/// <summary>
/// Marker for a read operation (CQRS query) that returns a <see cref="Result{TResponse}"/>.
/// Queries are named "verb + Query" (e.g. GetCurrentAccountQuery), per docs/07 §3/§5.
/// </summary>
/// <typeparam name="TResponse">Value produced on success.</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
