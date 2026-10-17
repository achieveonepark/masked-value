#if UNITY_6000_0_OR_NEWER
using UnityEngine;

namespace Achieve.MaskedValues
{
    // Unity 6 supports serialization callbacks on structs. Earlier supported
    // Editors use the optional MaskedValueField bridge instead. Keep all Unity
    // dependencies out of standalone .NET builds.
    public partial struct MaskedValue<T> : ISerializationCallbackReceiver where T : unmanaged
    {
        void ISerializationCallbackReceiver.OnBeforeSerialize() { }
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            storage = default;
            key = 0;
            Value = initialValue;
        }
    }

    public partial struct GuardedValue<T> : ISerializationCallbackReceiver where T : unmanaged
    {
        void ISerializationCallbackReceiver.OnBeforeSerialize() { }
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            // Loading trusted authoring data creates a fresh runtime value.
            // Ordinary writes still validate the previous state as before.
            storage = default;
            key = 0;
            tag = 0;
            Store(initialValue);
        }
    }

    public partial struct MaskedString : ISerializationCallbackReceiver
    {
        void ISerializationCallbackReceiver.OnBeforeSerialize() { }
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            storage = null;
            key = 0;
            Value = initialValue;
        }
    }

    public partial struct MaskedData<T> : ISerializationCallbackReceiver
    {
        void ISerializationCallbackReceiver.OnBeforeSerialize() { }
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            storage = null;
            key = 0;
            // Default instances still require the caller to supply a codec.
            if (codec != null) Value = initialValue;
        }
    }
}
#endif
