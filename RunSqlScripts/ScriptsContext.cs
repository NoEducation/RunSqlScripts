using System;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace RunSqlScripts
{
    public class ScriptsContext : DbContext
    {
        public IDbConnection Connection { get; set; }
        
        public ScriptsContext(DbContextOptions<ScriptsContext> options)
                : base(options)
        {
            this.Connection = this.Database.GetDbConnection();
        }
    }
}
