using HalOS.BuildingBlocks.Domain;
using MediatR;

namespace HalOS.BuildingBlocks.Application;

/// <summary>Handler for an <see cref="ICommand"/>.</summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

/// <summary>Handler for an <see cref="ICommand{TResponse}"/>.</summary>
public interface ICommandHandler<in TCommand, TResponse>
    : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
