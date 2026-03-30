using System;
using System.IO;
using System.Web.Hosting;

namespace Coepd.Web.Infrastructure
{
    public static class DiagnosticLogger
    {
        private static readonly object Sync = new object();

        public static void Info(string scope, string message)
        {
            Write("INFO", scope, message, null);
        }

        public static void Error(string scope, string message, Exception ex)
        {
            Write("ERROR", scope, message, ex);
        }

        private static void Write(string level, string scope, string message, Exception ex)
        {
            try
            {
                var root = HostingEnvironment.MapPath("~/App_Data");
                if (string.IsNullOrWhiteSpace(root)) return;

                if (!Directory.Exists(root))
                {
                    Directory.CreateDirectory(root);
                }

                var logPath = Path.Combine(root, "diagnostics.log");
                RotateIfNeeded(logPath);
                var line = string.Format(
                    "{0:u} [{1}] [{2}] {3}{4}",
                    DateTime.UtcNow,
                    level,
                    scope ?? "app",
                    message ?? string.Empty,
                    ex == null ? string.Empty : " | " + ex.GetType().Name + ": " + ex.Message);

                lock (Sync)
                {
                    File.AppendAllText(logPath, line + Environment.NewLine);
                }
            }
            catch
            {
            }
        }

        private static void RotateIfNeeded(string logPath)
        {
            try
            {
                var file = new FileInfo(logPath);
                if (!file.Exists || file.Length < 5 * 1024 * 1024)
                {
                    return;
                }

                var archivedPath = Path.Combine(file.DirectoryName ?? string.Empty, "diagnostics_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".log");
                File.Move(logPath, archivedPath);
            }
            catch
            {
            }
        }
    }
}
