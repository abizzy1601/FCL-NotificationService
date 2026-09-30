using Microsoft.AspNetCore.Mvc;

namespace NotificationService.API.Helpers
{
    /// <summary>
    /// Wired into ApiBehaviorOptions.InvalidModelStateResponseFactory (Program.cs)
    /// so a bad request body returns the same { title, status, errors } shape as
    /// everything else instead of the framework's default ValidationProblemDetails.
    /// </summary>
    public static class CustomInvalidModelStateResponseFactory
    {
        public static IActionResult Create(ActionContext context)
        {
            var errors = context.ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            var problemDetails = new ValidationProblemDetails(errors)
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Instance = context.HttpContext.Request.Path
            };

            return new BadRequestObjectResult(problemDetails);
        }
    }
}
