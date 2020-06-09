using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace Kings.EntityFrameworkCore.AutoMigration
{
    public static class DbContextExtensions
    {
        public static IQueryable<object> Get(this DbContext context, Type t)
        {
            return (IQueryable< object>)context.GetType().GetMethod("Set")?.MakeGenericMethod(t).Invoke(context, null);
        }
    }
}
