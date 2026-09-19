namespace Refacto.DotNet.Controllers.Development
{
    public sealed class NotificationRecorder
    {
        private readonly List<string> _entries = new();

        public IReadOnlyList<string> Entries
        {
            get
            {
                lock (_entries)
                {
                    return _entries.ToList();
                }
            }
        }

        public void Record(string entry)
        {
            lock (_entries)
            {
                _entries.Add(entry);
            }
        }

        public void Clear()
        {
            lock (_entries)
            {
                _entries.Clear();
            }
        }
    }
}
