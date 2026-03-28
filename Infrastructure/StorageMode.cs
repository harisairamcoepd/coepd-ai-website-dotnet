using System.Configuration;

namespace Coepd.Web.Infrastructure
{
    public static class StorageMode
    {
        public static bool UseRuntimeStore()
        {
            var connection = ConfigurationManager.ConnectionStrings["CoepdDb"];
            var value = connection == null ? string.Empty : connection.ConnectionString ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value)) return true;

            var normalized = value.ToLowerInvariant();
            return normalized.Contains("sql_server_name_from_somee")
                || normalized.Contains("db_name_from_somee")
                || normalized.Contains("db_user_from_somee")
                || normalized.Contains("db_password_from_somee");
        }
    }
}
