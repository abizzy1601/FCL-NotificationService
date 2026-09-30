namespace NotificationService.Application
{
    /// <summary>
    /// Abstraction over "who is calling right now" so Application/UseCases never
    /// touch HttpContext directly. Implemented in the API project against the
    /// authenticated principal (e.g. NameIdentifier / CustomerId claim).
    /// </summary>
    public interface IUserContext
    {
        string UserId { get; }
    }
}
