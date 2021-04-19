using System.Collections.Generic;

namespace CMI.Engine.MailTemplate
{
    public class BestellerMitAuftraegen
    {
        public BestellerMitAuftraegen(Person besteller, IEnumerator<Auftrag> enumerator)
        {
            Besteller = besteller;
            while (enumerator.MoveNext())
            {
                Aufträge.Add(enumerator.Current);
            }
        }

        public Person Besteller { get; }

        public List<Auftrag> Aufträge { get; } = new List<Auftrag>();
    }
}