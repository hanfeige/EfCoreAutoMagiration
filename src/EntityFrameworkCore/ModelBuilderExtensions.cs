using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Kings.EntityFrameworkCore.AutoMigration
{
    public static class ModelBuilderExtensions
    {
        public static void AddEntityForMigrationLog(this ModelBuilder modelBuilder, string tablePrefix = null)
        {
            if( modelBuilder.Model.FindEntityType(typeof(MigrationLog)) == null)
            {
                modelBuilder.Model.AddEntityType(typeof(MigrationLog));
            }
            if (!string.IsNullOrWhiteSpace(tablePrefix))
            {
                modelBuilder.Entity<MigrationLog>(b =>
                {
                    b.ToTable(tablePrefix + MigrationLog.TableName);

                });
            }
        }
        public static void DynamicBindingEntityTypes(this ModelBuilder modelBuilder, Type baseEntityType,Assembly assembly)
        {
            var entityTypes = GetEntityTypes(baseEntityType, assembly);
            foreach (var type in entityTypes)
            {
                if (modelBuilder.Model.FindEntityType(type) != null)
                    continue;
                modelBuilder.Model.AddEntityType(type);
            }
        }
        public static void DynamicBindingEntityTypes(this ModelBuilder modelBuilder, Assembly assembly, params Type[] baseEntityTypes)
        {
            for (var i = 0; i < baseEntityTypes.Length; i++)
            {
                modelBuilder.DynamicBindingEntityTypes(baseEntityTypes[i], assembly);
            }
        }
        public static void DynamicBindingEntityTypesForAssembly(this ModelBuilder modelBuilder, Assembly assembly, Type[] baseEntityTypes, params Assembly[] referencedAssemblies)
        {
            modelBuilder.DynamicBindingEntityTypes(assembly,baseEntityTypes);
        }
        #region private functions
        private static List<Type> GetEntityTypes(Type entityType, Assembly assembly)
        {
            return entityType.IsGenericType ?
                assembly.GetTypes()
                .Where(type => type.IsClass)
                .Where(type => !type.IsAbstract)
                .Where(type => entityType.IsGenericAssignableFrom(type)).ToList()
                :
                assembly.GetTypes()
                .Where(type => type.IsClass)
                .Where(type => !type.IsAbstract)
                .Where(type => entityType.IsAssignableFrom(type)).ToList();
        }

        private static List<Type> GetEntityConfigurationTypes(Assembly assembly)
        {
            var entityConfigurationTypes = assembly.GetTypes()
                .Where(type => type.IsClass)
                .Where(type => !type.IsAbstract)
                .Where(type => typeof(IEntityTypeConfiguration<>).IsGenericAssignableFrom(type)).ToList();
            return entityConfigurationTypes;
        }
        private static bool IsGenericAssignableFrom(this Type genericType, Type type)
        {
            if (!genericType.IsGenericType)
            {
                throw new ArgumentException("This feature only supports calls to generic types, which can use the IsAssignableFrom method.");
            }

            List<Type> allOthers = new List<Type> { type };
            if (genericType.IsInterface)
            {
                allOthers.AddRange(type.GetInterfaces());
            }
            foreach (var other in allOthers)
            {
                Type cur = other;
                while (cur != null)
                {
                    if (cur.IsGenericType)
                    {
                        cur = cur.GetGenericTypeDefinition();
                    }
                    if (cur.IsSubclassOf(genericType) || cur == genericType)
                    {
                        return true;
                    }
                    cur = cur.BaseType;
                }
            }
            return false;
        }
        #endregion
    }
}
