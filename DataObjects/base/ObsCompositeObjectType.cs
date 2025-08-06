namespace Observe.EntityExplorer.DataObjects
{
    [Flags]
    public enum ObsCompositeObjectType
    {
        Unknown          = 0,

        Dashboard         = 1 << 1,

        Worksheet         = 1 << 2,
        
        Monitor                         = 1 << 3,
        MetricThresholdMonitor          = 1 << 4,
        LogThresholdMonitor             = 1 << 5,
        ResourceCountThresholdMonitor   = 1 << 6,
        PromotionMonitor                = 1 << 7,
        ResourceTextValueMonitor        = 1 << 8,

        Monitor2                        = 1 << 9,

        Dataset                     = 1 << 10,
        DatastreamDataset           = 1 << 11,
        EventDataset                = 1 << 12,
        ResourceDataset             = 1 << 13,
        IntervalDataset             = 1 << 14,
        TableDataset                = 1 << 15,
        MetricSMADataset            = 1 << 16,
        MonitorSupportDataset       = 1 << 17,
        InterfaceMetricDataset      = 1 << 18,
        InterfaceLogDataset         = 1 << 19,
        ViewDataset                 = 1 << 25,

        Datastream                = 1 << 20,
        Token                     = 1 << 21,
        IngestToken               = 1 << 22,
        PollerToken               = 1 << 23,
        FiledropToken             = 1 << 24,

    }
}