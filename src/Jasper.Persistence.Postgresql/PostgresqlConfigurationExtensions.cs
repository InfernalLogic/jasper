﻿using Baseline;

namespace Jasper.Persistence.Postgresql;

public static class PostgresqlConfigurationExtensions
{
    /// <summary>
    ///     Register sql server backed message persistence to a known connection string
    /// </summary>
    /// <param name="options"></param>
    /// <param name="connectionString"></param>
    /// <param name="schema"></param>
    public static void PersistMessagesWithPostgresql(this JasperOptions options, string connectionString,
        string? schema = null)
    {
        options.Include<PostgresqlBackedPersistence>(o =>
        {
            o.Settings.ConnectionString = connectionString;
            if (schema.IsNotEmpty())
            {
                o.Settings.SchemaName = schema;
            }
        });
    }
}
