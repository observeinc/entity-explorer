using CsvHelper;
using CsvHelper.Configuration;
using NLog;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Observe.EntityExplorer
{
    public class CsvHelperHelper
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public static List<T> ReadListFromCSVString<T>(string csvData)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
                BadDataFound = rc =>
                    {
                        logger.Warn("Bad data found on field {0} in row {1}", rc.Field, rc.RawRecord);
                    }
            };

            using (var reader = new StringReader(csvData))
            {
                using (var csv = new CsvReader(reader, config))
                {
                    List<T> transformUsageList = csv.GetRecords<T>().ToList();
                    return transformUsageList;
                }
            }
        }
    }
}