using System.Data.Entity;
using Nop.Core.Infrastructure;

namespace Majako.Plugin.Payments.KlarnaCheckout.Data
{
    public class StartUpTask : IStartupTask
    {
        public void Execute()
        {
            Database.SetInitializer<KcoObjectContext>(null);
        }

        public int Order
        {
            get { return 0; }
        }
    }
}
