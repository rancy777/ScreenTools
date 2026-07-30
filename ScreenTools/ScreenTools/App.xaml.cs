using System.Windows;

namespace ScreenTools
{
    public partial class App : Application
    {
        public WindowFlowCoordinator WindowFlow { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            WindowFlow = new WindowFlowCoordinator(this);
            WindowFlow.ShowMainWindow();
        }
    }
}
