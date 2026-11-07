using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SuracDev.Api.Shared.Application.Exceptions;
using SuracDev.Api.Shared.Infrastructure;

namespace SuracDev.Api.Tests.Shared;

public sealed class GlobalExceptionHandlerTests
{
  [Fact]
  public async Task Validation_errors_become_400_with_field_errors()
  {
    var (handler, problems, context) = Arrange();

    var handled = await handler.TryHandleAsync(
      context, new DomainValidationException("password", "Too short."), CancellationToken.None);

    Assert.True(handled);
    Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    var details = Assert.IsType<ValidationProblemDetails>(problems.Written?.ProblemDetails);
    Assert.Equal(["Too short."], details.Errors["password"]);
  }

  [Fact]
  public async Task Conflicts_become_409_with_the_message_as_detail()
  {
    var (handler, problems, context) = Arrange();

    var handled = await handler.TryHandleAsync(context, new DomainConflictException("Already set."), CancellationToken.None);

    Assert.True(handled);
    Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
    Assert.Equal("Already set.", problems.Written?.ProblemDetails.Detail);
  }

  [Fact]
  public async Task Missing_resources_become_404()
  {
    var (handler, problems, context) = Arrange();

    var handled = await handler.TryHandleAsync(context, new DomainNotFoundException("No such thing."), CancellationToken.None);

    Assert.True(handled);
    Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    Assert.Equal("No such thing.", problems.Written?.ProblemDetails.Detail);
  }

  [Fact]
  public async Task Unexpected_exceptions_are_left_to_the_default_handling()
  {
    var (handler, problems, context) = Arrange();

    var handled = await handler.TryHandleAsync(context, new InvalidOperationException("boom"), CancellationToken.None);

    Assert.False(handled);
    Assert.Null(problems.Written);
  }

  private static (GlobalExceptionHandler Handler, RecordingProblemDetailsService Problems, DefaultHttpContext Context) Arrange()
  {
    var problems = new RecordingProblemDetailsService();
    return (new GlobalExceptionHandler(problems), problems, new DefaultHttpContext());
  }

  private sealed class RecordingProblemDetailsService : IProblemDetailsService
  {
    public ProblemDetailsContext? Written { get; private set; }

    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
      Written = context;
      return ValueTask.CompletedTask;
    }

    public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
    {
      Written = context;
      return ValueTask.FromResult(true);
    }
  }
}
