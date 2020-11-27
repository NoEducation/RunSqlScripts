using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace RunSqlScripts
{
    public static class Extension
    {
        public static void RegisterContext<TContext>(ContainerBuilder builder, string connectionStringName)
            where TContext : DbContext
        {
            builder.Register(componentContext =>
                {
                    var serviceProvider = componentContext.Resolve<IServiceProvider>();
                    var configuration = componentContext.Resolve<IConfiguration>();
                    var dbContextOptions = new DbContextOptions<TContext>(new Dictionary<Type, IDbContextOptionsExtension>());
                    var optionsBuilder = new DbContextOptionsBuilder<TContext>(dbContextOptions)
                        .UseApplicationServiceProvider(serviceProvider)
                        .UseSqlServer(configuration.GetConnectionString(connectionStringName),
                            serverOptions => serverOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null));

                    return optionsBuilder.Options;
                }).As<DbContextOptions<TContext>>()
                .InstancePerLifetimeScope();

            builder.Register(context => context.Resolve<DbContextOptions<TContext>>())
                .As<DbContextOptions>()
                .InstancePerLifetimeScope();

            builder.RegisterType<TContext>()
                .AsSelf()
                .InstancePerLifetimeScope();
        }

        public static IEnumerable<string> FilterFiles(this IEnumerable<string> source, IEnumerable<string> scriptsToNotRun)
        {
            var result =  source.Where(file => !file.Contains("_rollback") && file.Contains(".sql")
                                                        && !scriptsToNotRun.Any(x => file.Contains(x))).ToList();

            return result;
        }
    }
}
