using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace NutritionTrackerMAUI.Views
{
    public partial class FoodDiaryPage : ContentPage
    {
        private readonly User _user;
        private readonly SqliteDatabaseService _db;
        public ObservableCollection<FoodLogEntry> TodaysFood { get; set; } = new();
        private async void OnOpenDatabaseClicked(object sender, EventArgs e)
        {
            // Переходимо на сторінку бази, передаючи User і DB
            await Navigation.PushAsync(new FoodDatabasePage(_user, _db));
        }
        public FoodDiaryPage(User user, SqliteDatabaseService db)
        {
            InitializeComponent();
            _user = user;
            _db = db;
            FoodCollection.ItemsSource = TodaysFood;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            // 1. Завантажуємо список їжі на сьогодні
            var logs = await _db.GetFoodLogsAsync(_user.Id, DateTime.Today);
            TodaysFood.Clear();
            foreach (var log in logs) TodaysFood.Add(log);

            // 2. Отримуємо дані користувача для розрахунку норм
            var anthropometry = (await _db.GetUserDataAsync(_user.Id)).OrderByDescending(a => a.Date).FirstOrDefault();
            var (goal, strategy) = await _db.GetLatestGoalWithStrategyAsync(_user.Id);

            // 3. Розраховуємо цілі
            var targets = NutritionCalculator.CalculateTargets(_user, anthropometry, goal, strategy);

            // 4. Рахуємо скільки вже з'їли
            double eatenCals = logs.Sum(x => x.Calories);
            double eatenProt = logs.Sum(x => x.Protein);
            double eatenFat = logs.Sum(x => x.Fat);
            double eatenCarbs = logs.Sum(x => x.Carbs);

            // 5. Оновлюємо UI
            CaloriesLabel.Text = $"{eatenCals} / {targets.Calories} ккал";
            CaloriesProgress.Progress = targets.Calories > 0 ? eatenCals / targets.Calories : 0;

            ProteinLabel.Text = $"{eatenProt} / {targets.Protein}г";
            FatLabel.Text = $"{eatenFat} / {targets.Fat}г";
            CarbsLabel.Text = $"{eatenCarbs} / {targets.Carbs}г";
        }

        private async void OnAddFoodClicked(object sender, EventArgs e)
        {
            // Спрощений ввід для прикладу (в ідеалі - окрема сторінка)
            string name = await DisplayPromptAsync("Додати їжу", "Назва продукту:");
            if (string.IsNullOrWhiteSpace(name)) return;

            string calsStr = await DisplayPromptAsync("Калорії", "Кількість ккал:", keyboard: Keyboard.Numeric);
            if (!double.TryParse(calsStr, out double cals)) return;
            var newLog = new FoodLogEntry
            {
                UserId = _user.Id,
                Date = DateTime.Now,
                MealType = "Перекус", // Можна зробити вибір через ActionSheet
                Name = name,
                Calories = cals,
                Protein = cals * 0.07, // Приблизна заглушка
                Fat = cals * 0.03,
                Carbs = cals * 0.10
            };

            await _db.AddFoodLogAsync(newLog);
            await LoadDataAsync(); // Оновити екран
        }
        private async void OnFoodItemTapped(object sender, TappedEventArgs e)
        {
            // Отримуємо запис, на який натиснули
            if (e.Parameter is not FoodLogEntry selectedEntry) return;

            // Показуємо меню дій
            string action = await DisplayActionSheet(
                $"Меню: {selectedEntry.Name}",
                "Скасувати",
                "Видалити",
                "Редагувати назву",
                "Редагувати калорії");

            switch (action)
            {
                case "Видалити":
                    await DeleteEntryAsync(selectedEntry);
                    break;
                case "Редагувати назву":
                    await EditEntryNameAsync(selectedEntry);
                    break;
                case "Редагувати калорії":
                    await EditEntryCaloriesAsync(selectedEntry);
                    break;
            }
        }

        private async Task DeleteEntryAsync(FoodLogEntry entry)
        {
            bool confirm = await DisplayAlert("Видалення", $"Видалити '{entry.Name}'?", "Так", "Ні");
            if (!confirm) return;

            await _db.DeleteFoodLogAsync(entry);

            // Оновлюємо список та прогрес-бари
            await LoadDataAsync();
        }

        private async Task EditEntryNameAsync(FoodLogEntry entry)
        {
            string newName = await DisplayPromptAsync("Редагування", "Нова назва продукту:", initialValue: entry.Name);

            if (string.IsNullOrWhiteSpace(newName) || newName == entry.Name) return;

            entry.Name = newName;
            await _db.UpdateFoodLogAsync(entry);
            await LoadDataAsync();
        }

        private async void OnDietarySettingsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new DietarySettingsPage(_user, _db));
        }
        private async Task EditEntryCaloriesAsync(FoodLogEntry entry)
        {
            string newCalsStr = await DisplayPromptAsync("Редагування", "Нова калорійність:",
                                                         initialValue: entry.Calories.ToString(),
                                                         keyboard: Keyboard.Numeric);

            if (!double.TryParse(newCalsStr, out double newCals)) return;

            // Оновлюємо калорії
            entry.Calories = newCals;

            // Автоматично перераховуємо БЖВ пропорційно (якщо це була заглушка)
            // Або можна залишити старі, якщо хочете редагувати їх окремо.
            // Тут для прикладу перерахуємо за тією ж логікою, що при створенні:
            entry.Protein = newCals * 0.07;
            entry.Fat = newCals * 0.03;
            entry.Carbs = newCals * 0.10;

            await _db.UpdateFoodLogAsync(entry);
            await LoadDataAsync();
        }
    }
}