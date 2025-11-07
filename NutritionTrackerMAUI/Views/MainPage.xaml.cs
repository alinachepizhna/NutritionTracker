using Microsoft.Maui.Controls;
using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NutritionTrackerMAUI.Views
{
    public partial class MainPage : TabbedPage
    {
        private readonly SqliteDatabaseService _db; // Сервіс для роботи з базою
        private readonly User _user; // Поточний користувач

        // Колекція для міні-календаря на головній сторінці
        public ObservableCollection<CalendarDay> MiniCalendarDays { get; set; } = new();

        public MainPage(User user, SqliteDatabaseService db)
        {
            _user = user ?? throw new ArgumentNullException(nameof(user));
            _db = db ?? throw new ArgumentNullException(nameof(db));

            InitializeComponent();

            MiniCalendarCollection.ItemsSource = MiniCalendarDays;

            LoadLastGoal();

            // Додаємо вкладку планування тренувань
            var plannerPage = new TrainingPlannerPage(_user, _db)
            {
                Title = "Планування",
                IconImageSource = "dumbbell.png"
            };
            Children.Add(plannerPage);

            // Підписуємося на подію оновлення календаря після збереження тренувань
            plannerPage.OnCalendarUpdated += async () => await RefreshMiniCalendarAsync();

            // Ініціалізація міні-календаря
            RefreshMiniCalendarAsync().ConfigureAwait(false);
        }

        // --- Метод для оновлення міні-календаря ---
        private async Task RefreshMiniCalendarAsync()
        {
            // Отримуємо реальні тренування користувача з бази
            var trainings = await _db.Database.Table<TrainingPlan>()
                                             .Where(t => t.UserId == _user.Id)
                                             .ToListAsync();

            // Генеруємо календар на поточний місяць з реальними типами тренувань
            var calendar = GenerateMonthlyCalendar(DateTime.Today.Year, DateTime.Today.Month, trainings);

            MiniCalendarDays.Clear();
            foreach (var day in calendar)
                MiniCalendarDays.Add(day);
        }


        // --- Генерація календаря ---
        public ObservableCollection<CalendarDay> GenerateMonthlyCalendar(
            int year,
            int month,
            System.Collections.Generic.List<TrainingPlan> trainings)
        {
            var daysInMonth = DateTime.DaysInMonth(year, month);
            var calendar = new ObservableCollection<CalendarDay>();

            for (int i = 1; i <= daysInMonth; i++)
            {
                var date = new DateTime(year, month, i);

                // Шукаємо тренування по дню тижня
                var workout = trainings.FirstOrDefault(t => t.DayOfWeek == date.DayOfWeek.ToString());

                calendar.Add(new CalendarDay
                {
                    Date = date,
                    DateText = date.Day.ToString(),
                    WorkoutType = workout?.WorkoutType ?? "",
                    BackgroundColor = !string.IsNullOrEmpty(workout?.WorkoutType)
                                      ? WorkoutColorService.GetColor(workout.WorkoutType)
                                      : Colors.LightGray
                });
            }

            return calendar;
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

        // --- Кнопка "Нова ціль" ---
        private async void OnNewGoalClicked(object sender, EventArgs e)
        {
            if (Navigation != null)
                await Navigation.PushAsync(new GoalPage(_user, _db));
        }
    }

    // --- Модель дня календаря ---
}
