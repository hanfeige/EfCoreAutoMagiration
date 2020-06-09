# The document
This is an EF automatic migration extension, you do not need to execute any EF migration commands
## Step 1
Add MigrationLog to the DbContext, and you have two ways to add it, either by DbSet or by calling AddEntityForMigrationLog from OnModelCreating
>1.` public DbSet<MigrationLog> MigrationLogs { get; set; }` 

>2.` modelBuilder.AddEntityForMigrationLog();` 
## Step 2
Create your DbContext in any way and execute the AutoMigratorDatabase method
>` 
using (var db = new TestDynamicDbContext())
 {
                db.AutoMigratorDatabase();
}
` 
