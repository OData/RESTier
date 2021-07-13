using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Restier.EntityFrameworkCore
{

    /// <summary>
    /// Provides extensions to work with EntityFramework models.
    /// </summary>
    public static class EFCoreDbContextExtensions
    {

        /// <summary>
        /// Does the specified entity type have a DbSet mapping in the model
        /// </summary>
        /// <param name="context"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsDbSetMapped(this DbContext context, Type type)
        {
            Ensure.NotNull(context, nameof(context));
            Ensure.NotNull(type, nameof(type));

            var contextType = context.GetType();

            var genericProps = contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(c => c.PropertyType.IsGenericType);
            return genericProps.Any(c => c.PropertyType.GenericTypeArguments.Contains(type));

        }

    }

}
