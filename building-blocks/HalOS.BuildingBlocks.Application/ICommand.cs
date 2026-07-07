using HalOS.BuildingBlocks.Domain;
using MediatR;

namespace HalOS.BuildingBlocks.Application;

/// <summary>
/// Marker for a write operation (CQRS command) that returns a plain <see cref="Result"/>.
/// Commands are named "verb + Command" (e.g. CreateSaleCommand), per docs/07 §3/§5.
/// </summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>
/// Marker for a write operation (CQRS command) that returns a <see cref="Result{TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">Value produced on success.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
