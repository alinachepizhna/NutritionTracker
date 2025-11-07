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
        public ObservableCollection<CalendarDay> MiniCalendarDays { get; set; } = new();

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
            var lastGoal = await _db.GetLatestGoalAsync(_user.Id);

            MiniCalendarDays.Clear();

            if (lastGoal == null)
            {
                // Якщо цілі немає, показуємо тиждень «Відновлення»
                for (int i = 0; i < 7; i++)
                {
                    var date = DateTime.Today.AddDays(i);
                    MiniCalendarDays.Add(CreateCalendarDay(date, "Відновлення", Colors.Gray));
                }
                return;
            }

            // Завантажуємо всі тренування для останньої цілі
            var trainings = await _db.Database.Table<TrainingPlan>()
                                             .Where(t => t.UserId == _user.Id && t.GoalId == lastGoal.Id)
                                             .ToListAsync();

            // Генеруємо календар лише в межах періоду цілі
            for (var date = lastGoal.StartDate; date <= lastGoal.EndDate; date = date.AddDays(1))
            {
                var workout = trainings.FirstOrDefault(t => t.DayOfWeek == date.DayOfWeek.ToString());
                var workoutType = workout?.WorkoutType ?? "Відновлення";
                var color = !string.IsNullOrEmpty(workout?.WorkoutType)
                            ? WorkoutColorService.GetColor(workout.WorkoutType)
                            : Colors.Gray;

                MiniCalendarDays.Add(CreateCalendarDay(date, workoutType, color));
            }
        }

        // --- Універсальний метод для створення дня календаря ---
        private CalendarDay CreateCalendarDay(DateTime date, string workoutType, Color color)
        {
            return new CalendarDay
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
