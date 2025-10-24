using Microsoft.Maui.Controls;
using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using System;
using System.Threading.Tasks;

namespace NutritionTrackerMAUI.Controls
{
    public partial class AnthropometricWidget : ContentView
    {
        private readonly SqliteDatabaseService _db;
        private readonly User _currentUser;

        public AnthropometricWidget(User currentUser, SqliteDatabaseService db)
        {
            InitializeComponent();
            _db = db;                 // Передаємо сервіс бази даних
            _currentUser = currentUser; // Поточний користувач
        }

        private async void OnCalculateClicked(object sender, EventArgs e)
        {
            // Перевірка введених чисел
            bool validHeight = double.TryParse(HeightEntry.Text, out double height);
            bool validWeight = double.TryParse(WeightEntry.Text, out double weight);
            bool validAge = int.TryParse(AgeEntry.Text, out int age);
            string gender = GenderPicker.SelectedItem?.ToString();
            string system = SystemPicker.SelectedItem?.ToString();

            if (!validHeight || !validWeight || !validAge)
            {
                BMILabel.Text = "BMI: --";
                BMICategoryLabel.Text = "Помилка вводу чисел";
                BMICategoryLabel.TextColor = Colors.Gray;
                return;
            }

            if (string.IsNullOrEmpty(gender) || string.IsNullOrEmpty(system))
            {
                BMILabel.Text = "BMI: --";
                BMICategoryLabel.Text = "Будь ласка, оберіть стать та систему";
                BMICategoryLabel.TextColor = Colors.Gray;
                return;
            }

            // Розрахунок BMI
            double bmi = system == "Metric"
                ? weight / Math.Pow(height / 100.0, 2)
                : 703 * weight / Math.Pow(height, 2);

            bmi = Math.Round(bmi, 1);
            BMILabel.Text = $"BMI: {bmi}";

            // Визначення категорії
            string category;
            Color color;

            if (bmi < 18.5) { category = "Недостатня вага"; color = Colors.Blue; }
            else if (bmi < 25) { category = "Норма"; color = Colors.Green; }
            else if (bmi < 30) { category = "Надлишкова вага"; color = Colors.Orange; }
            else { category = "Ожиріння"; color = Colors.Red; }

            BMICategoryLabel.Text = category;
            BMICategoryLabel.TextColor = color;

            // ✅ Збереження даних у базу
            var data = new AnthropometricData
            {
                UserId = _currentUser.Id, // Обов’язково прив'язуємо до користувача
                Height = height,
                Weight = weight,
                Age = age,
                Gender = gender,
                MeasurementSystem = system
            };

            await _db.AddAnthropometricDataAsync(data); // Вставка в SQLite
        }
    }
}
