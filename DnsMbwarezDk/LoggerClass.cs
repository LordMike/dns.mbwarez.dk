using System;
using System.Configuration;
using System.Reflection;
using SharpRaven;

namespace DnsMbwarezDk
{
    public class LoggerClass
    {
        public static LoggerClass Instance => new LoggerClass();

        private readonly RavenClient _ravenClient;

        private LoggerClass()
        {
            string sentryDsn = ConfigurationManager.AppSettings["sentry_dsn"];
            if (sentryDsn != null)
            {
                _ravenClient = new RavenClient(sentryDsn);
                _ravenClient.Logger = Assembly.GetExecutingAssembly().GetName().Name;
            }
        }

        public void Log(Exception ex)
        {
            _ravenClient?.CaptureException(ex);
        }
    }
}