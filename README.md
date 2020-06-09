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
