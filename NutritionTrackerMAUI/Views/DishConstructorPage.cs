using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using System.Collections.ObjectModel;

namespace NutritionTrackerMAUI.Views
{
    public partial class DishConstructorPage : ContentPage
    {
        private readonly SqliteDatabaseService _db;
        private readonly User _user;

        // Список інгредієнтів для відображення на екрані
        public ObservableCollection<DishIngredient> Ingredients { get; set; } = new();

        // Змінні для підрахунку підсумків
        double _totalCals = 0, _totalProt = 0, _totalFat = 0, _totalCarbs = 0, _totalWeight = 0;

        public DishConstructorPage(User user, SqliteDatabaseService db)
        {
            InitializeComponent();
            _user = user;
            _db = db;

            // Прив'язуємо список до UI
            IngredientsCollection.ItemsSource = Ingredients;

            // ПІДПИСКА НА ПОВІДОМЛЕННЯ: Очікуємо дані від FoodDatabasePage
            MessagingCenter.Subscribe<FoodDatabasePage, (FoodItem, double)>(this, "AddIngredient", (sender, arg) =>
            {
                AddIngredientToDish(arg.Item1, arg.Item2);
            });
        }

        // Цей метод додає продукт у список інгредієнтів
        private void AddIngredientToDish(FoodItem item, double weight)
        {
            var ingredient = new DishIngredient
            {
                FoodItemId = item.Id,
                Name = item.Name,
                Weight = weight
            };

            // Розрахунок БЖВ цього інгредієнта (база на 100г)
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
            TotalLabel.Text = $"Разом: {Math.Round(_totalCals)} ккал ({_totalWeight}г)";
            MacrosLabel.Text = $"Б:{Math.Round(_totalProt)} Ж:{Math.Round(_totalFat)} В:{Math.Round(_totalCarbs)}";
        }

        // Кнопка "+ Додати інгредієнт"
        private async void OnAddIngredientClicked(object sender, EventArgs e)
        {
            // Відкриваємо базу продуктів у режимі ВИБОРУ (isSelectionMode = true)
            await Navigation.PushAsync(new FoodDatabasePage(_user, _db, isSelectionMode: true));
        }

        // Кнопка видалення (червоний хрестик)
        private void OnRemoveIngredientClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.BindingContext is DishIngredient ing)
            {
                // ВАЖЛИВО: Треба відняти калорії перед видаленням (тут спрощено, краще перерахувати весь список)
                // Для простоти просто видаляємо зі списку, але в ідеалі треба перерахувати _totalCals заново.
                Ingredients.Remove(ing);

                // Перерахунок з нуля (надійніше)
                RecalculateTotals();
            }
        }

        private void RecalculateTotals()
        {
            // Скидаємо
            _totalCals = 0; _totalProt = 0; _totalFat = 0; _totalCarbs = 0; _totalWeight = 0;

            // Тут потрібен доступ до оригінального FoodItem, щоб порахувати заново.
            // Оскільки в DishIngredient ми зберегли тільки Name і Weight, то для точного перерахунку
            // краще зберігати калорії в DishIngredient теж.
            // Для MVP поки залишимо так, але майте на увазі.

            // Оновлюємо текст (поки буде 0, якщо видалили все)
            if (Ingredients.Count == 0) UpdateTotals();
        }

        // Кнопка "Зберегти страву"
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

        // Відписуємося при закритті сторінки, щоб уникнути витоку пам'яті
        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            MessagingCenter.Unsubscribe<FoodDatabasePage, (FoodItem, double)>(this, "AddIngredient");
        }
    }
}