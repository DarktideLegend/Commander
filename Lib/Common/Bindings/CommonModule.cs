using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;

namespace Commander.Lib.Common.Bindings
{
    public class CommonModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<GlobalProvider>().SingleInstance();
            builder.RegisterType<LoggerImpl>().As<Logger>();
            builder.RegisterType<ACClientImpl>().As<GameClient>().SingleInstance();
        }
    }
}

