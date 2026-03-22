// This file is part of the Prowl Game Engine
// Licensed under the MIT License. See the LICENSE file in the project root for details.

using System.Reflection;

using Prowl.Runtime;

namespace Prowl.Editor.Assets;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public class FormerlySerializedAsAttribute : Attribute
{
    public string[] OldNames { get; }

    public FormerlySerializedAsAttribute(params string[] oldNames)
    {
        OldNames = oldNames;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public class FormerlySerializedAsTypeAttribute : Attribute
{
    public string[] OldTypeNames { get; }

    public FormerlySerializedAsTypeAttribute(params string[] oldTypeNames)
    {
        OldTypeNames = oldTypeNames;
    }
}

public static class TypeMigrationHelper
{
    private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> _fieldMigrationCache = new();
    private static readonly Dictionary<string, Type> _typeMigrationCache = new();

    public static FieldInfo? FindFieldByFormerName(Type type, string oldFieldName)
    {
        if (!_fieldMigrationCache.TryGetValue(type, out var fieldMap))
        {
            fieldMap = BuildFieldMigrationMap(type);
            _fieldMigrationCache[type] = fieldMap;
        }

        return fieldMap.TryGetValue(oldFieldName, out var field) ? field : null;
    }

    private static Dictionary<string, FieldInfo> BuildFieldMigrationMap(Type type)
    {
        var map = new Dictionary<string, FieldInfo>(StringComparer.OrdinalIgnoreCase);

        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var field in fields)
        {
            map[field.Name] = field;

            var formerlySerializedAs = field.GetCustomAttribute<FormerlySerializedAsAttribute>();
            if (formerlySerializedAs != null)
            {
                foreach (var oldName in formerlySerializedAs.OldNames)
                {
                    if (!map.ContainsKey(oldName))
                        map[oldName] = field;
                }
            }
        }

        return map;
    }

    public static Type? FindTypeByFormerName(string typeName)
    {
        if (_typeMigrationCache.TryGetValue(typeName, out var cachedType))
            return cachedType;

        var directType = AssemblyReplacer.FindTypeInExternalAssemblies(typeName);
        if (directType != null)
        {
            _typeMigrationCache[typeName] = directType;
            return directType;
        }

        foreach (var assembly in AssemblyManager.ExternalAssemblies)
        {
            foreach (var type in GetLoadableTypes(assembly))
            {
                if (type == null) continue;

                var formerlySerializedAsType = type.GetCustomAttribute<FormerlySerializedAsTypeAttribute>();
                if (formerlySerializedAsType != null)
                {
                    foreach (var oldName in formerlySerializedAsType.OldTypeNames)
                    {
                        if (oldName.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                        {
                            _typeMigrationCache[typeName] = type;
                            return type;
                        }
                    }
                }
            }
        }

        return null;
    }

    public static bool TryConvertValue(object? oldValue, Type newType, out object? newValue)
    {
        newValue = null;

        if (oldValue == null)
        {
            newValue = newType.IsValueType ? Activator.CreateInstance(newType) : null;
            return true;
        }

        var oldType = oldValue.GetType();

        if (newType.IsAssignableFrom(oldType))
        {
            newValue = oldValue;
            return true;
        }

        if (newType.IsEnum && oldValue is int intVal)
        {
            newValue = Enum.ToObject(newType, intVal);
            return true;
        }

        if (oldType.IsEnum && newType == typeof(int))
        {
            newValue = Convert.ToInt32(oldValue);
            return true;
        }

        if (IsNumericType(oldType) && IsNumericType(newType))
        {
            try
            {
                newValue = Convert.ChangeType(oldValue, newType);
                return true;
            }
            catch
            {
                newValue = GetDefaultValue(newType);
                return false;
            }
        }

        if (newType == typeof(string))
        {
            newValue = oldValue.ToString();
            return true;
        }

        if (oldType == typeof(string) && newType != typeof(string))
        {
            try
            {
                var parseMethod = newType.GetMethod("Parse", new[] { typeof(string) });
                if (parseMethod != null)
                {
                    newValue = parseMethod.Invoke(null, new[] { oldValue });
                    return true;
                }
            }
            catch { }
        }

        newValue = GetDefaultValue(newType);
        return false;
    }

    private static bool IsNumericType(Type type)
    {
        return type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(ushort) ||
               type == typeof(int) ||
               type == typeof(uint) ||
               type == typeof(long) ||
               type == typeof(ulong) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(decimal);
    }

    private static object? GetDefaultValue(Type type)
    {
        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private static IEnumerable<Type?> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null);
        }
    }

    public static void ClearCache()
    {
        _fieldMigrationCache.Clear();
        _typeMigrationCache.Clear();
    }
}
