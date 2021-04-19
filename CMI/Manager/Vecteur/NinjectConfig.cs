using CMI.Contract.Order;
using CMI.Utilities.ProxyClients.Order;
using Ninject.Modules;

namespace CMI.Manager.Vecteur
{
    /// <summary>Configuration for the dependency injection.</summary>
    public class NinjectConfig : NinjectModule
    {
        public override void Load()
        {
            RegisterServices();
        }

        private void RegisterServices()
        {
            Kernel?.Bind<IVecteurActions>().To<VecteurActionsClient>();
            Kernel?.Bind<IMessageBusCallHelper>().To<MessageBusCallHelper>();
            Kernel?.Bind<IPublicOrder>().To<OrderManagerClient>();
            Kernel?.Bind<IDigitizationHelper>().To<DigitizationHelper>();
        }
    }
}