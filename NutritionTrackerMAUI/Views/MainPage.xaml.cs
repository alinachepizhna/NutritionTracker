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
            _plannerPage.SendAppearing();

            Children.Add(_plannerPage);

            _plannerPage.OnCalendarUpdatedWithDays += (days) =>
            {
                UpdateMiniCalendar(days.ToList()); // .ToList() создаёт List<CalendarDay>
                return Task.CompletedTask;
            };


            _ = RefreshMiniCalendarAsync();
        }

        private async Task RefreshMiniCalendarAsync()
        {
            MiniCalendarDays.Clear();
            var today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var weekStart = today.AddDays(-diff);

            if (_plannerPage.CurrentProgramName == null)
            {
                for (var date = weekStart; date < weekStart.AddDays(7); date = date.AddDays(1))
                {
                    MiniCalendarDays.Add(CreateCalendarDay(date, "Відпочинок", Colors.Gray));
                }
                return;
            }

            var programDays = _plannerPage.CalendarDays
                                .Where(d => d.Date >= weekStart && d.Date < weekStart.AddDays(7))
                                .ToList();

            var lastGoal = await _db.GetLatestGoalAsync(_user.Id);
            var trainingsInDb = lastGoal != null
                ? await _db.Database.Table<TrainingPlan>()
                    .Where(t => t.UserId == _user.Id &&
                                t.GoalId == lastGoal.Id &&
                                t.ProgramName == _plannerPage.CurrentProgramName)
                    .ToListAsync()
                : new System.Collections.Generic.List<TrainingPlan>();

            for (var date = weekStart; date < weekStart.AddDays(7); date = date.AddDays(1))
            {
                var dayFromPlanner = programDays.FirstOrDefault(d => d.Date.Date == date.Date);
                var dayFromDb = trainingsInDb.FirstOrDefault(t => t.Date.Date == date.Date);

                string workoutType = dayFromDb?.WorkoutType ?? dayFromPlanner?.WorkoutType ?? "Відпочинок";
                Color color = dayFromDb != null
                              ? (dayFromDb.IsExtraWorkout ? Colors.Green : WorkoutColorService.GetColor(workoutType))
                              : dayFromPlanner?.BackgroundColor ?? Colors.Gray;

                MiniCalendarDays.Add(CreateCalendarDay(date, workoutType, color));
            }
        }

        private void UpdateMiniCalendar(System.Collections.Generic.List<TrainingPlannerPage.CalendarDay> days)
        {
            MiniCalendarDays.Clear();

            var today = DateTime.Today;
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            var weekStart = today.AddDays(-diff);

            for (var i = 0; i < 7; i++)
            {
                var date = weekStart.AddDays(i);
                var day = days.FirstOrDefault(d => d.Date.Date == date.Date);

                MiniCalendarDays.Add(CreateCalendarDay(
                    date,
                    day?.WorkoutType ?? "Відпочинок",
                    day?.BackgroundColor ?? Colors.Gray
                ));
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

        private async void OnNewGoalClicked(object sender, EventArgs e)
        {
            if (Navigation != null)
                await Navigation.PushAsync(new GoalPage(_user, _db));
        }

    }
}
