﻿using Baseline;

namespace Jasper.Persistence.SqlServer;

public static class SqlServerConfigurationExtensions
{
    /// <summary>
    ///     Register sql server backed message persistence to a known connection string
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="connectionString"></param>
    /// <param name="schema"></param>
    public static void PersistMessagesWithSqlServer(this JasperOptions options, string connectionString,
        string? schema = null)
    {
        options.Include<SqlServerBackedPersistence>(x =>
        {
            x.Settings.ConnectionString = connectionString;

            if (schema.IsNotEmpty())
            {
                x.Settings.SchemaName = schema;
            }
        });
    }
}
