using System.Windows;

namespace ScreenTools
{
    /// <summary>
    /// Owns application startup, shutdown, and the top-level window-flow coordinator.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Gets the coordinator responsible for the application window and capture workflows.
        /// </summary>
        public WindowFlowCoordinator WindowFlow { get; private set; } = null!;

        /// <summary>
        /// Initializes application services, configures explicit shutdown, and opens the main workspace.
        /// </summary>
        /// <summary>
        /// Initializes application services, configures explicit shutdown, and opens the main workspace.
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            WindowFlow = new WindowFlowCoordinator(this);
            WindowFlow.ShowMainWindow();
        }
    }
}
