using System;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Commander.Lib.Common;
using Commander.Lib.Common.Bindings;
using Commander.Lib.Models.Bindings;
using Commander.Lib.Managers;
using Commander.Lib.Managers.Bindings;
using Commander.Lib.Views;
using Commander.Lib.Views.Bindings;
using Decal.Adapter;
using Decal.Adapter.Wrappers;

namespace Commander
{
    [FriendlyName("Commander")]
    public class FilterCore : FilterBase
    {
        private IContainer _container;
        private Logger _logger;
        private static Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
        private static string[] EmbeddedLibraries =
           ExecutingAssembly.GetManifestResourceNames().Where(x => x.EndsWith(".dll")).ToArray();

        public FilterCore()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name).Name + ".dll";
 
            var resourceName = EmbeddedLibraries.FirstOrDefault(x => x.EndsWith(assemblyName));
            if (resourceName == null)
            {
                return null;
            }
 
            using (var stream = ExecutingAssembly.GetManifestResourceStream(resourceName))
            {
                var bytes = new byte[stream.Length];
                stream.Read(bytes, 0, bytes.Length);
                return Assembly.Load(bytes);
            }
        }

        private void ConfigureServices(NetServiceHost Host, CoreManager Core)
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(Host).As<NetServiceHost>().SingleInstance();
            builder.RegisterInstance(Core).As<CoreManager>().SingleInstance();
            builder.RegisterModule(new CommonModule());
            builder.RegisterModule(new ManagersModule());
            builder.RegisterModule(new ModelsModule());
            builder.RegisterModule(new ViewsModule());
            _container = builder.Build();
        }

        protected override void Startup()
        {
            try
            {
                ConfigureServices(Host, Core);
                _logger = _container.Resolve<Logger>().Scope("App");
                _logger.Info("Startup()");
                _container.Resolve<DebugManager>().Start(); 
                Core.FilterInitComplete += FilterInitComplete;
            } catch (Exception ex) { _logger.Error(ex); }
        }

        protected override void Shutdown()
        {
            try
            {
                _logger.Info("ShutDown()");
                Core.FilterInitComplete -= FilterInitComplete;
                _container.Resolve<MainView>().Dispose();          
                _container.Resolve<RareManager>().Dispose();
                _container.Resolve<BlinkManager>().Dispose();
                _container.Resolve<DebuffManager>().Dispose();
                _container.Resolve<VitaeManager>().Dispose();
                _container.Resolve<DeathManager>().Dispose();
                _container.Resolve<RelogManager>().Dispose();
                _container.Resolve<PlayerManager>().Dispose();
                _container.Resolve<DebugManager>().Dispose();      
                _container.Resolve<SettingsManager>().Dispose();
                _container.Resolve<LoginSessionManager>().Dispose();
            } catch (Exception ex) { _logger.Error(ex); }
        }

        private void FilterInitComplete(object sender, EventArgs e)
        {
            try
            {
                _logger.Info("FilterInitComplete()");
                _container.Resolve<LoginSessionManager>().Init();
                _container.Resolve<SettingsManager>().Init();
                _container.Resolve<PlayerManager>().Init();
                _container.Resolve<RelogManager>().Init();
                _container.Resolve<DeathManager>().Init();
                _container.Resolve<VitaeManager>().Init();
                _container.Resolve<DebuffManager>().Init();
                _container.Resolve<DebugManager>().Init();
                _container.Resolve<BlinkManager>().Init();
                _container.Resolve<RareManager>().Init();
                _container.Resolve<MainView>().Init();
            } catch (Exception ex) { _logger.Error(ex); }
        }
    }
}
