using System.Configuration;

namespace Coepd.Web.Infrastructure
{
    public static class StorageMode
    {
        public static bool UseRuntimeStore()
        {
            var forceRuntimeStore = ConfigurationManager.AppSettings["FORCE_RUNTIME_STORE"];
            if (!string.IsNullOrWhiteSpace(forceRuntimeStore) && forceRuntimeStore.Trim().ToLowerInvariant() == "true")
            {
                return true;
            }

            var connection = ConfigurationManager.ConnectionStrings["CoepdDb"];
            var value = connection == null ? string.Empty : connection.ConnectionString ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value)) return true;

            var normalized = value.ToLowerInvariant();
            return normalized.Contains("sql_server_name_from_somee")
                || normalized.Contains("db_name_from_somee")
                || normalized.Contains("db_user_from_somee")
                || normalized.Contains("db_password_from_somee")
                || normalized.Contains("somee_sql_host")
                || normalized.Contains("somee_database_name")
                || normalized.Contains("somee_database_user")
                || normalized.Contains("somee_database_password");
        }
    }
}
