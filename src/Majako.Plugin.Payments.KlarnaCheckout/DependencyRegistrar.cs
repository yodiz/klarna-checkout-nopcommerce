using Autofac;
using Autofac.Core;
using Autofac.Integration.Mvc;
using Majako.Plugin.Payments.KlarnaCheckout.Domain;
using Majako.Plugin.Payments.KlarnaCheckout.Services;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Data;

namespace Majako.Plugin.Payments.KlarnaCheckout
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        private const string CONTEXT_NAME = "nop_object_context_kco";
        public void Register(ContainerBuilder builder, ITypeFinder typeFinder)
        {
            var dataSettingsManager = new DataSettingsManager();
            var dataSettings = dataSettingsManager.LoadSettings();

            builder.RegisterType<KcoProcessor>().As<IKcoProcessor>();
            builder.RegisterType<KcoVmBuilder>().As<IKcoVmBuilder>();

            builder.Register<IDbContext>(c => RegisterIDbContext(c, dataSettings)).Named<IDbContext>(CONTEXT_NAME).InstancePerHttpRequest();
            builder.Register(c => RegisterIDbContext(c, dataSettings)).InstancePerHttpRequest();

            builder.RegisterType<EfRepository<KcoOrderRequest>>().As<IRepository<KcoOrderRequest>>().WithParameter(ResolvedParameter.ForNamed<IDbContext>(CONTEXT_NAME)).InstancePerHttpRequest();
        }
        public int Order
        {
            get { return 0; }
        }

        private KcoObjectContext RegisterIDbContext(IComponentContext componentContext, DataSettings dataSettings)
        {
            string dataConnectionStrings;

            if (dataSettings != null && dataSettings.IsValid())
            {
                dataConnectionStrings = dataSettings.DataConnectionString;
            }
            else
            {
                dataConnectionStrings = componentContext.Resolve<DataSettings>().DataConnectionString;
            }

            return new KcoObjectContext(dataConnectionStrings);
        }
    }
}
