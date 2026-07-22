using UnityEngine;
using UnityEngine.InputSystem;

namespace Deucarian.PointerCapture.InputSystem
{
    internal sealed class DeucarianPointerCaptureInputSystemAdapter :
        IDeucarianPointerPositionAdapter
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            DeucarianPointerCapturePointerPosition.RegisterAdapter(
                new DeucarianPointerCaptureInputSystemAdapter());
        }

        public bool TryGetPosition(out Vector2 position)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                position = default;
                return false;
            }

            position = mouse.position.ReadValue();
            return true;
        }

        public bool TrySetPosition(Vector2 position)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return false;
            }

            mouse.WarpCursorPosition(position);
            return true;
        }
    }
}
