using System;

namespace NoSleep
{
    /// <summary>
    /// Coordinates sleep prevention state changes and notifies observers.
    /// </summary>
    internal class SleepStateController
    {
        public event EventHandler<SleepStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Gets whether sleep prevention is currently active.
        /// </summary>
        public bool IsPreventingSleep => SleepManagement.PreventingSleep;

        /// <summary>
        /// Starts preventing the system from sleeping.
        /// </summary>
        public void Start()
        {
            if (!IsPreventingSleep)
            {
                SleepManagement.PreventSleep();
                OnStateChanged(new SleepStateChangedEventArgs(true));
            }
        }

        /// <summary>
        /// Stops preventing the system from sleeping.
        /// </summary>
        public void Stop()
        {
            if (IsPreventingSleep)
            {
                SleepManagement.AllowSleep();
                OnStateChanged(new SleepStateChangedEventArgs(false));
            }
        }

        /// <summary>
        /// Toggles between start and stop states.
        /// </summary>
        public void Toggle()
        {
            if (IsPreventingSleep)
                Stop();
            else
                Start();
        }

        protected virtual void OnStateChanged(SleepStateChangedEventArgs e)
        {
            StateChanged?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Event arguments for sleep state changes.
    /// </summary>
    internal class SleepStateChangedEventArgs : EventArgs
    {
        public bool IsPreventingSleep { get; }

        public SleepStateChangedEventArgs(bool isPreventingSleep)
        {
            IsPreventingSleep = isPreventingSleep;
        }
    }
}
