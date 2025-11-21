using Microsoft.Maui.Graphics;
using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using System.Collections.ObjectModel;

namespace NutritionTrackerMAUI.Views
{
    public partial class MainPage : TabbedPage
    {
        private readonly SqliteDatabaseService _db;
        private readonly User _user;
        private TrainingPlannerPage _plannerPage;

        public ObservableCollection<TrainingPlannerPage.CalendarDay> MiniCalendarDays { get; set; } = new();


        private string _recTitle = "Аналіз...";
        public string RecommendationTitle
        {
            get => _recTitle;
            set { _recTitle = value; OnPropertyChanged(); }
        }

        private string _recMessage = "Завантаження даних...";
        public string RecommendationMessage
        {
            get => _recMessage;
            set { _recMessage = value; OnPropertyChanged(); }
        }

        private Color _recColor = Colors.Gray;
        public Color RecommendationColor
        {
            get => _recColor;
            set { _recColor = value; OnPropertyChanged(); }
        }
        // ==========================================

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

            Children.Add(new FoodDiaryPage(_user, _db)
            {
                Title = "Щоденник",
            });

            Children.Add(new HabitsPage(_user, _db)
            {
                Title = "Звички",
            });
            Children.Add(new StatisticsPage(_user, _db)
            {
                Title = "Аналітика",
            });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await UpdateRecommendationWidget();

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

        private int _curSteps;
        public int CurrentSteps
        {
            get => _curSteps;
            set { _curSteps = value; OnPropertyChanged(); }
        }

        private string _curWater = "0 мл";
        public string CurrentWaterText
        {
            get => _curWater;
            set { _curWater = value; OnPropertyChanged(); }
        }

        private string _curSitting = "0 год";
        public string CurrentSittingText
        {
            get => _curSitting;
            set { _curSitting = value; OnPropertyChanged(); }
        }

        private async Task UpdateRecommendationWidget()
        {
            var today = DateTime.Today;
            var activity = await _db.GetActivityForDateAsync(_user.Id, today) ?? new DailyActivity();

            CurrentSteps = activity.Steps;
            CurrentWaterText = $"{activity.WaterMilliliters} мл";
            CurrentSittingText = $"{activity.SittingHours} год";

            var goal = await _db.GetLatestGoalWithStrategyAsync(_user.Id);
            string strategyName = goal.Item2?.Name ?? "Підтримка форми";
            var advice = RecommendationService.GetAdvice(activity, strategyName);

            RecommendationTitle = advice.Title;
            RecommendationMessage = advice.Message;
            RecommendationColor = advice.Color;
        }


        private async void OnAddStepsClicked(object sender, EventArgs e)
        {
            string result = await DisplayPromptAsync("Кроки", "Скільки кроків додати?", placeholder: "напр. 1500", keyboard: Keyboard.Numeric);
            if (int.TryParse(result, out int stepsToAdd))
            {
                var today = DateTime.Today;
                var activity = await _db.GetActivityForDateAsync(_user.Id, today) ?? new DailyActivity { UserId = _user.Id, Date = today };

                activity.Steps += stepsToAdd; 

                await _db.SaveActivityAsync(activity);
                await UpdateRecommendationWidget();

                if (sender is VisualElement view) await view.ScaleTo(0.9, 50).ContinueWith(t => view.ScaleTo(1.0, 50));
            }
        }

        private async void OnAddWaterClicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Додати води", "Скасувати", null, "+ 250 мл (склянка)", "+ 500 мл", "Ввести вручну");

            int waterToAdd = 0;
            if (action == "+ 250 мл (склянка)") waterToAdd = 250;
            else if (action == "+ 500 мл") waterToAdd = 500;
            else if (action == "Ввести вручну")
            {
                string res = await DisplayPromptAsync("Вода", "Введіть кількість мл:", keyboard: Keyboard.Numeric);
                int.TryParse(res, out waterToAdd);
            }

            if (waterToAdd > 0)
            {
                var today = DateTime.Today;
                var activity = await _db.GetActivityForDateAsync(_user.Id, today) ?? new DailyActivity { UserId = _user.Id, Date = today };

                activity.WaterMilliliters += waterToAdd; 

                await _db.SaveActivityAsync(activity);
                await UpdateRecommendationWidget();
            }
        }

        private async void OnSetSittingClicked(object sender, EventArgs e)
        {
            string result = await DisplayPromptAsync("Сидіння", "Скільки годин ви сиділи сьогодні всього?", placeholder: "напр. 6", keyboard: Keyboard.Numeric);
            if (int.TryParse(result, out int hours))
            {
                var today = DateTime.Today;
                var activity = await _db.GetActivityForDateAsync(_user.Id, today) ?? new DailyActivity { UserId = _user.Id, Date = today };

                activity.SittingHours = hours; 

                await _db.SaveActivityAsync(activity);
                await UpdateRecommendationWidget();
            }
        }

        private async void OnUpdateActivityClicked(object sender, EventArgs e)
        {
            string stepsStr = await DisplayPromptAsync("Активність", "Скільки кроків пройдено?", keyboard: Keyboard.Numeric);
            if (stepsStr == null) return; 

            string sitStr = await DisplayPromptAsync("Активність", "Скільки годин ви сиділи?", keyboard: Keyboard.Numeric);
            if (sitStr == null) return;

            if (int.TryParse(stepsStr, out int steps) && int.TryParse(sitStr, out int sitting))
            {
                var today = DateTime.Today;
                var activity = await _db.GetActivityForDateAsync(_user.Id, today) ?? new DailyActivity { UserId = _user.Id, Date = today };

                activity.Steps = steps;
                activity.SittingHours = sitting;

                // Зберігаємо
                await _db.SaveActivityAsync(activity);

                // Оновлюємо віджет
                await UpdateRecommendationWidget();
            }
        }



        private async void MainPage_CurrentPageChanged(object? sender, EventArgs e)
        {
            if (CurrentPage?.Title == "Головна")
            {
                await _plannerPage.InitialLoadTask;
                await RefreshMiniCalendarAsync();
                await UpdateRecommendationWidget(); 
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
                UpdateTodayWorkout(new List<TrainingPlannerPage.CalendarDay>());
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

            var temp = MiniCalendarDays.ToList();
            MiniCalendarDays.Clear();
            foreach (var item in temp) MiniCalendarDays.Add(item);
        }

        private async void OnNewGoalClicked(object sender, EventArgs e)
        {
            if (Navigation != null)
                await Navigation.PushAsync(new GoalPage(_user, _db));
        }
    }
}