using System;

namespace CMI.Manager.Cache
{
    public class ThresholdExceededEventArgs : EventArgs
    {
        public ThresholdExceededEventArgs(ulong threshold, ulong currentSize)
        {
            Threshold = threshold;
            CurrentSize = currentSize;
        }

        public ulong Threshold { get; }
        public ulong CurrentSize { get; }
    }
}