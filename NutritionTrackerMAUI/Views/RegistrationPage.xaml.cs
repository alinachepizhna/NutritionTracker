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

            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "nutrition.db3");
            _db = new SqliteDatabaseService(dbPath);

            PasswordEntry.TextChanged += OnPasswordChanged;
            EmailEntry.TextChanged += OnEmailChanged;
            FirstNameEntry.TextChanged += OnNameChanged;
            LastNameEntry.TextChanged += OnNameChanged;
        }

        // ✅ Перевірка і нормалізація імені
        private void OnNameChanged(object? sender, TextChangedEventArgs e)
        {
            var entry = sender as Entry;
            if (entry == null) return;

            string input = e.NewTextValue?.Trim() ?? string.Empty;

            // Дозволені лише літери, дефіс і апостроф
            if (!Regex.IsMatch(input, @"^[A-Za-zА-Яа-яІіЇїЄєҐґ'\-]*$"))
            {
                entry.TextColor = Colors.Red;
                return;
            }

            // Довжина від 2 до 50
            if (input.Length < 2 || input.Length > 50)
            {
                entry.TextColor = Colors.Orange;
                return;
            }

            // Title Case
            TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
            entry.Text = ti.ToTitleCase(input.ToLower());
            entry.CursorPosition = entry.Text.Length;
            entry.TextColor = Colors.Black;
        }

        // ✅ Перевірка Email
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

        // ✅ Перевірка сили пароля
        private void OnPasswordChanged(object? sender, TextChangedEventArgs e)
        {
            string password = e.NewTextValue ?? string.Empty;
            string feedback = GetPasswordFeedback(password, out Color color);

            PasswordStrengthLabel.Text = feedback;
            PasswordStrengthLabel.TextColor = color;
        }

        // 🔒 Перевірка складності з деталізацією
        private string GetPasswordFeedback(string password, out Color color)
        {
            color = Colors.Red;

            if (string.IsNullOrWhiteSpace(password))
                return "Пароль порожній";

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = Regex.IsMatch(password, @"[!@#$%^&*(),.?""{}|<>]");
            bool longEnough = password.Length >= 12;

            var missing = new List<string>();
            if (!hasUpper) missing.Add("велику літеру");
            if (!hasLower) missing.Add("малу літеру");
            if (!hasDigit) missing.Add("цифру");
            if (!hasSpecial) missing.Add("спецсимвол");
            if (!longEnough) missing.Add("довжину ≥ 12");

            // Список популярних паролів
            var weakPasswords = new[] { "password", "123456", "qwerty", "admin", "letmein" };
            if (weakPasswords.Any(p => password.Equals(p, StringComparison.OrdinalIgnoreCase)))
            {
                return "❌ Дуже слабкий пароль (поширений)";
            }

            if (missing.Count == 0)
            {
                color = Colors.Green;
                return "✅ Сильний пароль";
            }

            if (missing.Count <= 2)
            {
                color = Colors.Orange;
                return $"⚠️ Середній пароль. Додайте: {string.Join(", ", missing)}";
            }

            return $"❌ Слабкий пароль. Додайте: {string.Join(", ", missing)}";
        }

        // 👁 Показати/сховати пароль
        private void OnTogglePasswordClicked(object sender, EventArgs e)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            TogglePasswordButton.Text = PasswordEntry.IsPassword ? "👁" : "🙈";
        }

        // ✅ Кнопка "Зареєструватися"
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

            // Перевірка дублю користувача
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
            await DisplayAlert("✅ Успіх", "Реєстрація успішна!", "OK");

            await Navigation.PushAsync(new AnthropometricPage(user));
        }
    }
}
