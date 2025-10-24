using Microsoft.Maui.Controls;
using NutritionTrackerMAUI.Controls;
using NutritionTrackerMAUI.Models;

namespace NutritionTrackerMAUI.Views
{
    public class AnthropometricPage : ContentPage
    {
        private readonly User _user;

        public AnthropometricPage(User user)
        {
            _user = user;
            Title = "Anthropometrics";

            var widget = new AnthropometricWidget();

            Content = new ScrollView
            {
                Content = new StackLayout
                {
                    Padding = 20,
                    Spacing = 15,
                    Children = { widget }
                }
            };
        }
    }
}
