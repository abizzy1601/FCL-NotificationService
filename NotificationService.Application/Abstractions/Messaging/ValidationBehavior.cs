using FluentValidation;
using MediatR;
using NotificationService.Application.BasicResults;

namespace NotificationService.Application.Abstractions.Messaging
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IBaseCommand
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!_validators.Any())
                return await next();

            var failures = _validators
                .Select(v => v.Validate(request))
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count == 0)
                return await next();

            var error = Error.Validation(string.Join("; ", failures.Select(f => f.ErrorMessage)));

            if (typeof(TResponse) == typeof(Result))
                return (TResponse)(object)Result.Failure(error);

            var resultType = typeof(TResponse).GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethod(nameof(Result.Failure), 1, new[] { typeof(Error) })!
                .MakeGenericMethod(resultType);

            return (TResponse)failureMethod.Invoke(null, new object[] { error })!;
        }
    }
}
