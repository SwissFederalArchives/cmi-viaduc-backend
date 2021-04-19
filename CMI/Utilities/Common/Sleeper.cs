using System;
using System.Threading;

namespace CMI.Utilities.Common
{
    public class Sleeper : ISleeper
    {
        public void Sleep(TimeSpan timeToSleep)
        {
            Thread.Sleep(timeToSleep);
        }
    }
}