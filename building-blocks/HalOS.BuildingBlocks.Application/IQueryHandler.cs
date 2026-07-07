using HalOS.BuildingBlocks.Domain;
using MediatR;

namespace HalOS.BuildingBlocks.Application;

/// <summary>Handler for an <see cref="IQuery{TResponse}"/>.</summary>
public interface IQueryHandler<in TQuery, TResponse>
    : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
