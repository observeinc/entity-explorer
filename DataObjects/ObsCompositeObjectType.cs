namespace Observe.EntityExplorer.DataObjects
{
    [Flags]
    public enum ObsCompositeObjectType
    {
        Unknown          = 0,
        Dashboard         = 1 << 0,
        Worksheet         = 1 << 4,
        Monitor           = 1 << 5,
        MetricMonitor     = 1 << 6,
        LogMonitor        = 1 << 7,
        CountMonitor      = 1 << 8,
        ThresholdMonitor  = 1 << 9,
        TextMonitor       = 1 << 10,
        Dataset           = 1 << 16,
        DatastreamDataset = 1 << 17,
        EventDataset      = 1 << 18,
        ResourceDataset   = 1 << 19,
        IntervalDataset   = 1 << 20,
        MetricSMADataset  = 1 << 21,
        MonitorSupportDataset = 1 << 22,
        InterfaceMetricDataset = 1 << 23,
        InterfaceLogDataset = 1 << 24
    }
}