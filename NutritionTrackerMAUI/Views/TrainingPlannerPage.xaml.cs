using Microsoft.Maui.Controls;
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
    public partial class TrainingPlannerPage : ContentPage
    {
        private readonly SqliteDatabaseService _db;
        private readonly User _user;
        private Goal? _goal;
        private Strategy? _strategy;
        private bool _isCustomProgramMode = false;

        public ObservableCollection<CalendarDay> CalendarDays { get; set; } = new();
        public ObservableCollection<WorkoutProgram> Programs { get; set; } = new();
        public ObservableCollection<WorkoutProgram> UserPrograms { get; set; } = new();

        public event Func<Task>? OnCalendarUpdated;

        public TrainingPlannerPage(User user, SqliteDatabaseService db)
        {
            InitializeComponent();
            _user = user ?? throw new ArgumentNullException(nameof(user));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadGoalAndStrategyAsync();
            LoadFixedPrograms();
            await LoadUserProgramsAsync();
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

        private void LoadFixedPrograms()
        {
            Programs.Clear();

            // Сила та маса
            Programs.Add(new WorkoutProgram
            {
                Name = "Сила та маса",
                Description = "Набір м'язової маси з чергуванням груп",
                DailyWorkouts = new List<string>
        {
            "Руки", "Ноги", "FullBody", "Відпочинок", "Руки", "Кардіо", "Відпочинок"
        }
            });

            // Кардіо та витривалість
            Programs.Add(new WorkoutProgram
            {
                Name = "Кардіо та витривалість",
                Description = "Кардіо та легкі силові вправи",
                DailyWorkouts = new List<string>
        {
            "Кардіо", "FullBody", "Відпочинок", "Кардіо", "Руки", "Відновлення", "Відпочинок"
        }
            });

            // Схуднення
            Programs.Add(new WorkoutProgram
            {
                Name = "Схуднення",
                Description = "Тренування для спалювання жиру та силові вправи",
                DailyWorkouts = new List<string>
        {
            "FullBody", "Кардіо", "Відпочинок", "Ноги", "Руки", "Відновлення", "Відпочинок"
        }
            });

            ProgramCollection.ItemsSource = Programs;
            ProgramCollection.SelectionChanged += ProgramCollection_SelectionChanged;
        }


        private async Task LoadUserProgramsAsync()
        {
            UserPrograms.Clear();
            var savedPrograms = await _db.Database.Table<UserWorkoutProgram>()
                                                  .Where(p => p.UserId == _user.Id)
                                                  .ToListAsync();

            foreach (var prog in savedPrograms)
            {
                UserPrograms.Add(new WorkoutProgram
                {
                    Name = prog.Name,
                    Description = prog.Description,
                    DailyWorkouts = prog.DailyWorkouts.Split(',').ToList()
                });
            }

            UserProgramCollection.ItemsSource = UserPrograms;
            UserProgramCollection.SelectionChanged += UserProgramCollection_SelectionChanged;
        }

        // --- Готовые программы ---
        private void ProgramCollection_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is WorkoutProgram program)
            {
                _isCustomProgramMode = false; // только просмотр
                GenerateCalendarFromProgram(program);
            }
        }

        // --- Пользовательские программы ---
        private void UserProgramCollection_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is WorkoutProgram program)
            {
                _isCustomProgramMode = true; // можно редактировать календарь
                GenerateCalendarFromProgram(program);
            }
        }

        // --- Нажатие на день календаря ---
        private void OnCalendarDayTapped(object sender, EventArgs e)
        {
            if (!_isCustomProgramMode) return; // только для пользовательских программ

            if (sender is Border border && border.BindingContext is CalendarDay day)
            {
                // Переключаем день между "Відпочинок" и тренировкой
                if (day.IsExtraWorkout)
                {
                    day.IsExtraWorkout = false;
                    day.WorkoutType = "Відпочинок";
                    day.BackgroundColor = Colors.Gray;
                }
                else
                {
                    day.IsExtraWorkout = true;
                    day.WorkoutType = "Руки"; // стандартная тренировка
                    day.BackgroundColor = Colors.Green;
                }

                // Обновляем CollectionView
                CalendarCollection.ItemsSource = null;
                CalendarCollection.ItemsSource = CalendarDays;
            }
        }


        private void OnWorkoutTypeChanged(object sender, EventArgs e)
        {
            if (!_isCustomProgramMode) return;
            if (sender is Picker picker && picker.BindingContext is CalendarDay day)
            {
                day.WorkoutType = picker.SelectedItem?.ToString() ?? "Відпочинок";
                day.BackgroundColor = day.WorkoutType == "Відпочинок" ? Colors.Gray : Colors.Green;
                day.IsExtraWorkout = day.WorkoutType != "Відпочинок";
            }
        }

        private async void OnAddUserProgramClicked(object sender, EventArgs e)
        {
            string name = await DisplayPromptAsync("Нова програма", "Введіть назву програми");
            if (string.IsNullOrWhiteSpace(name)) return;

            var newProgram = new UserWorkoutProgram
            {
                UserId = _user.Id,
                Name = name,
                Description = "Користувацька програма",
                DailyWorkouts = string.Join(",", Enumerable.Repeat("Відпочинок", (int)(_goal.EndDate - _goal.StartDate).TotalDays + 1))
            };

            await _db.Database.InsertAsync(newProgram);

            UserPrograms.Add(new WorkoutProgram
            {
                Name = name,
                Description = "Користувацька програма",
                DailyWorkouts = newProgram.DailyWorkouts.Split(',').ToList()
            });
        }

        private async void OnDeleteUserProgramClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is WorkoutProgram program)
            {
                bool confirm = await DisplayAlert("Видалити програму?", $"Ви дійсно хочете видалити '{program.Name}'?", "Так", "Ні");
                if (!confirm) return;

                var userProgram = await _db.Database.Table<UserWorkoutProgram>()
                                                   .Where(p => p.UserId == _user.Id && p.Name == program.Name)
                                                   .FirstOrDefaultAsync();
                if (userProgram != null)
                    await _db.Database.DeleteAsync(userProgram);

                UserPrograms.Remove(program);
            }
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
                    TextColor = Colors.White,
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

            for (int i = 0; i < totalDays; i++)
            {
                var date = _goal.StartDate.AddDays(i);
                string workoutType = program.DailyWorkouts[i % program.DailyWorkouts.Count];
                bool isTrainingDay = false;

                if (!_isCustomProgramMode) // готовые программы
                {
                    switch (_strategy.Name)
                    {
                        case "Агресивно": isTrainingDay = i % 7 < 5; break; // 5 тренировочных дней
                        case "Помірно": isTrainingDay = i % 7 < 4; break;    // 4
                        case "Повільно": isTrainingDay = i % 7 < 2; break;   // 2
                    }
                }
                else
                {
                    isTrainingDay = workoutType != "Відпочинок"; // пользовательская программа
                }

                Color bgColor = isTrainingDay
                                ? (_isCustomProgramMode ? Colors.Green : Colors.Red)
                                : Colors.Gray;

                CalendarDays.Add(new CalendarDay
                {
                    Date = date,
                    DateText = date.Day.ToString(),
                    WorkoutType = isTrainingDay ? workoutType : "Відпочинок",
                    BackgroundColor = bgColor,
                    TextColor = Colors.White,
                    IsExtraWorkout = _isCustomProgramMode && isTrainingDay,
                    IsCustomProgramMode = _isCustomProgramMode
                });
            }

            CalendarCollection.ItemsSource = CalendarDays;
        }





        private async Task LoadCalendarAsync()
        {
            if (_goal == null) return;

            var savedPlans = await _db.Database.Table<TrainingPlan>()
                                              .Where(t => t.UserId == _user.Id &&
                                                          t.GoalId == _goal.Id)
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
                        BackgroundColor = day.IsExtraWorkout ? Colors.Green : Colors.Gray,
                        TextColor = Colors.White,
                        IsExtraWorkout = day.IsExtraWorkout
                    });
                }
            }
            else
            {
                GenerateEmptyCalendar(_goal.StartDate, _goal.EndDate);
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (_goal == null) return;

            // Сохраняем только пользовательские программы
            var existingPlans = await _db.Database.Table<TrainingPlan>()
                                                 .Where(t => t.UserId == _user.Id &&
                                                             t.GoalId == _goal.Id)
                                                 .ToListAsync();

            foreach (var p in existingPlans)
                await _db.Database.DeleteAsync(p);

            foreach (var day in CalendarDays)
            {
                if (!_isCustomProgramMode) continue; // сохраняем только пользовательские

                var plan = new TrainingPlan
                {
                    UserId = _user.Id,
                    GoalId = _goal.Id,
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
            public Color TextColor { get; set; } = Colors.White;
            public bool IsExtraWorkout { get; set; } = false;
            public bool IsCustomProgramMode { get; set; } = false;
        }

        public class WorkoutProgram
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public List<string> DailyWorkouts { get; set; } = new();
        }
    }
}
