using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Achieve.MaskedValues.Editor
{
    // Editor-only reflection. No discovery, boxing or extra branches in gameplay.
    internal sealed class MaskedValueBinding
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public |
            BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private static readonly Dictionary<Type, Dictionary<string, FieldInfo>> fieldCache =
            new Dictionary<Type, Dictionary<string, FieldInfo>>();
        private static readonly Dictionary<Type, PropertyInfo> valueCache =
            new Dictionary<Type, PropertyInfo>();
        private static readonly Dictionary<Type, FieldInfo[]> serializedFieldCache =
            new Dictionary<Type, FieldInfo[]>();
        private static readonly MethodInfo memberwiseClone = typeof(object).GetMethod("MemberwiseClone",
            BindingFlags.Instance | BindingFlags.NonPublic);
        private readonly string[] parentPath;
        private readonly string runtimeField;
        private int runtimeIndex = -1;

        internal static MaskedValueBinding ForPath(string propertyPath)
        {
            string path = propertyPath.Replace(".Array.data[", "[");
            string leaf = path.Substring(path.LastIndexOf('.') + 1);
            int bracket = leaf.IndexOf('[');
            var binding = new MaskedValueBinding(path, bracket < 0 ? leaf : leaf.Substring(0, bracket));
            if (bracket >= 0)
                binding.runtimeIndex = int.Parse(leaf.Substring(bracket + 1, leaf.Length - bracket - 2), CultureInfo.InvariantCulture);
            return binding;
        }

        internal MaskedValueBinding(string initialPropertyPath, string runtimeFieldName)
        {
            if (string.IsNullOrWhiteSpace(runtimeFieldName) || runtimeFieldName.Contains("."))
                throw new ArgumentException("Specify the name of a sibling runtime field.");
            string[] path = initialPropertyPath.Replace(".Array.data[", "[").Split('.');
            parentPath = new string[path.Length - 1];
            Array.Copy(path, parentPath, parentPath.Length);
            runtimeField = runtimeFieldName;
        }

        internal object Read(object target)
        {
            object owner = ResolveOwner(target, null);
            FieldInfo field = GetRuntimeField(owner);
            object masked = ReadMasked(owner, field);
            return GetValueProperty(masked.GetType()).GetValue(masked);
        }

        internal Type ValueType(object target)
        {
            object owner = ResolveOwner(target, null);
            return GetValueProperty(ReadMasked(owner, GetRuntimeField(owner)).GetType()).PropertyType;
        }

        internal void Write(object target, object value) => Write(target, null, value, false);

        internal void WriteChanges(object target, object before, object value) => Write(target, before, value, true);

        private void Write(object target, object before, object value, bool changesOnly)
        {
            var parents = new List<Parent>();
            object owner = ResolveOwner(target, parents);
            FieldInfo field = GetRuntimeField(owner);
            object masked = ReadMasked(owner, field);
            PropertyInfo property = GetValueProperty(masked.GetType());
            if (value != null && !property.PropertyType.IsInstanceOfType(value))
                value = property.PropertyType.IsEnum
                    ? Enum.ToObject(property.PropertyType, value)
                    : Convert.ChangeType(value, property.PropertyType, CultureInfo.InvariantCulture);

            // Use the legitimate setter, including GuardedValue's existing-state
            // check. A corrupt value must never be re-tagged by the Inspector.
            value = MergeSerializedFields(property.GetValue(masked), value, 0, before, changesOnly);
            property.SetValue(masked, value);
            WriteMasked(owner, field, masked);
            WriteParents(owner, parents);
        }

        private object ReadMasked(object owner, FieldInfo field)
        {
            object value = field.GetValue(owner);
            if (runtimeIndex < 0) return value;
            if (!(value is IList list) || runtimeIndex >= list.Count)
                throw new InvalidOperationException("The masked collection element is unavailable.");
            return list[runtimeIndex];
        }

        private void WriteMasked(object owner, FieldInfo field, object value)
        {
            if (runtimeIndex < 0) field.SetValue(owner, value);
            else ((IList)field.GetValue(owner))[runtimeIndex] = value;
        }

        private static void WriteParents(object owner, List<Parent> parents)
        {
            for (int i = parents.Count - 1; i >= 0; i--)
            {
                Parent parent = parents[i];
                if (parent.Field != null) parent.Field.SetValue(parent.Owner, owner);
                else ((IList)parent.Owner)[parent.Index] = owner;
                owner = parent.Owner;
            }
        }

        private object ResolveOwner(object target, List<Parent> parents)
        {
            object owner = target;
            foreach (string segment in parentPath)
            {
                if (owner == null) throw new InvalidOperationException("The runtime field's parent is null.");
                int bracket = segment.IndexOf('[');
                string name = bracket < 0 ? segment : segment.Substring(0, bracket);
                FieldInfo field = FindField(owner.GetType(), name);
                parents?.Add(new Parent { Owner = owner, Field = field });
                owner = field.GetValue(owner);
                if (bracket >= 0)
                {
                    int index = int.Parse(segment.Substring(bracket + 1, segment.Length - bracket - 2),
                        CultureInfo.InvariantCulture);
                    if (!(owner is IList list) || index < 0 || index >= list.Count)
                        throw new InvalidOperationException("The runtime field's collection element is unavailable.");
                    parents?.Add(new Parent { Owner = owner, Index = index });
                    owner = list[index];
                }
            }
            if (owner == null) throw new InvalidOperationException("The runtime field's parent is null.");
            return owner;
        }

        private FieldInfo GetRuntimeField(object owner)
        {
            FieldInfo field = FindField(owner.GetType(), runtimeField);
            if (field.IsInitOnly)
                throw new InvalidOperationException("The runtime masked field must not be readonly.");
            return field;
        }

        private static FieldInfo FindField(Type type, string name)
        {
            if (!fieldCache.TryGetValue(type, out Dictionary<string, FieldInfo> fields))
                fieldCache.Add(type, fields = new Dictionary<string, FieldInfo>());
            if (fields.TryGetValue(name, out FieldInfo result)) return result;
            for (Type current = type; current != null; current = current.BaseType)
            {
                result = current.GetField(name, Fields);
                if (result != null)
                {
                    fields.Add(name, result);
                    return result;
                }
            }
            throw new InvalidOperationException("Runtime field '" + name + "' was not found on " + type.Name + ".");
        }

        private static PropertyInfo GetValueProperty(Type type)
        {
            if (valueCache.TryGetValue(type, out PropertyInfo property)) return property;
            Type definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            if (definition != typeof(MaskedValue<>) && definition != typeof(GuardedValue<>) &&
                definition != typeof(MaskedData<>) && definition != typeof(MaskedString))
                throw new InvalidOperationException("The bound field must be a Masked Values runtime type.");
            property = type.GetProperty("Value");
            valueCache.Add(type, property);
            return property;
        }

        // A serialized editing buffer cannot carry nonserialized fields. Keep
        // those fields from the live value when a custom struct/class is edited.
        // Copy containers rather than changing a codec's returned object in place.
        private static object MergeSerializedFields(object original, object edited, int depth = 0,
            object before = null, bool changesOnly = false)
        {
            if (depth > 16) throw new NotSupportedException("Inspector editing does not support cyclic or deeply nested reference graphs.");
            if (changesOnly && Equals(before, edited)) return original;
            if (original == null || edited == null || original.GetType() != edited.GetType()) return edited;
            Type type = original.GetType();
            if (type.IsPrimitive || type.IsEnum || type == typeof(string) ||
                type.Assembly.GetName().Name.StartsWith("UnityEngine", StringComparison.Ordinal)) return edited;
            if (edited is IList editedList)
            {
                IList originalList = (IList)original;
                IList result = type.IsArray
                    ? (IList)Array.CreateInstance(type.GetElementType(), editedList.Count)
                    : (IList)Activator.CreateInstance(type);
                for (int i = 0; i < editedList.Count; i++)
                {
                    object element = i < originalList.Count
                        ? MergeSerializedFields(originalList[i], editedList[i], depth + 1,
                            before is IList beforeList && i < beforeList.Count ? beforeList[i] : null, changesOnly) : editedList[i];
                    if (type.IsArray) result[i] = element;
                    else result.Add(element);
                }
                return result;
            }
            if (!serializedFieldCache.TryGetValue(type, out FieldInfo[] fields))
            {
                var selected = new List<FieldInfo>();
                for (Type current = type; current != null; current = current.BaseType)
                {
                    foreach (FieldInfo field in current.GetFields(Fields))
                    {
                        if (field.IsInitOnly || field.FieldType.IsPointer || field.IsDefined(typeof(NonSerializedAttribute), true)) continue;
                        bool serialized = field.IsPublic;
                        foreach (CustomAttributeData attribute in field.GetCustomAttributesData())
                            if (attribute.AttributeType.FullName == "UnityEngine.SerializeField") serialized = true;
                        if (serialized) selected.Add(field);
                    }
                }
                fields = selected.ToArray();
                serializedFieldCache.Add(type, fields);
            }
            object merged = memberwiseClone.Invoke(original, null);
            foreach (FieldInfo field in fields)
                field.SetValue(merged, MergeSerializedFields(field.GetValue(original), field.GetValue(edited), depth + 1,
                    before != null && before.GetType() == type ? field.GetValue(before) : null, changesOnly));
            return merged;
        }

        private struct Parent
        {
            internal object Owner;
            internal FieldInfo Field;
            internal int Index;
        }
    }
}
