using Refacto.DotNet.Controllers.Infrastructure.Time;

namespace Refacto.Dotnet.Controllers.Tests.Fakes
{
    public sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public static readonly DateTime DefaultNow = new(2024, 6, 15, 10, 30, 0);

        public DateTime Now { get; set; } = DefaultNow;

        public DateTime Today => Now.Date;
    }
}
