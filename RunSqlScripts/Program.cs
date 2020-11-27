using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace RunSqlScripts
{
    public class Program
    {
        private static string _connectionName = "Cudev2Connection"; 
        private static IEnumerable<string> _targetPaths = new List<string>();
        private static IEnumerable<string> _scriptsToNotRun = new List<string>();
        static IConfiguration Configuration { get; set; }

        static int Main(string[] args)
        {
            var (connectionString, pathToChangelog) = GetConnectionStringAndPath();
            var container = BuildContainer();

            var context = container.Resolve<ScriptsContext>();

            context.Database.CanConnect();

            using (var dbContextTransaction = context.Database.BeginTransaction())
            {
                try
                {
                    Console.WriteLine("DB Update connectionString: " + connectionString);

                    foreach (var path in _targetPaths)
                    {
                        var files = Directory.GetFiles(Path.Combine(pathToChangelog, path)).FilterFiles(_scriptsToNotRun);
                        foreach (var file in files)
                        {
                            var sql = System.IO.File.ReadAllText(file);
                            Console.WriteLine($"Executing: {file}");
                            context.Database.ExecuteSqlRaw(string.Format(sql, 0));
  
                        }
                    }
                }
                catch (Exception ex)
                {
                    FastHandleError(ex);
                }
                finally
                {
                    dbContextTransaction.Rollback();
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Db upgrade success!");
            Console.ResetColor();
            return 0;
        }

        private static void FastHandleError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex);
            Console.ResetColor();
#if DEBUG
            Console.ReadLine();
#endif
            using (var errWriter = Console.Error)
            {
                errWriter.WriteLine(ex);
                errWriter.Flush();
            }

            Environment.Exit(-1);
        }

        private static (string,string) GetConnectionStringAndPath()
        {
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .Build();

            _targetPaths = Configuration.GetSection("ScriptsToRun")
                .AsEnumerable()
                .Select(x => x.Value)
                .Where(x => x != null)
                .ToList();

            _scriptsToNotRun = Configuration.GetSection("ScriptsToNotRun")
                .AsEnumerable()
                .Select(x => x.Value)
                .Where(x => x != null)
                .ToList();

            return (Configuration.GetConnectionString(_connectionName),
                Configuration.GetSection("PathToProject").Value);
        }

        private static IContainer BuildContainer()
        {
            var builder = new ContainerBuilder();
            var services = new ServiceCollection();

            builder.RegisterInstance(Configuration).As<IConfiguration>();
            builder.Populate(services);
            Extension.RegisterContext<ScriptsContext>(builder, _connectionName);
            return builder.Build();
        }

       
    }
}
