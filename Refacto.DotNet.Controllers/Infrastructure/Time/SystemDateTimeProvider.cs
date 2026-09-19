namespace Refacto.DotNet.Controllers.Infrastructure.Time
{
    public sealed class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTime Now => DateTime.Now;

        public DateTime Today => DateTime.Now.Date;
    }
}
