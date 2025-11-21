#pragma warning disable CS0618

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
        private bool _isSelectionMode;

        public FoodDatabasePage(User user, SqliteDatabaseService db, bool isSelectionMode = false)
        {
            InitializeComponent();
            _user = user;
            _db = db;
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
            var products = await _db.GetAllFoodItemsAsync();

            var dishes = await _db.GetUserDishesAsync(_user.Id);

            var dishItems = dishes.Select(d => new FoodItem
            {
                Name = d.Name,
                Category = "Готові страви",
                IsCustom = true,
                Calories = CalculatePer100g(d.TotalCalories, d.TotalWeight),
                Protein = CalculatePer100g(d.TotalProtein, d.TotalWeight),
                Fat = CalculatePer100g(d.TotalFat, d.TotalWeight),
                Carbs = CalculatePer100g(d.TotalCarbs, d.TotalWeight)
            }).ToList();

            _allItems = new List<FoodItem>();
            _allItems.AddRange(dishItems);
            _allItems.AddRange(products);

            ApplyFilter();
        }

        private double CalculatePer100g(double totalValue, double totalWeight)
        {
            if (totalWeight <= 0) return 0;
            return Math.Round((totalValue / totalWeight) * 100, 1);
        }

        private async void OnCreateDishClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new DishConstructorPage(_user, _db));
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

            foreach (var view in CategoryStack.Children)
            {
                if (view is Button btn) btn.BackgroundColor = Colors.LightGray;
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

            bool isCalValid = double.TryParse(calStr, out double cal);
            bool isProtValid = double.TryParse(protStr, out double prot);
            bool isFatValid = double.TryParse(fatStr, out double fat);
            bool isCarbValid = double.TryParse(carbStr, out double carb);

            if (!isCalValid || !isProtValid || !isFatValid || !isCarbValid)
            {
                await DisplayAlert("Помилка", "Будь ласка, введіть коректні цифри для всіх полів (БЖВ). Використовуйте кому або крапку.", "OK");
                return;
            }

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

            try
            {
                await _db.AddFoodItemAsync(newItem);
                await DisplayAlert("Успіх", $"Продукт '{name}' збережено в базу!", "OK");

                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Помилка БД", ex.Message, "OK");
            }
        }

        private async void OnProductTapped(object sender, TappedEventArgs e)
        {
            if (e.Parameter is not FoodItem item) return;

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

            string weightStr = await DisplayPromptAsync(item.Name, "Введіть вагу (грам):", keyboard: Keyboard.Numeric);

            if (double.TryParse(weightStr, out double weight))
            {
                if (_isSelectionMode)
                {
                    MessagingCenter.Send<object, (FoodItem, double)>(this, "AddIngredient", (item, weight));
                    await Navigation.PopAsync();
                    return;
                }

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