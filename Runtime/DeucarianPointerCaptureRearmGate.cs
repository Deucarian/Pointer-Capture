namespace Deucarian.PointerCapture
{
    internal sealed class DeucarianPointerCaptureRearmGate
    {
        private bool hasFocus = true;
        private bool waitingForNeutralInput;
        private bool waitingForNewAction;

        public bool CanProcessInput =>
            hasFocus && !waitingForNeutralInput && !waitingForNewAction;

        public void SetFocus(bool focused, bool requireNeutralInput = true)
        {
            hasFocus = focused;
            if (!focused)
            {
                BlockUntilNewAction(requireNeutralInput);
            }
        }

        public void BlockUntilNewAction(bool requireNeutralInput = true)
        {
            waitingForNeutralInput = requireNeutralInput;
            waitingForNewAction = true;
        }

        public void Refresh(bool isInputNeutral, bool hasNewCaptureAction)
        {
            if (!hasFocus)
            {
                return;
            }

            if (waitingForNeutralInput)
            {
                if (isInputNeutral)
                {
                    waitingForNeutralInput = false;
                }

                return;
            }

            if (waitingForNewAction && hasNewCaptureAction)
            {
                waitingForNewAction = false;
            }
        }
    }
}
