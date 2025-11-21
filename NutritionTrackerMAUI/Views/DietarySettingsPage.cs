using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;

namespace NutritionTrackerMAUI.Views
{
    public partial class DietarySettingsPage : ContentPage
    {
        private readonly SqliteDatabaseService _db;
        private readonly User _user;
        private UserDietarySettings? _settings;

        public DietarySettingsPage(User user, SqliteDatabaseService db)
        {
            InitializeComponent();
            _user = user;
            _db = db;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _settings = await _db.GetDietarySettingsAsync(_user.Id);
            BindingContext = _settings;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (_settings != null)
            {
                await _db.SaveDietarySettingsAsync(_settings);
                await DisplayAlert("Успіх", "Налаштування збережено!", "OK");
                await Navigation.PopAsync();
            }
        }
    }
}