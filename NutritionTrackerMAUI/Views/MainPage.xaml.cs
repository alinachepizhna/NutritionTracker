using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NutritionTrackerMAUI.Views
{
    public partial class MainPage : TabbedPage
    {
        private readonly SqliteDatabaseService _db;
        private readonly User _user;

        // Колекція для міні-календаря
        public ObservableCollection<TrainingPlannerPage.CalendarDay> MiniCalendarDays { get; set; } = new();

        public MainPage(User user, SqliteDatabaseService db)
        {
            _user = user ?? throw new ArgumentNullException(nameof(user));
            _db = db ?? throw new ArgumentNullException(nameof(db));

            InitializeComponent();

            MiniCalendarCollection.ItemsSource = MiniCalendarDays;

            LoadLastGoal();

            // Додаємо вкладку планувальника тренувань
            _plannerPage = new TrainingPlannerPage(_user, _db)
            {
                Title = "Планування",
                IconImageSource = "dumbbell.png"
            };

            Children.Add(_plannerPage);

            _plannerPage.OnCalendarUpdatedWithDays += async (days) =>
            {
                MiniCalendarDays.Clear();

                var today = DateTime.Today;
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                var weekStart = today.AddDays(-diff);
                var weekEnd = weekStart.AddDays(6);

                foreach (var date in Enumerable.Range(0, 7).Select(i => weekStart.AddDays(i)))
                {
                    var day = days.FirstOrDefault(d => d.Date.Date == date.Date);
                    if (day != null)
                    {
                        MiniCalendarDays.Add(new TrainingPlannerPage.CalendarDay
                        {
                            Date = day.Date,
                            DateText = day.Date.Day.ToString(),
                            WorkoutType = day.WorkoutType,
                            BackgroundColor = day.BackgroundColor,
                        });
                    }
                    else
                    {
                        MiniCalendarDays.Add(new TrainingPlannerPage.CalendarDay
                        {
                            Date = date,
                            DateText = date.Day.ToString(),
                            WorkoutType = "Відпочинок",
                            BackgroundColor = Colors.Gray
                        });
                    }
                }
            };

            _ = RefreshMiniCalendarAsync();
        }
        

        // --- Оновлення міні-календаря ---
        private async Task RefreshMiniCalendarAsync()
        {
            MiniCalendarDays.Clear();

            if (_plannerPage == null) return;

            var today = DateTime.Today;

            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var weekStart = today.AddDays(-diff);
            var weekEnd = weekStart.AddDays(6);

            var lastGoal = await _db.GetLatestGoalAsync(_user.Id);
            if (lastGoal == null) return;

            // Фильтруем тренировки по текущей программе
            var trainings = _plannerPage.CurrentProgramName != null
                ? await _db.Database.Table<TrainingPlan>()
                                    .Where(t => t.UserId == _user.Id &&
                                                t.GoalId == lastGoal.Id &&
                                                t.ProgramName == _plannerPage.CurrentProgramName)
                                    .ToListAsync()
                : await _db.Database.Table<TrainingPlan>()
                                    .Where(t => t.UserId == _user.Id &&
                                                t.GoalId == lastGoal.Id)
                                    .ToListAsync();

            for (var date = weekStart; date <= weekEnd; date = date.AddDays(1))
            {
                var workout = trainings.FirstOrDefault(t => t.Date.Date == date.Date);
                var workoutType = workout?.WorkoutType ?? "Відпочинок";
                var color = !string.IsNullOrEmpty(workout?.WorkoutType)
                            ? WorkoutColorService.GetColor(workout.WorkoutType)
                            : Colors.Gray;

                MiniCalendarDays.Add(CreateCalendarDay(date, workoutType, color));
            }
        }



        // --- Універсальний метод для створення дня календаря ---
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

        // --- Завантаження останньої цілі користувача ---
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

        // --- Обробник кнопки «Нова ціль» ---
        private async void OnNewGoalClicked(object sender, EventArgs e)
        {
            if (Navigation != null)
                await Navigation.PushAsync(new GoalPage(_user, _db));
        }
        private TrainingPlannerPage _plannerPage;

    }
}
