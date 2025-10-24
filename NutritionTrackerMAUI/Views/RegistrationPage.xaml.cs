using Microsoft.Maui.Controls;
using NutritionTrackerMAUI.Helpers;
using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using System.Text.RegularExpressions;

namespace NutritionTrackerMAUI.Views
{
    public partial class RegistrationPage : ContentPage
    {
        private readonly SqliteDatabaseService _db;

        public RegistrationPage()
        {
            InitializeComponent();

            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "nutrition.db3");
            _db = new SqliteDatabaseService(dbPath);

            PasswordEntry.TextChanged += OnPasswordChanged;
            EmailEntry.TextChanged += OnEmailChanged;
        }

        // Сила пароля (з кольорами)
        private void OnPasswordChanged(object? sender, TextChangedEventArgs e)
        {
            string strength = PasswordHasher.GetPasswordStrength(e.NewTextValue);

            Color color = Colors.Black;
            string displayText = string.Empty;

            switch (strength)
            {
                case "Weak":
                case "Слабкий":
                    color = Colors.Red;
                    displayText = "Слабкий пароль";
                    break;

                case "Medium":
                case "Середній":
                    color = Colors.Orange;
                    displayText = "Середній пароль";
                    break;

                case "Strong":
                case "Сильний":
                    color = Colors.Green;
                    displayText = "Сильний пароль";
                    break;
            }

            PasswordStrengthLabel.Text = displayText;
            PasswordStrengthLabel.TextColor = color;
        }

        // Перевірка Email
        private void OnEmailChanged(object? sender, TextChangedEventArgs e)
        {
            EmailValidationLabel.Text = IsValidEmail(e.NewTextValue)
                ? string.Empty
                : "Неправильний формат Email";
        }

        // Показати/сховати пароль
        private void OnTogglePasswordClicked(object sender, EventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            TogglePasswordButton.Text = PasswordEntry.IsPassword ? "👁" : "🙈";
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        // Натискання кнопки "Зареєструватися"
        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FirstNameEntry.Text) ||
                string.IsNullOrWhiteSpace(LastNameEntry.Text) ||
                string.IsNullOrWhiteSpace(EmailEntry.Text) ||
                string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await DisplayAlert("Помилка", "Будь ласка, заповніть всі поля", "OK");
                return;
            }

            if (!IsValidEmail(EmailEntry.Text))
            {
                await DisplayAlert("Помилка", "Неправильний формат Email", "OK");
                return;
            }

            var existingUser = await _db.GetUserAsync(FirstNameEntry.Text!, LastNameEntry.Text!);
            if (existingUser != null)
            {
                await DisplayAlert("Помилка", "Користувач з такими даними вже існує", "OK");
                return;
            }

            var user = new User
            {
                FirstName = FirstNameEntry.Text!,
                LastName = LastNameEntry.Text!,
                Email = EmailEntry.Text!,
                PasswordHash = PasswordHasher.HashPassword(PasswordEntry.Text!)
            };

            await _db.AddUserAsync(user);
            await DisplayAlert("Успіх", "Реєстрація успішна!", "OK");

            await Navigation.PushAsync(new AnthropometricPage(user));
        }
    }
}
