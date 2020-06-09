* Step 1: Add the definition of the migrated entity in DbContext
    * There are two ways to add it, or in any way you like
    * Public DbSet MigrationLogs { get; The set; }
    * or
    * ModelBuilder.AddEntityForMigrationLog()
* Step 2: Create your DbContext in any way and execute the AutoMigratorDatabase method
    * Using(var DB = new TestDynamicDbContext())
    *{
        * Db.AutoMigratorDatabase();
    *}
___________________________________________________________________

*第一步：在DbContext添加迁移实体的定义
    *提供了如下二种方式添加，或用你自己喜欢的方式
    *public DbSet<MigrationLog> MigrationLogs { get; set; }
    *或者
    *modelBuilder.AddEntityForMigrationLog()

*第二步：以任意方式创建你的DbContext，并执行AutoMigratorDatabase方法
    *using (var db = new TestDynamicDbContext())
    *{
        *db.AutoMigratorDatabase();
    *}
