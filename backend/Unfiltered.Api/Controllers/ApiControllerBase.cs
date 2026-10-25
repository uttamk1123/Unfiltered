using Microsoft.AspNetCore.Mvc;
using Unfiltered.Api.Dtos;
using Unfiltered.Api.Infrastructure;

namespace Unfiltered.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>The signed-in user's id. Only call from actions that require authorization.</summary>
    protected int CurrentUserId => User.UserId() ?? throw new InvalidOperationException("No signed-in user.");

    protected ObjectResult Fail(string message, int status = StatusCodes.Status400BadRequest) =>
        StatusCode(status, new ErrorResponse(message));

    protected ObjectResult NotFoundError(string message) => Fail(message, StatusCodes.Status404NotFound);
}
