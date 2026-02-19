namespace Edgar.Config
{
    public sealed class DatabaseOptions
    {
        public const string DefaultLocalHost = "mongodb://localhost:27017";
        public const string DefaultEdgarDbName = "edgar";
        public const string DefaultEdgarLoggingDbName = "edgarLogging";
        public const string DefaultEdgarMarketEquityDbName = "edgarMarketEquity";

        public string Host { get; }
        public string EdgarDbName { get; }
        public string EdgarLoggingDbName { get; }

        public string EdgarMarketEquityDbName => DefaultEdgarMarketEquityDbName;

        public DatabaseOptions(
            string? host = null,
            string? edgarDbName = null,
            string? edgarLoggingDbName = null)
        {
            Host = string.IsNullOrWhiteSpace(host) ? DefaultLocalHost : host;
            EdgarDbName = string.IsNullOrWhiteSpace(edgarDbName) ? DefaultEdgarDbName : edgarDbName;
            EdgarLoggingDbName = string.IsNullOrWhiteSpace(edgarLoggingDbName) ? DefaultEdgarLoggingDbName : edgarLoggingDbName;
        }

        public static DatabaseOptions FromEnvironment()
        {
            return new DatabaseOptions(
                host: Environment.GetEnvironmentVariable("DB_HOST"),
                edgarDbName: Environment.GetEnvironmentVariable("DB_NAME"),
                edgarLoggingDbName: Environment.GetEnvironmentVariable("DB_LOGGING_NAME")
            );
        }
    }
}

