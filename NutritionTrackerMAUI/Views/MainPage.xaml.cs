using Microsoft.Maui.Graphics;
using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NutritionTrackerMAUI.Views
{
    public partial class MainPage : TabbedPage
    {
        private readonly SqliteDatabaseService _db;
        private readonly User _user;
        private TrainingPlannerPage _plannerPage;

        public ObservableCollection<TrainingPlannerPage.CalendarDay> MiniCalendarDays { get; set; } = new();

        public MainPage(User user, SqliteDatabaseService db)
        {
            _user = user ?? throw new ArgumentNullException(nameof(user));
            _db = db ?? throw new ArgumentNullException(nameof(db));

            InitializeComponent();
            BindingContext = this;
            MiniCalendarCollection.ItemsSource = MiniCalendarDays;

            LoadLastGoal();

            _plannerPage = new TrainingPlannerPage(_user, _db)
            {
                Title = "Планування",
                IconImageSource = "dumbbell.png"
            };

            Children.Add(_plannerPage);

            _plannerPage.OnCalendarUpdatedWithDays += async (days) =>
            {
                if (days != null && days.Any())
                {
                    UpdateMiniCalendar(days.ToList());
                    UpdateTodayWorkout(days.ToList());
                }
                await Task.CompletedTask;
            };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_plannerPage != null)
            {
                await _plannerPage.InitializeDataAsync();

                if (_plannerPage.CalendarDays.Any())
                {
                    await RefreshMiniCalendarAsync();
                    UpdateTodayWorkout(_plannerPage.CalendarDays.ToList());
                }
                else
                {
                    await Task.Delay(200);
                    await RefreshMiniCalendarAsync();
                }
            }
        }
        private async void MainPage_CurrentPageChanged(object? sender, EventArgs e)
        {
            if (CurrentPage?.Title == "Головна")
            {
                await _plannerPage.InitialLoadTask;
                await RefreshMiniCalendarAsync();
            }
        }
        private async Task RefreshMiniCalendarAsync()
        {
            MiniCalendarDays.Clear();
            var today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var weekStart = today.AddDays(-diff);

            var plannerDays = _plannerPage.CalendarDays.ToList();

            if (!plannerDays.Any())
            {

                for (var date = weekStart; date < weekStart.AddDays(7); date = date.AddDays(1))
                {
                    MiniCalendarDays.Add(CreateCalendarDay(date, "Відпочинок", Colors.Gray));
                }
                UpdateTodayWorkout(new List<TrainingPlannerPage.CalendarDay>()); // Оновлюємо етикетку
                return;
            }


            var programDays = plannerDays
                                         .Where(d => d.Date.Date >= weekStart.Date && d.Date.Date < weekStart.AddDays(7).Date)
                                         .ToList();

            for (var date = weekStart; date < weekStart.AddDays(7); date = date.AddDays(1))
            {
                var dayFromPlanner = programDays.FirstOrDefault(d => d.Date.Date == date.Date);

                string workoutType = dayFromPlanner?.WorkoutType ?? "Відпочинок";
                Color color = dayFromPlanner?.BackgroundColor ?? Colors.Gray;

                MiniCalendarDays.Add(CreateCalendarDay(date, workoutType, color));
            }
            UpdateTodayWorkout(plannerDays);
        }

        private void UpdateMiniCalendar(List<TrainingPlannerPage.CalendarDay> allDays)
        {
            MiniCalendarDays.Clear();
            var today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var weekStart = today.AddDays(-diff);

            var programDays = allDays
                                .Where(d => d.Date.Date >= weekStart.Date && d.Date.Date < weekStart.AddDays(7).Date)
                                .ToList();

            for (var date = weekStart; date < weekStart.AddDays(7); date = date.AddDays(1))
            {
                var dayFromPlanner = programDays.FirstOrDefault(d => d.Date.Date == date.Date);

                string workoutType = dayFromPlanner?.WorkoutType ?? "Відпочинок";
                Color color = dayFromPlanner?.BackgroundColor ?? Colors.Gray;

                MiniCalendarDays.Add(CreateCalendarDay(date, workoutType, color));
            }
        }

        private void UpdateTodayWorkout(List<TrainingPlannerPage.CalendarDay> allDays)
        {
            var todayPlan = allDays.FirstOrDefault(d => d.Date.Date == DateTime.Today.Date);

            if (todayPlan != null && todayPlan.WorkoutType != "Відпочинок")
            {
                TodayWorkoutLabel.Text = $"Сьогодні: {todayPlan.WorkoutType} 🔥";
                TodayWorkoutLabel.TextColor = todayPlan.BackgroundColor;
            }
            else
            {
                TodayWorkoutLabel.Text = "Сьогодні: Відпочинок. Відновлюйся! 😌";
                TodayWorkoutLabel.TextColor = Colors.Gray;
            }
        }

        private TrainingPlannerPage.CalendarDay CreateCalendarDay(DateTime date, string workoutType, Color color)
        {
            return new TrainingPlannerPage.CalendarDay
            {
                Date = date,
                DateText = date.Day.ToString(),
                WorkoutType = workoutType,
                BackgroundColor = color
            };
        }

        private async void LoadLastGoal()
        {
            var lastGoal = await _db.GetLatestGoalAsync(_user.Id);
            if (lastGoal != null)
            {
                var strategy = await _db.GetStrategyByIdAsync(lastGoal.StrategyId);
                CurrentGoalLabel.Text = $"Ціль: {lastGoal.Description}";
                CurrentStrategyLabel.Text = $"Стратегія: {strategy?.Name ?? "Невідомо"}";
            }
            else
            {
                CurrentGoalLabel.Text = "Ціль: не задано";
                CurrentStrategyLabel.Text = "Стратегія: —";
            }
        }

        private async void OnRefreshCalendarClicked(object sender, EventArgs e)
        {
            await _plannerPage.ForceReloadAsync();
            await RefreshMiniCalendarAsync();
            LoadLastGoal();
            MiniCalendarCollection.ItemsSource = null;
            MiniCalendarCollection.ItemsSource = MiniCalendarDays;
        }
        private async void OnNewGoalClicked(object sender, EventArgs e)
        {
            if (Navigation != null)
                await Navigation.PushAsync(new GoalPage(_user, _db));
        }
    }
}

