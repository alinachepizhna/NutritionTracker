using Microcharts;
using Microsoft.Maui.Graphics;
using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using SkiaSharp;

namespace NutritionTrackerMAUI.Views
{
    public partial class StatisticsPage : ContentPage
    {
        private readonly User _user;
        private readonly SqliteDatabaseService _db;
        private bool _isWeekly = true;

        private readonly SKColor ColorTrack = SKColor.Parse("#F0F0F0"); 
        private readonly SKColor ColorTrackGreen = SKColor.Parse("#40FFFFFF"); 

        public StatisticsPage(User user, SqliteDatabaseService db)
        {
            InitializeComponent();
            _user = user;
            _db = db;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadStatisticsAsync();
        }

        private async void OnWeekClicked(object sender, EventArgs e) { _isWeekly = true; UpdateToggleUI(); await LoadStatisticsAsync(); }
        private async void OnMonthClicked(object sender, EventArgs e) { _isWeekly = false; UpdateToggleUI(); await LoadStatisticsAsync(); }
        private void UpdateToggleUI()
        {
            if (_isWeekly) { BtnWeek.BackgroundColor = Colors.White; BtnWeek.TextColor = Colors.Black; BtnMonth.BackgroundColor = Colors.Transparent; BtnMonth.TextColor = Colors.Gray; }
            else { BtnMonth.BackgroundColor = Colors.White; BtnMonth.TextColor = Colors.Black; BtnWeek.BackgroundColor = Colors.Transparent; BtnWeek.TextColor = Colors.Gray; }
        }

        private async Task LoadStatisticsAsync()
        {
            try
            {
                DateTime endDate = DateTime.Today;
                DateTime startDate = _isWeekly ? endDate.AddDays(-6) : endDate.AddDays(-29);

                var foodTask = _db.GetFoodLogsRangeAsync(_user.Id, startDate, endDate);
                var activityTask = _db.GetActivityRangeAsync(_user.Id, startDate, endDate);
                var habitsCountTask = _db.GetCompletedHabitsCountAsync(_user.Id, startDate, endDate);
                var allHabitsTask = _db.GetUserHabitsAsync(_user.Id);
                var goalTask = _db.GetLatestGoalWithStrategyAsync(_user.Id);
                var userDataTask = _db.GetUserDataAsync(_user.Id);

                await Task.WhenAll(foodTask, activityTask, habitsCountTask, allHabitsTask, goalTask, userDataTask);

                var (goal, strategy) = goalTask.Result;
                var anthropometry = userDataTask.Result.LastOrDefault();
                var targets = NutritionCalculator.CalculateTargets(_user, anthropometry, goal, strategy);

                // Оновлюємо кругові діаграми
                UpdateCaloriesCircle(foodTask.Result, startDate, endDate, (float)targets.Calories);
                UpdateStepsCircle(activityTask.Result, startDate, endDate);
                UpdateHabitsCircle(habitsCountTask.Result, allHabitsTask.Result.Count, startDate, endDate);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка", ex.Message, "OK");
            }
        }

        private void UpdateCaloriesCircle(List<FoodLogEntry> logs, DateTime start, DateTime end, float target)
        {
            int days = (end - start).Days + 1;
            double totalCals = logs.Sum(x => x.Calories);
            double avgCals = totalCals / days; 

            float percent = target > 0 ? (float)(avgCals / target) * 100 : 0;
            float chartValue = percent > 100 ? 100 : percent;
            float remaining = 100 - chartValue;

            bool isOver = percent > 110;
            SKColor mainColor = isOver ? SKColor.Parse("#D32F2F") : SKColor.Parse("#4CAF50");

            CaloriesPercentLabel.Text = $"{Math.Round(percent)}%";
            CaloriesPercentLabel.TextColor = isOver ? Colors.Red : Colors.Black; 

            AvgCaloriesLabel.Text = $"{Math.Round(avgCals)} / {Math.Round(target)}";
            CaloriesStatusLabel.Text = isOver ? "Перебір!" : "В межах норми";
            CaloriesStatusLabel.TextColor = isOver ? Colors.Red : Colors.Green;

            var entries = new[]
            {
                new ChartEntry(chartValue) { Color = mainColor, ValueLabel = "", Label = "" },
                new ChartEntry(remaining) { Color = ColorTrack, ValueLabel = "", Label = "" }
            };

            CaloriesChart.Chart = new DonutChart
            {
                Entries = entries,
                BackgroundColor = SKColors.Transparent,
                HoleRadius = 0.7f,
                LabelTextSize = 0,
                Margin = 0
            };
        }

        private void UpdateStepsCircle(List<DailyActivity> activities, DateTime start, DateTime end)
        {
            int days = (end - start).Days + 1;
            double totalSteps = activities.Sum(a => a.Steps);
            double avgSteps = totalSteps / days;
            double targetSteps = 8000; // Ціль

            float percent = (float)(avgSteps / targetSteps) * 100;
            float chartValue = percent > 100 ? 100 : percent;
            float remaining = 100 - chartValue;

            StepsPercentLabel.Text = $"{Math.Round(percent)}%";
            TotalStepsLabel.Text = $"{Math.Round(avgSteps)} кроків"; // Показуємо середнє

            var entries = new[]
            {
                new ChartEntry(chartValue) { Color = SKColor.Parse("#2196F3"), ValueLabel = "", Label = "" }, // Синій
                new ChartEntry(remaining) { Color = ColorTrack, ValueLabel = "", Label = "" }
            };

            StepsChart.Chart = new DonutChart
            {
                Entries = entries,
                BackgroundColor = SKColors.Transparent,
                HoleRadius = 0.7f,
                LabelTextSize = 0,
                Margin = 0
            };
        }

        private void UpdateHabitsCircle(int completedCount, int totalHabitsCount, DateTime start, DateTime end)
        {
            int days = (end - start).Days + 1;
            int totalPossible = totalHabitsCount * days;
            float percent = totalPossible > 0 ? ((float)completedCount / totalPossible) * 100 : 0;
            float remaining = 100 - percent;

            HabitPercentLabel.Text = $"{Math.Round(percent)}%";

            var entries = new[]
            {
                new ChartEntry(percent) { Color = SKColors.White, ValueLabel = "", Label = "" },
                new ChartEntry(remaining) { Color = ColorTrackGreen, ValueLabel = "", Label = "" }
            };

            HabitsChart.Chart = new DonutChart
            {
                Entries = entries,
                BackgroundColor = SKColors.Transparent,
                HoleRadius = 0.7f,
                LabelTextSize = 0,
                Margin = 0
            };
        }
    }
}