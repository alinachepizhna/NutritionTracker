using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using System.Collections.ObjectModel;

namespace NutritionTrackerMAUI.Views
{
    public partial class DishConstructorPage : ContentPage
    {
        private readonly SqliteDatabaseService _db;
        private readonly User _user;

        // Список інгредієнтів (Public Property для Binding)
        public ObservableCollection<DishIngredient> Ingredients { get; set; } = new();

        double _totalCals = 0, _totalProt = 0, _totalFat = 0, _totalCarbs = 0, _totalWeight = 0;

        public DishConstructorPage(User user, SqliteDatabaseService db)
        {
            InitializeComponent();
            _user = user;
            _db = db;

            this.BindingContext = this;

            MessagingCenter.Subscribe<object, (FoodItem, double)>(this, "AddIngredient", (sender, arg) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AddIngredientToDish(arg.Item1, arg.Item2);
                });
            });
        }

        private void AddIngredientToDish(FoodItem item, double weight)
        {
            var ingredient = new DishIngredient
            {
                FoodItemId = item.Id,
                Name = item.Name,
                Weight = weight
            };

            double factor = weight / 100.0;

            _totalCals += item.Calories * factor;
            _totalProt += item.Protein * factor;
            _totalFat += item.Fat * factor;
            _totalCarbs += item.Carbs * factor;
            _totalWeight += weight;

            Ingredients.Add(ingredient);
            UpdateTotals();
        }

        private void UpdateTotals()
        {
            if (TotalLabel != null)
                TotalLabel.Text = $"Разом: {Math.Round(_totalCals)} ккал ({_totalWeight}г)";

            if (MacrosLabel != null)
                MacrosLabel.Text = $"Б:{Math.Round(_totalProt)} Ж:{Math.Round(_totalFat)} В:{Math.Round(_totalCarbs)}";
        }

        private async void OnAddIngredientClicked(object sender, EventArgs e)
        {
            // Відкриваємо базу в режимі вибору
            await Navigation.PushAsync(new FoodDatabasePage(_user, _db, isSelectionMode: true));
        }

        private void OnRemoveIngredientClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is DishIngredient ing)
            {
                Ingredients.Remove(ing);

                _totalWeight -= ing.Weight;
                if (Ingredients.Count == 0)
                {
                    _totalCals = 0; _totalProt = 0; _totalFat = 0; _totalCarbs = 0; _totalWeight = 0;
                }

                UpdateTotals();
            }
        }

        private async void OnSaveDishClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DishNameEntry.Text))
            {
                await DisplayAlert("Помилка", "Введіть назву страви", "OK");
                return;
            }
            if (Ingredients.Count == 0)
            {
                await DisplayAlert("Помилка", "Додайте хоча б один інгредієнт", "OK");
                return;
            }

            var dish = new Dish
            {
                UserId = _user.Id,
                Name = DishNameEntry.Text,
                TotalCalories = Math.Round(_totalCals),
                TotalProtein = Math.Round(_totalProt),
                TotalFat = Math.Round(_totalFat),
                TotalCarbs = Math.Round(_totalCarbs),
                TotalWeight = Math.Round(_totalWeight)
            };

            await _db.SaveDishAsync(dish, Ingredients.ToList());
            await DisplayAlert("Успіх", "Страву збережено!", "OK");
            await Navigation.PopAsync();
        }
    }
}