using Microsoft.Maui.Controls;
using NutritionTrackerMAUI.Models;

namespace NutritionTrackerMAUI.Controls
{
    public partial class AnthropometricWidget : ContentView
    {
        private AnthropometricData? _data;

        public AnthropometricWidget()
        {
            InitializeComponent();
        }

        // Передача даних у віджет
        public void SetData(AnthropometricData data)
        {
            _data = data;
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (_data == null)
                return;

            double bmi = CalculateBMI(_data);
            string category = GetBMICategory(bmi);
            Color color = GetBMIColor(category);

            HeightLabel.Text = $"{_data.Height} {(_data.MeasurementSystem.StartsWith("Metric") ? "см" : "in")}";
            WeightLabel.Text = $"{_data.Weight} {(_data.MeasurementSystem.StartsWith("Metric") ? "кг" : "lbs")}";
            AgeLabel.Text = $"{_data.Age}";
            GenderLabel.Text = _data.Gender;
            SystemLabel.Text = _data.MeasurementSystem;

            BMILabel.Text = $"BMI: {bmi:F1}";
            BMICategoryLabel.Text = category;
            BMICategoryLabel.TextColor = color;
        }

        private double CalculateBMI(AnthropometricData data)
        {
            if (data.MeasurementSystem.StartsWith("Imperial"))
            {
                // Формула для імперської системи
                return (data.Weight / (data.Height * data.Height)) * 703;
            }
            else
            {
                // Формула для метричної системи
                double heightMeters = data.Height / 100.0;
                return data.Weight / (heightMeters * heightMeters);
            }
        }

        private string GetBMICategory(double bmi)
        {
            if (bmi < 18.5)
                return "Недостатня вага";
            if (bmi < 25)
                return "Норма";
            if (bmi < 30)
                return "Надмірна вага";
            return "Ожиріння";
        }

        private Color GetBMIColor(string category)
        {
            return category switch
            {
                "Недостатня вага" => Colors.Blue,
                "Норма" => Colors.Green,
                "Надмірна вага" => Colors.Orange,
                "Ожиріння" => Colors.Red,
                _ => Colors.Gray
            };
        }
    }
}
