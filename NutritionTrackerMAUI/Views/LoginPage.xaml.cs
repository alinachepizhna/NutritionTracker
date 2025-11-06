using Microsoft.Maui.Controls;
using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using NutritionTrackerMAUI.Helpers;

namespace NutritionTrackerMAUI.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly SqliteDatabaseService _db;

        public LoginPage(SqliteDatabaseService db)
        {
            InitializeComponent();
            _db = db;
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            string firstName = FirstNameEntry.Text?.Trim() ?? "";
            string lastName = LastNameEntry.Text?.Trim() ?? "";
            string password = PasswordEntry.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Помилка", "Будь ласка, заповніть усі поля", "OK");
                return;
            }

            var user = await _db.GetUserAsync(firstName, lastName);

            if (user == null)
            {
                await DisplayAlert("Помилка", "Користувача не знайдено", "OK");
                return;
            }

            // 🔐 Використовуємо хешування для перевірки пароля
            if (!PasswordHasher.VerifyPassword(password, user.PasswordHash))
            {
                await DisplayAlert("Помилка", "Невірний пароль", "OK");
                return;
            }


            await DisplayAlert("✅", "Вхід успішний", "OK");
            Application.Current.MainPage = new NavigationPage(new MainPage(user, _db));
        }

        private async void OnBackToRegisterClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
