using System.Collections.ObjectModel;
using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;

namespace NutritionTrackerMAUI.Views
{
    public class HabitDisplayItem : BindableObject
    {
        public Habit Habit { get; set; }

        private bool _isCompleted;
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                _isCompleted = value;
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(CardColor)); 
                OnPropertyChanged(nameof(TextColor)); 
            }
        }

        private int _streak;
        public int Streak
        {
            get => _streak;
            set { _streak = value; OnPropertyChanged(); }
        }

        public string FrequencyText
        {
            get
            {
                if (Habit.FrequencyType == 0) return "Щодня";
                if (Habit.FrequencyType == 1) return "Декілька разів";
                if (Habit.FrequencyType == 2)
                {
                    if (Habit.TargetDays.Contains("Monday")) return "По буднях";
                    if (Habit.TargetDays.Contains("Saturday")) return "Вихідні";
                    return "За графіком";
                }
                return "";
            }
        }

        public Color CardColor => IsCompleted ? Color.FromArgb("#E8F5E9") : Colors.White; // Світло-зелений, якщо виконано
        public Color TextColor => IsCompleted ? Color.FromArgb("#2E7D32") : Colors.Black;
    }

        public partial class HabitsPage : ContentPage
        {
            private readonly SqliteDatabaseService _db;
            private readonly User _user;
            public ObservableCollection<HabitDisplayItem> HabitsCollection { get; set; } = new();

            private double _progressValue;
            public double ProgressValue
            {
                get => _progressValue;
                set { _progressValue = value; OnPropertyChanged(); }
            }

            private string _progressText;
            public string ProgressText
            {
                get => _progressText;
                set { _progressText = value; OnPropertyChanged(); }
            }

            public HabitsPage(User user, SqliteDatabaseService db)
            {
                InitializeComponent();
                _user = user;
                _db = db;
                BindingContext = this;
            }

            protected override async void OnAppearing()
            {
                base.OnAppearing();
                await LoadHabits();
            }

            private async Task LoadHabits()
            {
                HabitsCollection.Clear();
                var habits = await _db.GetUserHabitsAsync(_user.Id);

                foreach (var h in habits)
                {
                    var isDone = await _db.IsHabitCompletedTodayAsync(h.Id);
                    var streak = await _db.GetHabitStreakAsync(h.Id);

                    HabitsCollection.Add(new HabitDisplayItem
                    {
                        Habit = h,
                        IsCompleted = isDone,
                        Streak = streak
                    });
                }
                UpdateDailyProgress(); 
            }

            private void UpdateDailyProgress()
            {
                if (HabitsCollection.Count == 0)
                {
                    ProgressValue = 0;
                    ProgressText = "Немає звичок";
                    return;
                }

                int completed = HabitsCollection.Count(h => h.IsCompleted);
                int total = HabitsCollection.Count;

                ProgressValue = (double)completed / total;
                ProgressText = $"Виконано: {completed} з {total}";
            }

            private async void OnAddHabitClicked(object sender, EventArgs e)
            {
                string title = await DisplayPromptAsync("Нова звичка", "Назва (напр. Біг):");
                if (string.IsNullOrWhiteSpace(title)) return;

                string desc = await DisplayPromptAsync("Опис", "Короткий опис (напр. 20 хв у парку):");

                string freqAction = await DisplayActionSheet($"Графік для '{title}'?", "Скасувати", null,
                    "Щодня", "Тільки будні", "Тільки вихідні");

                if (freqAction == "Скасувати" || freqAction == null) return;

                int freqType = 0;
                string targetDays = "";

                if (freqAction == "Тільки будні") { freqType = 2; targetDays = "Monday,Tuesday,Wednesday,Thursday,Friday"; }
                else if (freqAction == "Тільки вихідні") { freqType = 2; targetDays = "Saturday,Sunday"; }

                var habit = new Habit
                {
                    UserId = _user.Id,
                    Title = title,
                    Description = desc, 
                    Icon = "✨", 
                    FrequencyType = freqType,
                    TargetDays = targetDays
                };

                await _db.SaveHabitAsync(habit);
                await LoadHabits();
            }

            private async void OnToggleHabit(object sender, EventArgs e)
            {
                if (sender is VisualElement view && view.BindingContext is HabitDisplayItem item)
                {
                    await view.ScaleTo(0.8, 100, Easing.Linear);
                    await view.ScaleTo(1.2, 100, Easing.Linear);
                    await view.ScaleTo(1.0, 100, Easing.Linear);

                    await _db.ToggleHabitAsync(item.Habit.Id, DateTime.Today);

                    item.IsCompleted = !item.IsCompleted;

                    if (item.IsCompleted)
                    {
                        item.Streak++;
                        CheckAchievements(item.Streak, item.Habit.Title);
                    }
                    else
                    {
                        item.Streak = Math.Max(0, item.Streak - 1);
                    }

                    UpdateDailyProgress(); 
                }
            }

            private async void CheckAchievements(int streak, string habitName)
            {
                if (streak == 7)
                    await DisplayAlert("🏆 Тиждень сили!", $"Ви виконуєте '{habitName}' вже 7 днів поспіль! Так тримати!", "Ура!");
                else if (streak == 30)
                    await DisplayAlert("🥇 Місяць дисципліни!", $"30 днів звички '{habitName}'. Це вже спосіб життя!", "Круто!");
                else if (streak == 100)
                    await DisplayAlert("💎 Легенда!", $"100 днів поспіль! '{habitName}' тепер частина вашого ДНК.", "Я машина!");
            }

            private async void OnDeleteHabit(object sender, EventArgs e)
            {
                if (sender is MenuItem menu && menu.BindingContext is HabitDisplayItem item)
                {
                    bool confirm = await DisplayAlert("Видалити?", $"Видалити звичку '{item.Habit.Title}'?", "Так", "Ні");
                    if (confirm)
                    {
                        await _db.DeleteHabitAsync(item.Habit);
                        HabitsCollection.Remove(item);
                        UpdateDailyProgress(); 
                    }
                }
            }
        }
    }