using System;
using System.IO;
using Achieve.MaskedValues;
using UnityEditor;
using UnityEngine;

public static class MaskedValuesVerification
{
    [MenuItem("Tools/Masked Values/Run Verification")]
    public static void Run()
    {
        try
        {
            var vector = new MaskedValue<Vector3>(new Vector3(1, 2, 3));
            if (vector.Value != new Vector3(1, 2, 3)) throw new Exception("Vector3 mismatch");
            var quaternion = new MaskedValue<Quaternion>(Quaternion.Euler(1, 2, 3));
            Quaternion expected = Quaternion.Euler(1, 2, 3);
            Quaternion actual = quaternion.Value;
            if (actual.x != expected.x || actual.y != expected.y || actual.z != expected.z || actual.w != expected.w)
                throw new Exception("Quaternion mismatch");
            var color = new MaskedValue<Color>(new Color(0.1f, 0.2f, 0.3f, 0.4f));
            if (color.Value != new Color(0.1f, 0.2f, 0.3f, 0.4f)) throw new Exception("Color mismatch");
            var guardedVector = new GuardedValue<Vector3>(new Vector3(1, 2, 3));
            guardedVector.RefreshMask();
            if (!guardedVector.TryGetValue(out Vector3 guardedResult) || guardedResult != new Vector3(1, 2, 3))
                throw new Exception("Guarded Vector3 mismatch");
            var guardedQuaternion = new GuardedValue<Quaternion>(expected);
            guardedQuaternion.RefreshMask();
            Quaternion guardedRotation = guardedQuaternion.Value;
            if (guardedRotation.x != expected.x || guardedRotation.y != expected.y ||
                guardedRotation.z != expected.z || guardedRotation.w != expected.w)
                throw new Exception("Guarded Quaternion mismatch");
            string report = MaskedValuesVerificationSuite.Run();
            File.WriteAllText(Path.Combine(Application.dataPath, "../../unity-verification.md"), report);
            Debug.Log("MASKED_VALUES_VERIFICATION_PASSED\n" + report);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
    }
}
