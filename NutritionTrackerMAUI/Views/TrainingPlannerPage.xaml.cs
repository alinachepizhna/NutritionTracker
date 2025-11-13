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

        private WorkoutProgram? _currentUserProgram;
        private bool _currentUserProgramSelected = false; // флаг для первой загрузки программы

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

            if (!_currentUserProgramSelected && _goal != null)
            {
                GenerateEmptyCalendar(_goal.StartDate, _goal.EndDate);
            }
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
        }

        private void LoadFixedPrograms()
        {
            Programs.Clear();

            Programs.Add(new WorkoutProgram
            {
                Name = "Сила та маса",
                Description = "Набір м'язової маси з чергуванням груп",
                DailyWorkouts = new List<string> { "Руки", "Ноги", "FullBody", "Відпочинок", "Руки", "Кардіо", "Ноги" }
            });

            Programs.Add(new WorkoutProgram
            {
                Name = "Кардіо та витривалість",
                Description = "Кардіо та легкі силові вправи",
                DailyWorkouts = new List<string> { "Кардіо", "FullBody", "Відновлення", "Кардіо", "FullBody", "Відновлення", "Кардіо" }
            });

            Programs.Add(new WorkoutProgram
            {
                Name = "Схуднення",
                Description = "Тренування для спалювання жиру та силові вправи",
                DailyWorkouts = new List<string> { "FullBody", "Кардіо", "Відновлення", "Ноги", "Руки", "Відновлення", "FullBody" }
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
                    DailyWorkouts = prog.DailyWorkouts.Split(',').ToList(),
                    IsLocked = prog.IsLocked
                });
            }

            UserProgramCollection.ItemsSource = UserPrograms;
            UserProgramCollection.SelectionChanged += UserProgramCollection_SelectionChanged;
        }

        private void ProgramCollection_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is WorkoutProgram program)
            {
                _isCustomProgramMode = false;
                GenerateCalendarFromProgram(program);
            }
        }

        private bool _isProgramSelectionInitializing = false;

        private async void UserProgramCollection_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isProgramSelectionInitializing) return;
            _isProgramSelectionInitializing = true;

            if (e.CurrentSelection.FirstOrDefault() is WorkoutProgram program)
            {
                _currentUserProgram = program;
                _isCustomProgramMode = !_currentUserProgram.IsLocked;

                if (_currentUserProgram.IsLocked)
                {
                    var savedPlans = await _db.Database.Table<TrainingPlan>()
                                                       .Where(t => t.UserId == _user.Id &&
                                                                   t.GoalId == _goal.Id &&
                                                                   t.ProgramName == _currentUserProgram.Name)
                                                       .OrderBy(t => t.Date)
                                                       .ToListAsync();

                    CalendarDays.Clear();

                    if (savedPlans.Any())
                    {
                        foreach (var day in savedPlans)
                        {
                            CalendarDays.Add(new CalendarDay
                            {
                                Date = day.Date,
                                DateText = day.Date.Day.ToString(),
                                WorkoutType = day.WorkoutType,
                                BackgroundColor = day.IsExtraWorkout ? Colors.Green : Colors.Gray,
                                TextColor = Colors.White,
                                IsExtraWorkout = day.IsExtraWorkout,
                                IsCustomProgramMode = false
                            });
                        }
                    }
                    else
                    {
                        GenerateCalendarFromProgram(program);
                    }
                }
                else
                {
                    GenerateCalendarFromProgram(program);
                }

                UserProgramCollection.SelectedItem = null;
                _currentUserProgramSelected = true;
            }

            _isProgramSelectionInitializing = false;
        }

        private void OnCalendarDayTapped(object sender, EventArgs e)
        {
            if (!_isCustomProgramMode) return;

            if (sender is Border border && border.BindingContext is CalendarDay day)
            {
                day.IsExtraWorkout = !day.IsExtraWorkout;

                if (!day.IsExtraWorkout)
                {
                    day.WorkoutType = "Відпочинок";
                    day.BackgroundColor = Colors.Gray;
                }
                else
                {
                    day.BackgroundColor = Colors.Green;
                }
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
                bool isTrainingDay = !_isCustomProgramMode ? _strategy.Name switch
                {
                    "Агресивно" => i % 7 < 5,
                    "Помірно" => i % 7 < 4,
                    "Повільно" => i % 7 < 2,
                    _ => true
                } : workoutType != "Відпочинок";

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

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (_goal == null || _currentUserProgram == null) return;

            var existingPlans = await _db.Database.Table<TrainingPlan>()
                                                 .Where(t => t.UserId == _user.Id &&
                                                             t.GoalId == _goal.Id &&
                                                             t.ProgramName == _currentUserProgram.Name)
                                                 .ToListAsync();
            foreach (var p in existingPlans)
                await _db.Database.DeleteAsync(p);

            foreach (var day in CalendarDays)
            {
                var plan = new TrainingPlan
                {
                    UserId = _user.Id,
                    GoalId = _goal.Id,
                    ProgramName = _currentUserProgram.Name,
                    Date = day.Date,
                    WorkoutType = day.WorkoutType,
                    IsExtraWorkout = day.IsExtraWorkout
                };
                await _db.Database.InsertAsync(plan);
            }

            if (!_currentUserProgram.IsLocked)
            {
                var dbProg = await _db.Database.Table<UserWorkoutProgram>()
                                               .Where(p => p.UserId == _user.Id && p.Name == _currentUserProgram.Name)
                                               .FirstOrDefaultAsync();
                if (dbProg != null)
                {
                    dbProg.IsLocked = true;
                    await _db.Database.UpdateAsync(dbProg);

                    var localProg = UserPrograms.FirstOrDefault(p => p.Name == dbProg.Name);
                    if (localProg != null)
                        localProg.IsLocked = true;
                }
            }

            _isCustomProgramMode = false;

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
            public bool IsLocked { get; set; } = false;
        }
    }
}
