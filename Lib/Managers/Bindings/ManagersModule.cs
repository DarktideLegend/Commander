using Autofac;

namespace Commander.Lib.Services.Bindings
{
    public class ManagersModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<PlayerManagerImpl>().As<PlayerManager>().SingleInstance();
            builder.RegisterType<LoginSessionManagerImpl>().As<LoginSessionManager>().SingleInstance();
            builder.RegisterType<SettingsManagerImpl>().As<SettingsManager>().SingleInstance();
            builder.RegisterType<DebuggerImpl>().As<DebugManager>().SingleInstance();
            builder.RegisterType<RelogManagerImpl>().As<RelogManager>().SingleInstance();
            builder.RegisterType<DebuffManagerImpl>().As<DebuffManager>().SingleInstance();
            builder.RegisterType<DeathManagerImpl>().As<DeathManager>().SingleInstance();
            builder.RegisterType<BlinkManagerImpl>().As<BlinkManager>().SingleInstance();
            builder.RegisterType<VitaeManagerImpl>().As<VitaeManager>().SingleInstance();
            builder.RegisterType<RareManagerImpl>().As<RareManager>().SingleInstance();
        }
    }
}
