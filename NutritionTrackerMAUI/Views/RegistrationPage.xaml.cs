using Microsoft.Maui.Controls;
using NutritionTrackerMAUI.Helpers;
using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using System.Text.RegularExpressions;
using System.Globalization;

namespace NutritionTrackerMAUI.Views
{
    public partial class RegistrationPage : ContentPage
    {
        private readonly SqliteDatabaseService _db;

        public RegistrationPage()
        {
            InitializeComponent();
            _db = new SqliteDatabaseService();

            PasswordEntry.TextChanged += OnPasswordChanged;
            EmailEntry.TextChanged += OnEmailChanged;
            FirstNameEntry.TextChanged += OnNameChanged;
            LastNameEntry.TextChanged += OnNameChanged;
        }

        private void OnNameChanged(object? sender, TextChangedEventArgs e)
        {
            var entry = sender as Entry;
            if (entry == null) return;
            string input = e.NewTextValue?.Trim() ?? "";

            if (!Regex.IsMatch(input, @"^[A-Za-zА-Яа-яІіЇїЄєҐґ'\-]*$"))
            {
                entry.TextColor = Colors.Red;
                return;
            }
            if (input.Length < 2 || input.Length > 50)
            {
                entry.TextColor = Colors.Orange;
                return;
            }

            TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
            entry.Text = ti.ToTitleCase(input.ToLower());
            entry.CursorPosition = entry.Text.Length;
            entry.TextColor = Colors.Black;
        }

        private void OnEmailChanged(object? sender, TextChangedEventArgs e)
        {
            EmailValidationLabel.Text = IsValidEmail(e.NewTextValue)
                ? string.Empty
                : "Неправильний формат Email";
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private void OnPasswordChanged(object? sender, TextChangedEventArgs e)
        {
            string password = e.NewTextValue ?? "";
            double score = 0;
            string feedback = "Слабкий пароль";
            Color color = Colors.Red;

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = Regex.IsMatch(password, @"[!@#$%^&*(),.?""{}|<>]");
            bool longEnough = password.Length >= 12;

            if (longEnough) score += 0.25;
            if (hasUpper) score += 0.2;
            if (hasLower) score += 0.2;
            if (hasDigit) score += 0.2;
            if (hasSpecial) score += 0.15;

            List<string> recommendations = new();
            if (!longEnough) recommendations.Add("≥ 12 символів");
            if (!hasUpper) recommendations.Add("Велика літера");
            if (!hasLower) recommendations.Add("Мала літера");
            if (!hasDigit) recommendations.Add("Цифра");
            if (!hasSpecial) recommendations.Add("Спецсимвол (!@#$%)");

            if (score < 0.4) { feedback = "❌ Слабкий пароль. Додайте: " + string.Join(", ", recommendations); color = Colors.Red; }
            else if (score < 0.75) { feedback = "⚠️ Середній пароль. Рекомендації: " + string.Join(", ", recommendations); color = Colors.Orange; }
            else { feedback = "✅ Сильний пароль"; color = Colors.Green; }

            PasswordStrengthLabel.Text = feedback;
            PasswordStrengthLabel.TextColor = color;
            PasswordStrengthBar.Progress = score;
            PasswordStrengthBar.ProgressColor = color;
        }

        private void OnTogglePasswordClicked(object sender, EventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            TogglePasswordButton.Text = PasswordEntry.IsPassword ? "👁" : "🙈";
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            string firstName = FirstNameEntry.Text?.Trim() ?? "";
            string lastName = LastNameEntry.Text?.Trim() ?? "";
            string email = EmailEntry.Text?.Trim() ?? "";
            string password = PasswordEntry.Text ?? "";

            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Помилка", "Будь ласка, заповніть всі поля", "OK");
                return;
            }

            if (!IsValidEmail(email))
            {
                await DisplayAlert("Помилка", "Неправильний формат Email", "OK");
                return;
            }

            var existingUser = await _db.GetUserAsync(firstName, lastName);
            if (existingUser != null)
            {
                await DisplayAlert("Помилка", "Користувач з такими даними вже існує", "OK");
                return;
            }

            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PasswordHash = PasswordHasher.HashPassword(password)
            };

            await _db.AddUserAsync(user);

            // Переходимо на AnthropometricPage
            await Navigation.PushAsync(new AnthropometricPage(user, _db));
        }
    }
}
