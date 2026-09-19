namespace Refacto.DotNet.Controllers.Infrastructure.Time
{
    public interface IDateTimeProvider
    {
        DateTime Now { get; }

        DateTime Today { get; }
    }
}
