using System;

namespace CargoReport.Web.Helpers.JqGridHelpers.Utils
{
    public static class ReflectionHelper
    {
        public static Type FindFieldType(this Type type, string fieldName, string parent = "", int dumpLevel = 3)
        {
            foreach (var property in type.GetProperties())
            {
                if (parent + property.Name == fieldName)
                    return property.PropertyType;

                if (parent.Split('.').Length > dumpLevel)
                    continue;

                if (!IsNestedProperty(property.PropertyType))
                    continue;

                var result = FindFieldType(property.PropertyType, fieldName, property.Name + ".");
                if (result != null)
                    return result;
            }

            return null;
        }

        private static bool IsNestedProperty(Type type)
        {
            if (type.Assembly.FullName.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase)) return false;
            return
                (type.IsClass || type.IsInterface) &&
                !type.IsValueType && type.Namespace != null &&
                !string.IsNullOrEmpty(type.Namespace) &&
                !type.Namespace.StartsWith("System.", StringComparison.OrdinalIgnoreCase);
        }
    }
}