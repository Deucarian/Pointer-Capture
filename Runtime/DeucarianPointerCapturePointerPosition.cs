using UnityEngine;

namespace Deucarian.PointerCapture
{
    internal interface IDeucarianPointerPositionAdapter
    {
        bool TryGetPosition(out Vector2 position);

        bool TrySetPosition(Vector2 position);
    }

    internal static class DeucarianPointerCapturePointerPosition
    {
        private static IDeucarianPointerPositionAdapter adapter;

        public static void RegisterAdapter(IDeucarianPointerPositionAdapter pointerPositionAdapter)
        {
            adapter = pointerPositionAdapter;
        }

        public static bool TryGetPosition(out Vector2 position)
        {
            if (adapter != null && adapter.TryGetPosition(out position))
            {
                return true;
            }

            position = default;
            return false;
        }

        public static bool TrySetPosition(Vector2 position)
        {
            return adapter != null && adapter.TrySetPosition(position);
        }
    }
}
