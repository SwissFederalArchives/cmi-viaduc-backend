using System.Collections.Generic;

namespace CMI.Engine.MailTemplate
{
    public class ListWithFlags<T> : List<T>
    {
        public bool HatKeinen => Count == 0;

        public bool HatGenauEinen => Count == 1;

        public bool HatMehrAlsEinen => Count > 1;
    }
}