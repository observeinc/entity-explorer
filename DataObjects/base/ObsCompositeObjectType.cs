namespace Observe.EntityExplorer.DataObjects
{
    [Flags]
    public enum ObsCompositeObjectType
    {
        Unknown          = 0,

        Dashboard         = 1 << 2,

        Worksheet         = 1 << 4,
        
        Monitor                         = 1 << 5,
        MetricThresholdMonitor          = 1 << 6,
        LogThresholdMonitor             = 1 << 7,
        ResourceCountThresholdMonitor   = 1 << 8,
        PromotionMonitor                = 1 << 9,
        ResourceTextValueMonitor        = 1 << 10,

        Monitor2                        = 1 << 11,

        Dataset                     = 1 << 16,
        DatastreamDataset           = 1 << 17,
        EventDataset                = 1 << 18,
        ResourceDataset             = 1 << 19,
        IntervalDataset             = 1 << 20,
        TableDataset                = 1 << 21,
        MetricSMADataset            = 1 << 22,
        MonitorSupportDataset       = 1 << 23,
        InterfaceMetricDataset      = 1 << 24,
        InterfaceLogDataset         = 1 << 25,

        Datastream                = 1 << 30,
        Token                     = 1 << 31,
        IngestToken               = 1 << 32,
        PollerToken               = 1 << 33,
        FiledropToken             = 1 << 34,

    }
}