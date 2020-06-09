using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Kings.EntityFrameworkCore.AutoMigration
{
    public static class DbContextMigrationExtensions
    {
        private const string _dbContentModelSnapshot = "KingsDbContextModelSnapshot";

        /// <summary>
        /// Automatically migrate the database
        /// Note: The DbContext to be migrated needs to inherit from IKingsDbContext or manually add entity MigrationLog in any way.
        /// </summary>
        /// <param name="dbContext"></param>
        public static void AutoMigratorDatabase(this DbContext dbContext)
        {
            Console.WriteLine($"database begin to magration ......");
            IModel lastModel = null;
            try
            {
                var relationDatabase = dbContext.GetService<IRelationalDatabaseCreator>();
                if (!relationDatabase.Exists())
                {
                    relationDatabase.Create();
                }
                else
                {
                    var lastMigration = dbContext.Set<MigrationLog>()
                        .OrderByDescending(e => e.Id)
                        .FirstOrDefault();
                    lastModel = lastMigration == null ? null : (CreateModelSnapshot(lastMigration.SnapshotDefine, typeof(DbContextMigrationExtensions).Namespace
                    , _dbContentModelSnapshot, dbContext.GetType())?.Model);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            var modelDiffer = dbContext.Database.GetService<IMigrationsModelDiffer>();
            if (modelDiffer.HasDifferences(lastModel, dbContext.Model))
            {
                var upOperations = modelDiffer.GetDifferences(lastModel, dbContext.Model);
                Migrationing(upOperations, dbContext);

                var serviceProvider = new DesignTimeServicesBuilder(dbContext.GetType().Assembly,
                    Assembly.GetEntryAssembly(),
                    new OperationReporter(new OperationReportHandler()), new string[0])
                     .Build(dbContext);
                var migrationsCodeGenerator = serviceProvider.GetService(typeof(IMigrationsCodeGenerator)) as IMigrationsCodeGenerator;
                var snapshotCode = migrationsCodeGenerator.GenerateSnapshot(typeof(DbContextMigrationExtensions).Namespace, dbContext.GetType(), _dbContentModelSnapshot, dbContext.Model);
                dbContext.Set<MigrationLog>().Add(new MigrationLog()
                {
                    SnapshotDefine = snapshotCode,
                    MigrationTime = DateTime.Now
                });
                dbContext.SaveChanges();
            }
            Console.WriteLine($"database magration end......");
        }

        #region private functions for Migrator

        #region sqlextensions
        private static int ExecuteListSqlCommand(this DbContext dbContext, List<string> sqlList)
        {
            int retunInt = 0;
            try
            {
                sqlList.ForEach(cmd => retunInt += dbContext.Database.ExecuteSqlRaw(cmd));
            }
            catch (DbException ex)
            {
                dbContext.Database.RollbackTransaction();
            }
            return retunInt;
        }
        private static DataTable SqlQuery(this DbContext efcontent, string sql, params object[] commandParameters)
        {
            var dt = new DataTable();
            using (var connection = efcontent.Database.GetDbConnection())
            {
                using (var cmd = connection.CreateCommand())
                {
                    efcontent.Database.OpenConnection();
                    cmd.CommandText = sql;

                    if (commandParameters != null && commandParameters.Length > 0)
                        cmd.Parameters.AddRange(commandParameters);
                    using (var reader = cmd.ExecuteReader())
                    {
                        dt.Load(reader);
                    }
                }
            }
            return dt;
        }
        #endregion

        private static ModelSnapshot CreateModelSnapshot(string codedefine, string nameSpace, string className, Type dbType)
        {
            var references = dbType.Assembly
                .GetReferencedAssemblies()
                .Select(e => MetadataReference.CreateFromFile(Assembly.Load(e).Location))
                .Union(new MetadataReference[]
                {
                    MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                    MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(ModelSnapshot).Assembly.Location),
                    MetadataReference.CreateFromFile(dbType.Assembly.Location)
                });
            var compilation = CSharpCompilation.Create(nameSpace)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(references)
                .AddSyntaxTrees(SyntaxFactory.ParseSyntaxTree(codedefine));
           
            using (var stream = new MemoryStream())
            {
                var compileResult = compilation.Emit(stream);
                return compileResult.Success
                    ? Assembly.Load(stream.GetBuffer()).CreateInstance(nameSpace + "." + className) as ModelSnapshot
                    : null;
            }
        }
        private static void Migrationing(IReadOnlyList<MigrationOperation> upOperations, DbContext _dbContext)
        {
            List<string> sqlChangeColumNameList = new List<string>();
            List<MigrationOperation> list = new List<MigrationOperation>();
            foreach (var upOperation in upOperations)
            {
                if (upOperation is RenameColumnOperation)
                {

                    sqlChangeColumNameList.Add(RenameColumnOperationToSql(upOperation as RenameColumnOperation, _dbContext));
                }
                else
                {
                    list.Add(upOperation);
                }
            }
            int columChangeCount = sqlChangeColumNameList.Count > 0 ? _dbContext.ExecuteListSqlCommand(sqlChangeColumNameList) : 0;
            if (list.Count > 0)
            {
                var sqlList = _dbContext.Database.GetService<IMigrationsSqlGenerator>()
                    .Generate(list, _dbContext.Model)
                    .Select(p => p.CommandText).ToList();
                int changeCount = _dbContext.ExecuteListSqlCommand(sqlList);
            }
        }
        private static string RenameColumnOperationToSql(RenameColumnOperation renameColumnOperation, DbContext _dbContext)
        {
            string column_type = string.Empty;
            string sql = "select column_type from information_schema.columns where table_name='" + (renameColumnOperation as RenameColumnOperation).Table + "'  and column_name='" + (renameColumnOperation as RenameColumnOperation).Name + "'";
            var dataTable = _dbContext.SqlQuery(sql);
            if (dataTable != null && dataTable.Rows.Count > 0)
            {
                column_type = dataTable.Rows[0].ItemArray[0].ToString();
            }
            return "alter table " + (renameColumnOperation as RenameColumnOperation).Table + " change  column " + (renameColumnOperation as RenameColumnOperation).Name + " " + (renameColumnOperation as RenameColumnOperation).NewName + " " + column_type + " ;";

        }

        #endregion
    }
}
