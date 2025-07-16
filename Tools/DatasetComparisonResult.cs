namespace Observe.EntityExplorer.Tools
{
    public class DatasetComparisonResult
    {
        public string SourceName { get; set; } = string.Empty; // dashboard or monitor
        public string StageName { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public int BaselineRowCount { get; set; }
        public int OptimizedRowCount { get; set; }
        public List<string> BaselineColumns { get; set; } = new();
        public List<string> OptimizedColumns { get; set; } = new();
        public List<string[]> BaselineSample { get; set; } = new();
        public List<string[]> OptimizedSample { get; set; } = new();
        public string BaselineCsv { get; set; } = string.Empty;
        public string OptimizedCsv { get; set; } = string.Empty;
        public bool? Match { get; set; }
        public string Error { get; set; } = string.Empty;
    }
}
