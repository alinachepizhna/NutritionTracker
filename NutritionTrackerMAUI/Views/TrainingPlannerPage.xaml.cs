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
    public partial class TrainingPlannerPage : ContentPage
    {
        private readonly SqliteDatabaseService _db;
        private readonly User _user;
        private Goal? _goal;
        private Strategy? _strategy;

        public ObservableCollection<CalendarDay> CalendarDays { get; set; } = new();
        public ObservableCollection<WorkoutProgram> Programs { get; set; } = new();

        public event Func<Task>? OnCalendarUpdated;

        public TrainingPlannerPage(User user, SqliteDatabaseService db)
        {
            _user = user ?? throw new ArgumentNullException(nameof(user));
            _db = db ?? throw new ArgumentNullException(nameof(db));
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadGoalAndStrategyAsync();
            LoadPrograms();
            await LoadCalendarAsync();
        }

        private async Task LoadGoalAndStrategyAsync()
        {
            _goal = await _db.GetLatestGoalAsync(_user.Id);
            if (_goal == null)
            {
                await DisplayAlert("ℹ️", "У вас ще немає збережених цілей.", "OK");
                return;
            }

            _strategy = await _db.GetStrategyByIdAsync(_goal.StrategyId);

            GoalLabel.Text = _goal?.Description ?? "Ціль не задана";
            StrategyLabel.Text = _strategy?.Name ?? "Невідомо";

            if (!CalendarDays.Any())
                GenerateEmptyCalendar(_goal.StartDate, _goal.EndDate);
        }

        private void LoadPrograms()
        {
            Programs.Clear();

            Programs.Add(new WorkoutProgram
            {
                Name = "Сила та маса",
                Description = "Набір м'язової маси",
                DailyWorkouts = new List<string> { "Руки", "Ноги", "Відпочинок", "FullBody", "Кардіо", "Відновлення", "Руки" }
            });

            Programs.Add(new WorkoutProgram
            {
                Name = "Кардіо та витривалість",
                Description = "Щоденні кардіо-тренування",
                DailyWorkouts = new List<string> { "Кардіо", "Кардіо", "Відпочинок", "Кардіо", "Кардіо", "Відновлення", "Кардіо" }
            });

            Programs.Add(new WorkoutProgram
            {
                Name = "Схуднення",
                Description = "Тренування для спалювання жиру та легкі силові вправи",
                DailyWorkouts = new List<string> { "Кардіо", "FullBody", "Відпочинок", "Кардіо", "Руки", "Відновлення", "Ноги" }
            });

            ProgramCollection.ItemsSource = Programs;
            ProgramCollection.SelectionChanged += ProgramCollection_SelectionChanged;
        }

        private void ProgramCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is WorkoutProgram program)
                GenerateCalendarFromProgram(program);
        }

        private void GenerateEmptyCalendar(DateTime startDate, DateTime endDate)
        {
            CalendarDays.Clear();
            int totalDays = (int)(endDate - startDate).TotalDays + 1;

            for (int i = 0; i < totalDays; i++)
            {
                var date = startDate.AddDays(i);
                CalendarDays.Add(new CalendarDay
                {
                    Date = date,
                    DateText = date.Day.ToString(),
                    WorkoutType = "Відпочинок",
                    BackgroundColor = Colors.Gray,
                    TextColor = Colors.Black,
                    IsExtraWorkout = false
                });
            }

            CalendarCollection.ItemsSource = CalendarDays;
        }

        private void GenerateCalendarFromProgram(WorkoutProgram program)
        {
            if (_goal == null || _strategy == null) return;

            CalendarDays.Clear();
            int totalDays = (int)(_goal.EndDate - _goal.StartDate).TotalDays + 1;

            var fullWorkouts = new List<string>();
            for (int i = 0; i < totalDays; i++)
                fullWorkouts.Add(program.DailyWorkouts[i % program.DailyWorkouts.Count]);

            for (int i = 0; i < totalDays; i++)
            {
                var date = _goal.StartDate.AddDays(i);
                bool isTrainingDay = _strategy.Name switch
                {
                    "Повільно" => i % 5 == 0,
                    "Помірно" => i % 3 == 0,
                    "Агресивно" => i % 2 == 0,
                    _ => false
                };

                string workoutType = isTrainingDay ? fullWorkouts[i] : "Відпочинок";

                CalendarDays.Add(new CalendarDay
                {
                    Date = date,
                    DateText = date.Day.ToString(),
                    WorkoutType = workoutType,
                    BackgroundColor = isTrainingDay ? Colors.Red : Colors.Gray,
                    TextColor = isTrainingDay ? Colors.White : Colors.Black,
                    IsExtraWorkout = false
                });
            }

            CalendarCollection.ItemsSource = CalendarDays;
        }

        public void AddExtraWorkout(DateTime date, string workoutType)
        {
            var day = CalendarDays.FirstOrDefault(d => d.Date.Date == date.Date);
            if (day != null)
            {
                day.WorkoutType = workoutType;
                day.IsExtraWorkout = true;
                day.BackgroundColor = Colors.Green;
                day.TextColor = Colors.White;
            }
        }

        private async Task LoadCalendarAsync()
        {
            if (_goal == null || _strategy == null)
                return;

            var savedPlans = await _db.Database.Table<TrainingPlan>()
                                              .Where(t => t.UserId == _user.Id &&
                                                          t.GoalId == _goal.Id &&
                                                          t.StrategyId == _strategy.Id)
                                              .ToListAsync();

            if (savedPlans.Any())
            {
                CalendarDays.Clear();
                foreach (var day in savedPlans)
                {
                    CalendarDays.Add(new CalendarDay
                    {
                        Date = day.Date,
                        DateText = day.Date.Day.ToString(),
                        WorkoutType = day.WorkoutType,
                        BackgroundColor = day.WorkoutType == "Відпочинок" ? Colors.Gray :
                                          day.IsExtraWorkout ? Colors.Green : Colors.Red,
                        TextColor = Colors.White,
                        IsExtraWorkout = day.IsExtraWorkout
                    });
                }
                CalendarCollection.ItemsSource = CalendarDays;
            }
            else
            {
                GenerateEmptyCalendar(_goal.StartDate, _goal.EndDate);
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (_goal == null || _strategy == null)
            {
                await DisplayAlert("Помилка", "Не знайдено ціль або стратегію.", "OK");
                return;
            }

            var existingPlans = await _db.Database.Table<TrainingPlan>()
                                                 .Where(t => t.UserId == _user.Id &&
                                                             t.GoalId == _goal.Id &&
                                                             t.StrategyId == _strategy.Id)
                                                 .ToListAsync();

            foreach (var p in existingPlans)
                await _db.Database.DeleteAsync(p);

            foreach (var day in CalendarDays)
            {
                var plan = new TrainingPlan
                {
                    UserId = _user.Id,
                    GoalId = _goal.Id,
                    StrategyId = _strategy.Id,
                    Date = day.Date,
                    WorkoutType = day.WorkoutType,
                    IsExtraWorkout = day.IsExtraWorkout
                };
                await _db.Database.InsertAsync(plan);
            }

            await DisplayAlert("✅ Успіх", "План тренувань збережено!", "OK");

            if (OnCalendarUpdated != null)
                await OnCalendarUpdated.Invoke();
        }

        public class CalendarDay
        {
            public DateTime Date { get; set; }
            public string DateText { get; set; } = string.Empty;
            public Color BackgroundColor { get; set; } = Colors.Gray;
            public string WorkoutType { get; set; } = "Відпочинок";
            public Color TextColor { get; set; } = Colors.Black;
            public bool IsExtraWorkout { get; set; } = false;
        }

        public class WorkoutProgram
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public List<string> DailyWorkouts { get; set; } = new();
        }
    }
}
