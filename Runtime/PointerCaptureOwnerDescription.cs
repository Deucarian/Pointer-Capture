namespace Deucarian.PointerCapture
{
    internal static class PointerCaptureOwnerDescription
    {
        internal static string Describe(object owner) => owner == null ? string.Empty
            : owner is UnityEngine.Object unityObject && unityObject != null ? unityObject.name : owner.GetType().Name;
    }
}
