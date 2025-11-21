using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using System.Collections.ObjectModel;

namespace NutritionTrackerMAUI.Views
{
    public partial class FoodDatabasePage : ContentPage
    {
        private readonly SqliteDatabaseService _db;
        private readonly User _user;
        private List<FoodItem> _allItems = new();
        public ObservableCollection<FoodItem> FilteredItems { get; set; } = new();
        private string _selectedCategory = "Всі";
        private UserDietarySettings _dietSettings;

        // Поле для збереження режиму
        private bool _isSelectionMode;

        // ✅ ВИПРАВЛЕНИЙ КОНСТРУКТОР: Додано параметр isSelectionMode = false
        public FoodDatabasePage(User user, SqliteDatabaseService db, bool isSelectionMode = false)
        {
            InitializeComponent();
            _user = user;
            _db = db;

            // ✅ Зберігаємо передане значення
            _isSelectionMode = isSelectionMode;

            ProductsCollection.ItemsSource = FilteredItems;
            CreateCategoryChips();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _db.SeedDatabaseAsync();
            _dietSettings = await _db.GetDietarySettingsAsync(_user.Id);
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            _allItems = await _db.GetAllFoodItemsAsync();
            ApplyFilter();
        }

        private void CreateCategoryChips()
        {
            string[] categories = { "Всі", "М'ясо", "Риба", "Крупи", "Овочі", "Фрукти", "Молочні", "Готові страви", "Своє" };

            foreach (var cat in categories)
            {
                var btn = new Button
                {
                    Text = cat,
                    FontSize = 12,
                    HeightRequest = 35,
                    CornerRadius = 15,
                    Padding = new Thickness(10, 0),
                    BackgroundColor = cat == "Всі" ? Colors.DarkRed : Colors.LightGray,
                    TextColor = Colors.White
                };

                btn.Clicked += (s, e) => OnCategoryClicked(cat, btn);
                CategoryStack.Children.Add(btn);
            }
        }

        private void OnCategoryClicked(string category, Button clickedBtn)
        {
            _selectedCategory = category;

            foreach (Button btn in CategoryStack.Children)
            {
                btn.BackgroundColor = Colors.LightGray;
            }
            clickedBtn.BackgroundColor = Colors.DarkRed;

            ApplyFilter();
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var query = _allItems.AsEnumerable();

            var searchText = ProductSearch.Text?.ToLower() ?? "";
            if (_selectedCategory == "Своє")
                query = query.Where(x => x.IsCustom);
            else if (_selectedCategory != "Всі")
                query = query.Where(x => x.Category == _selectedCategory);

            if (!string.IsNullOrWhiteSpace(searchText))
                query = query.Where(x => x.Name.ToLower().Contains(searchText));

            FilteredItems.Clear();
            foreach (var item in query)
            {
                FilteredItems.Add(item);
            }
        }

        private async void OnAddCustomProductClicked(object sender, EventArgs e)
        {
            string name = await DisplayPromptAsync("Новий продукт", "Назва продукту:");
            if (string.IsNullOrWhiteSpace(name)) return;

            string calStr = await DisplayPromptAsync("Калорійність", "Ккал на 100г:", keyboard: Keyboard.Numeric);
            string protStr = await DisplayPromptAsync("Білки", "Білки на 100г:", keyboard: Keyboard.Numeric);
            string fatStr = await DisplayPromptAsync("Жири", "Жири на 100г:", keyboard: Keyboard.Numeric);
            string carbStr = await DisplayPromptAsync("Вуглеводи", "Вуглеводи на 100г:", keyboard: Keyboard.Numeric);

            if (double.TryParse(calStr, out double cal) &&
                double.TryParse(protStr, out double prot) &&
                double.TryParse(fatStr, out double fat) &&
                double.TryParse(carbStr, out double carb))
            {
                var newItem = new FoodItem
                {
                    Name = name,
                    Category = "Своє",
                    Calories = cal,
                    Protein = prot,
                    Fat = fat,
                    Carbs = carb,
                    IsCustom = true
                };

                await _db.AddFoodItemAsync(newItem);
                await DisplayAlert("Успіх", "Продукт додано в базу!", "OK");
                await LoadDataAsync();
            }
        }

        private async void OnProductTapped(object sender, TappedEventArgs e)
        {
            // 1. Отримуємо продукт
            if (e.Parameter is not FoodItem item) return;

            // 2. Перевірка на алергени
            List<string> warnings = new List<string>();

            if (_dietSettings != null)
            {
                if (_dietSettings.AvoidGluten && item.HasGluten) warnings.Add("ГЛЮТЕН");
                if (_dietSettings.AvoidLactose && item.HasLactose) warnings.Add("ЛАКТОЗУ");
                if (_dietSettings.AvoidNuts && item.HasNuts) warnings.Add("ГОРІХИ");
                if (_dietSettings.AvoidSugar && item.HasSugar) warnings.Add("ЦУКОР");
            }

            if (warnings.Count > 0)
            {
                string warningText = string.Join(", ", warnings);
                bool proceed = await DisplayAlert(
                    "⚠️ ПОПЕРЕДЖЕННЯ",
                    $"Цей продукт містить {warningText}, що суперечить вашій дієті.\n\nВи точно хочете його додати?",
                    "Так, додати",
                    "Ні, скасувати");

                if (!proceed) return;
            }

            // 3. Запитуємо вагу
            string weightStr = await DisplayPromptAsync(item.Name, "Введіть вагу (грам):", keyboard: Keyboard.Numeric);

            if (double.TryParse(weightStr, out double weight))
            {
                // ✅ ЛОГІКА КОНСТРУКТОРА СТРАВ
                if (_isSelectionMode)
                {
                    // Повертаємо дані назад через MessagingCenter
                    MessagingCenter.Send(this, "AddIngredient", (item, weight));
                    await Navigation.PopAsync();
                    return; // Виходимо, щоб не додавати в щоденник
                }

                // --- ЗВИЧАЙНИЙ РЕЖИМ (ЗАПИС В ЩОДЕННИК) ---
                string mealType = await DisplayActionSheet("Куди додати?", "Скасувати", null,
                    "Сніданок", "Обід", "Вечеря", "Перекус");

                if (mealType == "Скасувати" || mealType == null) return;

                double factor = weight / 100.0;
                var logEntry = new FoodLogEntry
                {
                    UserId = _user.Id,
                    Date = DateTime.Now,
                    MealType = mealType,
                    Name = $"{item.Name} ({weight}г)",
                    Calories = Math.Round(item.Calories * factor),
                    Protein = Math.Round(item.Protein * factor),
                    Fat = Math.Round(item.Fat * factor),
                    Carbs = Math.Round(item.Carbs * factor)
                };

                await _db.AddFoodLogAsync(logEntry);
                await DisplayAlert("Готово", $"{item.Name} додано в {mealType}", "OK");
                await Navigation.PopAsync();
            }
        }
    }
}