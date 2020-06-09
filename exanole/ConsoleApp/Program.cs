using Kings.EntityFrameworkCore.AutoMigration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection;

namespace ConsoleApp
{
    public interface IEntity
    {
    }
    public class Blog: IEntity
    {
        public int BlogId { get; set; }
        public string Url { get; set; }
    }
    public class TestDbContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(LocalDb)\\MSSQLLocalDB;Database=testDb;Trusted_Connection=True;MultipleActiveResultSets=true");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddEntityForMigrationLog();//You can also set it directly using the DbSet property
        }
    }

    public class TestDynamicDbContext : DbContext
    {

        public DbSet<MigrationLog> MigrationLogs { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer("Server=(LocalDb)\\MSSQLLocalDB;Database=testDynamicDb;Trusted_Connection=True;MultipleActiveResultSets=true");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var entityStr = @"
                            using System;
                            using ConsoleApp;

                            public class Student : IEntity
                            {
                                public int Id { get; set; }

                                public string Name { get; set; }

                                public short Age { get; set; }
                            }
                            ";
            var assembly = CompileHelper.Compile(entityStr, CompileHelper.GetCompileAssemblies(typeof(IEntity)));
            Program.DyAssembly = assembly;
            modelBuilder.DynamicBindingEntityTypes(assembly, typeof(IEntity));
        }
    }

    class Program
    {
        public static Assembly DyAssembly;
        static int Main(string[] args)
        {
            #region TestDbContext
            using (var db = new TestDbContext())
            {
                db.AutoMigratorDatabase();

                var blog = new Blog()
                {
                    Url = $"http://www.{new Random().Next(0, 99999999)}.com"
                };
                db.Blogs.Add(blog);
                db.SaveChanges();
            }
            #endregion

            #region TestDynamicDbContext
            using (var db = new TestDynamicDbContext())
            {
                db.AutoMigratorDatabase();//See OnModelCreating for DbContext
            }
            #endregion


            using (var db = new TestDbContext())
            {
                var blogList = db.Blogs.ToList();
                blogList.ForEach(blog =>
                {
                    Console.WriteLine($"blog id:{blog.BlogId},url:{blog.Url}");
                });
            }
            using (var db = new TestDynamicDbContext())
            {
                var entityType = DyAssembly.GetTypes().FirstOrDefault(p => p.Name == "Student");
                var studentQuery = db.Get(entityType);
                var count = studentQuery.Count();//You can open up the database and add some data
                Console.WriteLine($"students count:{count}");
            }
            return 0;
        }
    }
}

