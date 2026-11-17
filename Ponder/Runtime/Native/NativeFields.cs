using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Ponder.Runtime
{
    internal static class NativeFields
    {
        private static readonly Dictionary<(Type Type, string Name), FieldInfo> Fields = new();
        internal static FieldInfo Find(Type type, string name)
        {
            var key = (type, name);
            if (!Fields.TryGetValue(key, out var field))
            {
                field = AccessTools.Field(type, name);
                Fields.Add(key, field);
            }

            return field;
        }

        internal static T Read<T>(object instance, string name)
            where T : class => instance == null ? null : Find(instance.GetType(), name)?.GetValue(instance) as T;
    }
}
