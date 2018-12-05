using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Thrive2018Demo.Utilities
{
    class Logger : SPDiagnosticsServiceBase
    {
        private const string DiagnosticAreaName = "Thrive2018";

        /// <summary>
        /// Constructor
        /// </summary>
        private Logger() : base("Thrive 2018 Logging Service", SPFarm.Local)
        { }

        private static Logger loggerInstance;
        /// <summary>
        /// Single logger instance.
        /// </summary>
        private static Logger LoggerInstance
        {
            get { return loggerInstance ?? (loggerInstance = new Logger()); }
        }

        /// <summary>
        /// Write an error to sharepoint ulm log.
        /// </summary>
        /// <param name="ex">exception to log</param>
        public static void ToLog(Exception ex)
        {
            var category = LoggerInstance.Areas[DiagnosticAreaName].Categories["Error"];
            LoggerInstance.WriteTrace(0, category, TraceSeverity.High,
                String.Format(CultureInfo.InvariantCulture, "{0}\n{1}", ex, ExtractStack(ex)));

            //ToLogEvent(ex, "Error");
        }

        public static void ToLog(Exception ex, string opis)
        {
            var category = LoggerInstance.Areas[DiagnosticAreaName].Categories["Error"];
            LoggerInstance.WriteTrace(0, category, TraceSeverity.High,
                String.Format(CultureInfo.InvariantCulture, "{0}\n{1}\n{2}", opis, ex, ExtractStack(ex)));

            //ToLogEvent(opis, ex, "Error");
        }

        public static void ToLogEvent(Exception ex, string diagCategory)
        {
            CheckIfEventSourceExists(DiagnosticAreaName);
            var category = LoggerInstance.Areas[DiagnosticAreaName].Categories[diagCategory];
            LoggerInstance.WriteEvent(GetEventId(diagCategory), category, GetEventSeverity(diagCategory), String.Format(CultureInfo.InvariantCulture, "{0}\n{1}", ex, ExtractStack(ex)));
        }

        public static void ToLogEvent(string customMessage, string diagCategory)
        {
            CheckIfEventSourceExists(DiagnosticAreaName);
            var category = LoggerInstance.Areas[DiagnosticAreaName].Categories[diagCategory];
            LoggerInstance.WriteEvent(GetEventId(diagCategory), category, GetEventSeverity(diagCategory), String.Format(CultureInfo.InvariantCulture, "{0}\n{1}", "Informacije", customMessage));
        }
        public static void ToLogEvent(string customMessage, Exception ex, string diagCategory)
        {
            CheckIfEventSourceExists(DiagnosticAreaName);
            var category = LoggerInstance.Areas[DiagnosticAreaName].Categories[diagCategory];
            LoggerInstance.WriteEvent(GetEventId(diagCategory), category, GetEventSeverity(diagCategory), String.Format(CultureInfo.InvariantCulture, "{0}\n{1}\n{2}", customMessage, ex, ExtractStack(ex)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<SPDiagnosticsArea> ProvideAreas()
        {
            var areas = new List<SPDiagnosticsArea> {
                new SPDiagnosticsArea(DiagnosticAreaName,
                    new List<SPDiagnosticsCategory> {
                        new SPDiagnosticsCategory("Error", TraceSeverity.High, EventSeverity.Error),
                        new SPDiagnosticsCategory("Warning", TraceSeverity.Medium, EventSeverity.Warning),
                        new SPDiagnosticsCategory("Information", TraceSeverity.Monitorable, EventSeverity.Information)
                    })
            };
            return areas;
        }

        /// <summary>
        /// Extract stack trace from exception.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private static string ExtractStack(Exception ex)
        {
            var ret = new List<string>();
            var exCurrent = ex;
            while (exCurrent != null)
            {
                ret.Add(ex.StackTrace);
                exCurrent = exCurrent.InnerException;
            }
            return String.Join("\n", ret.ToArray());
        }

        private static ushort GetEventId(string diagCategory)
        {
            ushort eventId = 0;
            switch (diagCategory)
            {
                case "Information":
                    eventId = 4444;
                    break;
                case "Warning":
                    eventId = 4445;
                    break;
                case "Error":
                    eventId = 4446;
                    break;
            }
            return eventId;
        }

        private static EventSeverity GetEventSeverity(string diagCategory)
        {
            var eventSeverity = EventSeverity.None;
            switch (diagCategory)
            {
                case "Information":
                    eventSeverity = EventSeverity.Information;
                    break;
                case "Warning":
                    eventSeverity = EventSeverity.Warning;
                    break;
                case "Error":
                    eventSeverity = EventSeverity.Error;
                    break;
            }
            return eventSeverity;
        }

        private static void CheckIfEventSourceExists(string type)
        {
            SPSecurity.RunWithElevatedPrivileges(delegate ()
            {
                if (!EventLog.SourceExists(type))
                {
                    try
                    {
                        EventLog.CreateEventSource(type, "Application");
                    }
                    catch (Exception ex)
                    {
                        ToLog(ex, "Event source could not be created");
                    }
                }
            });

        }
    }
}
