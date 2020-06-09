using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kings.EntityFrameworkCore.AutoMigration
{
    [Table(MigrationLog.TableName)]
    public class MigrationLog
    {
        public const string TableName = "MigrationLogs";
        [Key]
        public int Id { get; set; }

        public string SnapshotDefine { get; set; }

        public DateTime MigrationTime { get; set; }
    }
}
