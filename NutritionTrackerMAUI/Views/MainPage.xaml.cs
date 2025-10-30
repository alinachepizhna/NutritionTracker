using Microsoft.Maui.Controls;
using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;

namespace NutritionTrackerMAUI.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly SqliteDatabaseService _db;
        private readonly User _user;

        public MainPage(User user, SqliteDatabaseService db)
        {
            InitializeComponent();
            _db = db;
            _user = user;

            LoadLastGoal();
        }

        private async void LoadLastGoal()
        {
            var lastGoal = await _db.GetLatestGoalAsync(_user.Id);
            if (lastGoal != null)
            {
                DisplayCurrentGoal(lastGoal);
            }
        }

        public void DisplayCurrentGoal(Goal goal)
        {
            CurrentGoalLabel.Text = $"Ціль: {goal.Description}";
            CurrentStrategyLabel.Text = $"Стратегія: {goal.Strategy}";
        }

        private async void OnNewGoalClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new GoalPage(_user, _db));
        }
    }
}
