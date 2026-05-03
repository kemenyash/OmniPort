namespace Web.Telemetry
{
    public sealed record ApplicationLogPage(
        IReadOnlyList<ApplicationLogEntry> Entries,
        int PageIndex,
        int PageSize,
        int TotalCount,
        int TotalPages)
    {
        public bool HasNewer => PageIndex > 0;

        public bool HasOlder => PageIndex + 1 < TotalPages;
    }
}
