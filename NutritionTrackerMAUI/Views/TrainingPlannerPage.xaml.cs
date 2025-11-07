using Microsoft.Maui.Controls;
using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.ObjectModel;

namespace NutritionTrackerMAUI.Views
{
    public partial class TrainingPlannerPage : ContentPage
    {
        private readonly SqliteDatabaseService _db; // Сервіс для роботи з базою
        private readonly User _user; // Поточний користувач

        private Goal? _goal; // Поточна ціль користувача
        private Strategy? _strategy; // Поточна стратегія

        // Колекція для відображення календаря на сторінці планування
        public ObservableCollection<CalendarDay> CalendarDays { get; set; } = new();

        // Подія, яка повідомляє головну сторінку, що календар оновлено
        public event Func<System.Threading.Tasks.Task>? OnCalendarUpdated;

        // Конструктор сторінки
        public TrainingPlannerPage(User user, SqliteDatabaseService db)
        {
            _user = user ?? throw new ArgumentNullException(nameof(user));
            _db = db ?? throw new ArgumentNullException(nameof(db));

            InitializeComponent();
        }

        // Метод викликається при появі сторінки
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadGoalAndStrategyAsync(); // Завантажуємо останню ціль і стратегію
        }

        // Завантаження останньої цілі та стратегії користувача
        private async System.Threading.Tasks.Task LoadGoalAndStrategyAsync()
        {
            _goal = await _db.GetLatestGoalAsync(_user.Id); // Остання ціль
            if (_goal == null)
            {
                await DisplayAlert("ℹ️", "У вас ще немає збережених цілей.", "OK");
                return;
            }

            _strategy = await _db.GetStrategyByIdAsync(_goal.StrategyId); // Стратегія для цілі

            GoalLabel.Text = _goal?.Description ?? "Ціль не задана";
            StrategyLabel.Text = _strategy?.Name ?? "Невідомо";

            GenerateCalendar(_goal.StartDate, _goal.EndDate); // Генеруємо календар для періоду цілі
        }

        // Генерація календаря для поточного плану
        private void GenerateCalendar(DateTime startDate, DateTime endDate)
        {
            CalendarDays.Clear();
            int totalDays = (endDate - startDate).Days + 1;

            for (int i = 0; i < totalDays; i++)
            {
                var date = startDate.AddDays(i);
                bool isTrainingDay = IsTrainingDay(date); // Перевірка, чи день тренувальний

                // Додаємо день до календаря
                CalendarDays.Add(new CalendarDay
                {
                    Date = date,
                    DateText = date.Day.ToString(), // Відображаємо число місяця
                    BackgroundColor = isTrainingDay ? Colors.DarkRed : Colors.Gray, // Колір залежить від того, тренування чи ні
                    WorkoutType = isTrainingDay ? "Руки" : "Відпочинок" // Тип тренування
                });
            }

            CalendarCollection.ItemsSource = CalendarDays; // Прив'язка до CollectionView на сторінці
        }

        // Логіка визначення тренувального дня залежно від стратегії
        private bool IsTrainingDay(DateTime date)
        {
            if (_strategy == null || _goal == null)
                return false;

            // Повільно – через кожні 5 днів, Помірно – через 3, Агресивно – щодня
            return _strategy.Name switch
            {
                "Повільно" => (date - _goal.StartDate).Days % 5 == 0,
                "Помірно" => (date - _goal.StartDate).Days % 3 == 0,
                "Агресивно" => (date - _goal.StartDate).Days % 2 == 0,
            };
        }

        // Збереження плану тренувань у базу даних
        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (_goal == null || _strategy == null)
            {
                await DisplayAlert("Помилка", "Не знайдено ціль або стратегію.", "OK");
                return;
            }

            // Очищаємо попередні тренування для цього користувача, цілі та стратегії
            var existingPlans = await _db.Database.Table<TrainingPlan>()
                                                 .Where(t => t.UserId == _user.Id &&
                                                             t.GoalId == _goal.Id &&
                                                             t.StrategyId == _strategy.Id)
                                                 .ToListAsync();
            foreach (var p in existingPlans)
                await _db.Database.DeleteAsync(p);

            // Зберігаємо нові
            foreach (var day in CalendarDays)
            {
                var plan = new TrainingPlan
                {
                    UserId = _user.Id,
                    GoalId = _goal.Id,
                    StrategyId = _strategy.Id,
                    DayOfWeek = day.Date.DayOfWeek.ToString(),
                    WorkoutType = day.WorkoutType
                };
                await _db.Database.InsertAsync(plan);
            }

            await DisplayAlert("✅ Успіх", "План тренувань збережено!", "OK");

            // Викликаємо подію з передачею актуальних тренувань
            if (OnCalendarUpdated != null)
                await OnCalendarUpdated.Invoke(); // Головна сторінка сама підтягує з бази
        }


        // Модель для відображення дня календаря
        public class CalendarDay
        {
            public DateTime Date { get; set; } // Дата
            public string DateText { get; set; } = string.Empty; // Текст дня (число)
            public Color BackgroundColor { get; set; } = Colors.Gray; // Колір фону
            public string WorkoutType { get; set; } = "Відпочинок"; // Тип тренування
        }
    }
}
