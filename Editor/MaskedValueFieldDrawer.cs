using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Achieve.MaskedValues.Editor
{
    [CustomPropertyDrawer(typeof(MaskedValueFieldAttribute))]
    internal sealed class MaskedValueFieldDrawer : PropertyDrawer
    {
        [ThreadStatic] private static int nativeDrawing;
        [ThreadStatic] private static int toolkitDrawing;
        private readonly Dictionary<string, string> faults = new Dictionary<string, string>();
        private readonly Dictionary<string, MaskedValueBinding> bindings = new Dictionary<string, MaskedValueBinding>();
        private readonly Dictionary<string, Preview> previews = new Dictionary<string, Preview>();
        private const float ErrorHeight = 62f;

        public MaskedValueFieldDrawer() => MaskedValueInspectorRepaint.Register(this);

        internal void ClearPreviews()
        {
            foreach (Preview preview in previews.Values) preview.Dispose();
            previews.Clear();
            faults.Clear();
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var view = new IMGUIContainer(() =>
            {
                if (property.serializedObject.targetObject == null) return;
                property.serializedObject.UpdateIfRequiredOrScript();
                var label = new GUIContent(property.displayName, property.tooltip);
                float height = GetPropertyHeight(property, label);
                Rect rect = EditorGUILayout.GetControlRect(false, height);
                EditorGUI.BeginChangeCheck();
                bool changed;
                toolkitDrawing++;
                try { OnGUI(rect, property, label); }
                finally
                {
                    toolkitDrawing--;
                    changed = EditorGUI.EndChangeCheck();
                }
                if (changed && !IsRuntime(property)) property.serializedObject.ApplyModifiedProperties();
            });
            // Only attached, visible Inspectors are scheduled by UI Toolkit.
            view.schedule.Execute(() =>
            {
                if (EditorApplication.isPlaying && view.visible) view.MarkDirtyRepaint();
            }).Every(200);
            return view;
        }

#pragma warning disable CS0672 // Required for live IMGUI values on older supported Editors.
        public override bool CanCacheInspectorGUI(SerializedProperty property) => false;
#pragma warning restore CS0672

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (nativeDrawing != 0 || !IsRuntime(property)) return NativeHeight(property, label);
            if (property.serializedObject.isEditingMultipleObjects || faults.ContainsKey(Key(property))) return ErrorHeight;
            try
            {
                return NativeHeight(CreatePreview(property).Property, label);
            }
            catch (Exception exception)
            {
                RememberFault(property, exception);
                return ErrorHeight;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (nativeDrawing != 0 || !IsRuntime(property))
            {
                DrawNative(position, property, label);
                return;
            }
            if (property.serializedObject.isEditingMultipleObjects)
            {
                EditorGUI.HelpBox(position, "Select one object to inspect runtime masked values. Initial values support multi-object editing in Edit mode.", MessageType.Info);
                return;
            }
            string key = Key(property);
            if (faults.TryGetValue(key, out string message))
            {
                DrawFault(position, key, message);
                return;
            }
            if (toolkitDrawing == 0) MaskedValueInspectorRepaint.Request();
            try
            {
                Preview preview = CreatePreview(property);
                var runtimeLabel = new GUIContent(label.text + " (Runtime)",
                    "Edits update the masked Value and are discarded when Play mode ends. Initial scene/prefab values are unchanged.");
                bool previousChanged = GUI.changed;
                EditorGUI.BeginChangeCheck();
                bool edited;
                try { DrawNative(position, preview.Property, runtimeLabel); }
                finally
                {
                    edited = EditorGUI.EndChangeCheck();
                    // Leave serialized authoring values clean after a runtime edit.
                    GUI.changed = previousChanged;
                }
                property.isExpanded = preview.Property.isExpanded;
                if (edited)
                {
                    Binding(property).WriteChanges(property.serializedObject.targetObject, preview.OriginalValue, preview.ReadEditedValue());
                    preview.Reset(Binding(property).Read(property.serializedObject.targetObject));
                }
            }
            catch (ExitGUIException) { throw; }
            catch (Exception exception)
            {
                RememberFault(property, exception);
                DrawFault(position, key, faults[key]);
            }
        }

        private Preview CreatePreview(SerializedProperty property)
        {
            if (property.isArray || property.isFixedBuffer || property.propertyType == SerializedPropertyType.ManagedReference)
                throw new NotSupportedException("Bind a single serialized value. Array/list roots, fixed buffers and SerializeReference are not supported.");
            if (fieldInfo != null && fieldInfo.FieldType != Binding(property).ValueType(property.serializedObject.targetObject))
                throw new InvalidOperationException("The initial field and the masked Value must have the same type.");
            string key = Key(property);
            MaskedValueBinding binding = Binding(property);
            UnityEngine.Object target = property.serializedObject.targetObject;
            if (!previews.TryGetValue(key, out Preview preview))
            {
                preview = new Preview(binding.ValueType(target), binding.Read(target));
                previews.Add(key, preview);
            }
            else
            {
                // Native popups may finish between Inspector GUI events. Keep
                // their editing buffer alive and commit pending edits first.
                if (preview.HasPendingEdits) binding.WriteChanges(target, preview.OriginalValue, preview.ReadEditedValue());
                preview.Reset(binding.Read(target));
            }
            preview.Property.isExpanded = property.isExpanded;
            return preview;
        }

        private MaskedValueBinding Binding(SerializedProperty property)
        {
            string path = property.propertyPath;
            if (!bindings.TryGetValue(path, out MaskedValueBinding binding))
            {
                binding = new MaskedValueBinding(path, ((MaskedValueFieldAttribute)attribute).RuntimeFieldName);
                bindings.Add(path, binding);
            }
            return binding;
        }

        private void RememberFault(SerializedProperty property, Exception exception)
        {
            while (exception is TargetInvocationException invocation && invocation.InnerException != null)
                exception = invocation.InnerException;
            faults[Key(property)] = exception.Message;
        }

        private void DrawFault(Rect position, string key, string message)
        {
            Rect button = new Rect(position.x, position.yMax - EditorGUIUtility.singleLineHeight,
                position.width, EditorGUIUtility.singleLineHeight);
            position.height = Mathf.Max(0, position.height - button.height - 2);
            EditorGUI.HelpBox(position, message, MessageType.Error);
            if (GUI.Button(button, "Retry read")) faults.Remove(key);
        }

        private static string Key(SerializedProperty property) =>
            property.serializedObject.targetObject.GetHashCode() + ":" + property.propertyPath;

        private static bool IsRuntime(SerializedProperty property) =>
            EditorApplication.isPlaying && Application.IsPlaying(property.serializedObject.targetObject);

        private static float NativeHeight(SerializedProperty property, GUIContent label)
        {
            nativeDrawing++;
            try { return EditorGUI.GetPropertyHeight(property, label, true); }
            finally { nativeDrawing--; }
        }

        private static void DrawNative(Rect position, SerializedProperty property, GUIContent label)
        {
            nativeDrawing++;
            try { EditorGUI.PropertyField(position, property, label, true); }
            finally { nativeDrawing--; }
        }

        // Native controls and their deferred popup callbacks edit a hidden
        // scratch object. They can never apply data to the component or asset.
        internal sealed class Preview : IDisposable
        {
            private readonly MaskedValuePreviewHost host;
            private readonly SerializedObject serializedObject;
            private readonly FieldInfo valueField;
            // contentHash is uint in Unity 2022 and Hash128 in newer Editors.
            private object baseline;
            internal SerializedProperty Property { get; private set; }
            internal object OriginalValue { get; private set; }

            internal Preview(Type type, object value)
            {
                host = ScriptableObject.CreateInstance<MaskedValuePreviewHost>();
                host.hideFlags = HideFlags.HideAndDontSave;
                Type boxType = typeof(MaskedValuePreviewBox<>).MakeGenericType(type);
                host.box = Activator.CreateInstance(boxType);
                valueField = boxType.GetField("value");
                valueField.SetValue(host.box, value);
                serializedObject = new SerializedObject(host);
                Property = serializedObject.FindProperty("box.value");
                if (Property == null)
                {
                    Dispose();
                    throw new NotSupportedException("Unity cannot serialize this value for Inspector editing.");
                }
                baseline = Property.contentHash;
                OriginalValue = Property.boxedValue;
            }

            internal bool HasPendingEdits
            {
                get
                {
                    if (!serializedObject.hasModifiedProperties) serializedObject.UpdateIfRequiredOrScript();
                    return !Property.contentHash.Equals(baseline);
                }
            }

            internal object ReadEditedValue() => Property.boxedValue;

            internal void Reset(object value)
            {
                // No Apply is required: replace only the scratch object's value
                // and rebuild its serialized snapshot after committing edits.
                valueField.SetValue(host.box, value);
                serializedObject.Update();
                baseline = Property.contentHash;
                OriginalValue = Property.boxedValue;
            }

            public void Dispose()
            {
                Property?.Dispose();
                serializedObject.Dispose();
                UnityEngine.Object.DestroyImmediate(host);
            }
        }
    }

    internal sealed class MaskedValuePreviewHost : ScriptableObject
    {
        [SerializeReference] public object box;
    }

    [Serializable]
    internal sealed class MaskedValuePreviewBox<T>
    {
        public T value;
    }

    // IMGUI inspectors have no UI Toolkit scheduler. Refresh only Inspectors,
    // at most 5 Hz and only while a runtime drawer is being displayed.
    [InitializeOnLoad]
    internal static class MaskedValueInspectorRepaint
    {
        private static double lastRequest = double.NegativeInfinity;
        private static double nextRepaint;
        private static readonly List<WeakReference<MaskedValueFieldDrawer>> drawers =
            new List<WeakReference<MaskedValueFieldDrawer>>();

        static MaskedValueInspectorRepaint()
        {
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged += state => Clear();
            AssemblyReloadEvents.beforeAssemblyReload += Clear;
            EditorApplication.quitting += Clear;
        }

        internal static void Register(MaskedValueFieldDrawer drawer) => drawers.Add(new WeakReference<MaskedValueFieldDrawer>(drawer));

        private static void Clear()
        {
            for (int i = drawers.Count - 1; i >= 0; i--)
            {
                if (drawers[i].TryGetTarget(out MaskedValueFieldDrawer drawer)) drawer.ClearPreviews();
                else drawers.RemoveAt(i);
            }
            lastRequest = double.NegativeInfinity;
        }

        internal static void Request() => lastRequest = EditorApplication.timeSinceStartup;

        private static void Update()
        {
            double now = EditorApplication.timeSinceStartup;
            if (!EditorApplication.isPlaying || now < nextRepaint || now - lastRequest > 0.5) return;
            nextRepaint = now + 0.2;
            foreach (UnityEditor.Editor editor in ActiveEditorTracker.sharedTracker.activeEditors)
                if (editor != null) editor.Repaint();
        }
    }
}
