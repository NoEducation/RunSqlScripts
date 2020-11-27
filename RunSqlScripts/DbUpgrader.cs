using System;
using System.Collections.Generic;
using System.IO;
using DbUp;
using DbUp.Builder;
using DbUp.Engine;
using DbUp.Helpers;

namespace RunSqlScripts
{
    public class DbUpgrader
    {
        List<SqlScript> allExecutedScripts = new List<SqlScript>();

        public DatabaseUpgradeResult UpgradeDatabase(string connectionString, string pathToChangelog, bool runRollbacks = true)
        {
            EnsureDatabase.For.SqlDatabase(connectionString);

            DatabaseUpgradeResult upgradeResult = PerformUpgrade(connectionString, Path.Combine(pathToChangelog, "schema\\1.0.X"), 
                script => !script.Contains("_rollback"));
            if (!upgradeResult.Successful)
                return new DatabaseUpgradeResult(allExecutedScripts, upgradeResult.Successful, upgradeResult.Error);

            //upgradeResult = PerformUpgrade(connectionString, Path.Combine(pathToChangelog, "procedures"),
            //    script => !script.Contains("_rollback"));
            //if (!upgradeResult.Successful)
            //    return new DatabaseUpgradeResult(allExecutedScripts, upgradeResult.Successful, upgradeResult.Error);

            if (runRollbacks)
            {
                //upgradeResult = PerformUpgrade(connectionString, Path.Combine(pathToChangelog, "procedures"),
                //    script => script.Contains("_rollback"));
                //if (!upgradeResult.Successful)
                //    return new DatabaseUpgradeResult(allExecutedScripts, upgradeResult.Successful, upgradeResult.Error);

                upgradeResult = PerformUpgrade(connectionString, Path.Combine(pathToChangelog, "schema\\1.0.X"),
                    script => script.Contains("_rollback"));
                if (!upgradeResult.Successful)
                    return new DatabaseUpgradeResult(allExecutedScripts, upgradeResult.Successful, upgradeResult.Error);
            }

            return new DatabaseUpgradeResult(allExecutedScripts, upgradeResult.Successful, upgradeResult.Error);
        }

        private DatabaseUpgradeResult PerformUpgrade(string connectionString, string filesInPath, Func<string, bool> fileNameFilter)
        {
            UpgradeEngineBuilder builder = DeployChanges
                .To
                .SqlDatabase(connectionString)
                .WithTransaction()
                //.WithTransactionAlwaysRollback()
                .WithVariablesDisabled()
                .WithScriptsFromFileSystem(filesInPath, fileNameFilter);

            DatabaseUpgradeResult result = builder.Build().PerformUpgrade();

            allExecutedScripts.AddRange(result.Scripts);

            return result;
        }
    }
}

