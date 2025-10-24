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
            try
            {
                double height = double.Parse(HeightEntry.Text);
                double weight = double.Parse(WeightEntry.Text);
                int age = int.Parse(AgeEntry.Text);
                string gender = GenderPicker.SelectedItem?.ToString() ?? "Невідомо";
                string system = SystemPicker.SelectedItem?.ToString() ?? "Metric";

                double bmi;

                if (system == "Metric")
                {
                    double heightMeters = height / 100.0;
                    bmi = weight / (heightMeters * heightMeters);
                }
                else
                {
                    bmi = 703 * (weight / (height * height));
                }

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
            catch
            {
                BMILabel.Text = "BMI: --";
                BMICategoryLabel.Text = "Помилка вводу";
                BMICategoryLabel.TextColor = Colors.Gray;
            }
        }
    }
}
