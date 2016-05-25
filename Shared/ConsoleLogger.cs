using System;
using System.Configuration;
using System.Reflection;
using SharpRaven;
using SharpRaven.Data;

namespace Shared
{
    public enum ConsoleLoggerErrorLevel
    {
        Fatal,
        Error,
        Warning,
        Info,
        Debug,
    }

    public class ConsoleLogger
    {
        private RavenClient _ravenClient;

        public string CaptureException(Exception exception, ConsoleLoggerErrorLevel level = ConsoleLoggerErrorLevel.Error)
        {
            return _ravenClient.CaptureException(exception, level: (ErrorLevel)level);
        }

        public string CaptureMessage(string message, ConsoleLoggerErrorLevel level = ConsoleLoggerErrorLevel.Info)
        {
            return _ravenClient.CaptureMessage(message, (ErrorLevel)level);
        }

        public static ConsoleLogger ApplySentry()
        {
            ConsoleLogger res = new ConsoleLogger();
            string ravenDsn = ConfigurationManager.AppSettings["sentry_dsn"];
            if (ravenDsn != null)
            {
                res._ravenClient = new RavenClient(ravenDsn);
                res._ravenClient.Logger = Assembly.GetExecutingAssembly().GetName().Name;
            }

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Exception exception = eventArgs.ExceptionObject as Exception;
                if (exception == null)
                    return;

                res._ravenClient?.CaptureException(exception);
            };

            return res;
        }
    }
}