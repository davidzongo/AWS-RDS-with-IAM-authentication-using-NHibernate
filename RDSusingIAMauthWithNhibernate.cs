using System;
using System.IO;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Npgsql;
using System.Collections.Generic;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
;
using NHibernate.Tool.hbm2ddl;
using Amazon.RDS.Util;

namespace Aws.RDS.IAM.PostGreSql.Authentication.With.Nhibernate.Demo
{
	public class RDSusingIAMauthWithNhibernate : IHealthCheck
	{
		private readonly ILogger Logger;

		public RDSusingIAMauthWithNhibernate(ILogger logger)
		{
			Logger = logger;
		}


        public static void CheckPostgreSQLDatabase(ILogger Logger)
        {
            try
            {
                var configurationFileName =
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Settings.Default.ConfigurationFile);
                NHibernate.Cfg.Configuration cfg = new NHibernate.Cfg.Configuration().Configure(configurationFileName);

                var connectionString = cfg.GetProperty(NHibernate.Cfg.Environment.ConnectionString);

                var dialect = cfg.GetProperty(NHibernate.Cfg.Environment.Dialect);
                GlobalConstant.DB_PostgreSQL = dialect.IndexOf("PostgreSQL") >= 0;

                if (GlobalConstant.DB_PostgreSQL)
                {
                    var ctalogConnectionString = new NpgsqlConnectionStringBuilder(connectionString);
                    var configuration = Fluently.Configure()
                                       .Database(PostgreSQLConfiguration.Standard
                                           .ConnectionString(c => c
                                               .Host(ctalogConnectionString.Host)
                                               .Port(ctalogConnectionString.Port)
                                               .Database(ctalogConnectionString.Database)
                                               .Username(ctalogConnectionString.Username)
                                               .Password(GenerateAwsIamAuthToken(ctalogConnectionString.Host, ctalogConnectionString.Port, ctalogConnectionString.Database, ctalogConnectionString.Username)))
                                       )
                    .Mappings(m => m.FluentMappings.AddFromAssemblyOf<VersionInfo>())
                    .ExposeConfiguration(config => new SchemaExport(config).Create(false, true))
                                   .BuildSessionFactory();

                    using (var session = configuration.OpenSession())
                    {
                        // Query all objects
                        Logger.Info("PostgreSQL Database is ready....");
                        var completeList = session.CreateCriteria<Object>().List();

                        Console.ReadLine();

                    }
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        private static string GenerateAwsIamAuthToken(string host, int port, string database, string username)
        {
            try
            {
                if (host.EndsWith("rds.amazonaws.com"))
                {
                    return Amazon.RDS.Util.RDSAuthTokenGenerator.GenerateAuthToken(host, port, username);
                }
                else
                {
                    return $"{username}";
                }
            }
            catch
            {

                throw;
            }

        }

    }
}
