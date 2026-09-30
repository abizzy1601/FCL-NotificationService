namespace NotificationService.Application.BasicResults
{
    public class Error
    {
        public static readonly Error None = new(string.Empty, string.Empty);

        public Error(string code, string message)
        {
            Code = code;
            Message = message;
        }

        public string Code { get; }
        public string Message { get; }

        public static Error NotFound(string message) => new("NotFound", message);
        public static Error Validation(string message) => new("Validation", message);
        public static Error Throttled(string message) => new("Throttled", message);
        public static Error Conflict(string message) => new("Conflict", message);
    }

    public class Result
    {
        protected Result(bool isSuccess, Error error)
        {
            if (isSuccess && error != Error.None)
                throw new InvalidOperationException("A successful result cannot contain an error.");
            if (!isSuccess && error == Error.None)
                throw new InvalidOperationException("A failed result must contain an error.");

            IsSuccess = isSuccess;
            Error = error;
        }

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public Error Error { get; }

        public static Result Success() => new(true, Error.None);
        public static Result Failure(Error error) => new(false, error);

        public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
        public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
    }

    public class Result<TValue> : Result
    {
        private readonly TValue? _value;

        protected internal Result(TValue? value, bool isSuccess, Error error)
            : base(isSuccess, error)
        {
            _value = value;
        }

        public TValue Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access the value of a failed result.");

        public static implicit operator Result<TValue>(TValue value) => Success(value);
    }
}
