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
            var plannerPage = new TrainingPlannerPage(_user, _db)
            {
                Title = "Планування",
                IconImageSource = "dumbbell.png"
            };
            Children.Add(plannerPage);

            // Підписка на подію оновлення календаря після збереження тренувань
            plannerPage.OnCalendarUpdated += async () => await RefreshMiniCalendarAsync();

            // Ініціалізація міні-календаря
            _ = RefreshMiniCalendarAsync();
        }

        // --- Оновлення міні-календаря ---
        private async Task RefreshMiniCalendarAsync()
        {
            MiniCalendarDays.Clear();

            var today = DateTime.Today;

            // Знаходимо початок і кінець поточного тижня (понеділок – неділя)
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var weekStart = today.AddDays(-diff);
            var weekEnd = weekStart.AddDays(6);

            // Отримуємо останню ціль користувача
            var lastGoal = await _db.GetLatestGoalAsync(_user.Id);

            // Завантажуємо тренування для останньої цілі
            var trainings = lastGoal != null
                ? await _db.Database.Table<TrainingPlan>()
                                    .Where(t => t.UserId == _user.Id && t.GoalId == lastGoal.Id)
                                    .ToListAsync()
                : new List<TrainingPlan>();

            // Генеруємо дні поточного тижня
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
    }
}
