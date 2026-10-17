using System;
using System.Diagnostics;
using UnityEngine;

namespace Achieve.MaskedValues
{
    /// <summary>
    /// Draws a serialized initial value normally in Edit mode, and a sibling
    /// runtime masked field's Value in Play mode. Initialize that field in Awake.
    /// The attribute is omitted from player metadata.
    /// </summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MaskedValueFieldAttribute : PropertyAttribute
    {
        public string RuntimeFieldName { get; }

        public MaskedValueFieldAttribute(string runtimeFieldName)
        {
            RuntimeFieldName = runtimeFieldName;
        }
    }
}
