using Microsoft.Maui.Controls;
using NutritionTrackerMAUI.Views;

namespace NutritionTrackerMAUI
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        // MAUI 7+ рекомендує ініціалізувати через CreateWindow
        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = new Window(new NavigationPage(new RegistrationPage()));
            return window;
        }
    }
}
