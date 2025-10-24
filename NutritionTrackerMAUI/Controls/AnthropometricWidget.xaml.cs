using Microsoft.Maui.Controls;
using System;

namespace NutritionTrackerMAUI.Controls
{
    public partial class AnthropometricWidget : ContentView
    {
        public AnthropometricWidget()
        {
            InitializeComponent();
        }

        private void OnCalculateClicked(object sender, EventArgs e)
        {
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

            double bmi = system == "Metric"
                ? weight / Math.Pow(height / 100.0, 2)
                : 703 * weight / Math.Pow(height, 2);

            bmi = Math.Round(bmi, 1);
            BMILabel.Text = $"BMI: {bmi}";

            string category;
            Color color;

            if (bmi < 18.5)
            {
                category = "Недостатня вага";
                color = Colors.Blue;
            }
            else if (bmi < 25)
            {
                category = "Норма";
                color = Colors.Green;
            }
            else if (bmi < 30)
            {
                category = "Надлишкова вага";
                color = Colors.Orange;
            }
            else
            {
                category = "Ожиріння";
                color = Colors.Red;
            }

            BMICategoryLabel.Text = category;
            BMICategoryLabel.TextColor = color;
        }

    }
}

