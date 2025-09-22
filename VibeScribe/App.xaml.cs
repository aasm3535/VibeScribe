using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using VibeScribe.Services;
using VibeScribe.ViewModels;

namespace VibeScribe
{
    public partial class App : Application
    {
        public static ServiceProvider? Services { get; private set; }

        public App()
        {
            this.InitializeComponent();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            Services = serviceCollection.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<Messenger>();
            services.AddSingleton<TranscriptionService>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<TranscriptionViewModel>();
            services.AddTransient<TranscriptionDetailViewModel>();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            var window = new MainWindow();
            window.Activate();
        }
    }
}