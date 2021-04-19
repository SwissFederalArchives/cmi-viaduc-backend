using CMI.Contract.Common;

namespace CMI.Engine.MailTemplate
{
    public class Behältnis
    {
        private readonly ElasticContainer container;

        public Behältnis(ElasticContainer container)
        {
            this.container = container;
        }

        public string Typ => container.ContainerType;

        public string Lokation => container.ContainerLocation;

        public string Code => container.ContainerCode;

        public string Band => container.GetBand();

        public string IdName => container.IdName;
    }
}