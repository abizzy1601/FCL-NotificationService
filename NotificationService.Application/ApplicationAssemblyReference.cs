using System.Reflection;

namespace NotificationService.Application
{
    /// <summary>Marker used for MediatR/FluentValidation assembly scanning.</summary>
    public static class ApplicationAssemblyReference
    {
        public static readonly Assembly Assembly = typeof(ApplicationAssemblyReference).Assembly;
    }
}
