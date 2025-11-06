using Microsoft.Maui.Controls;
using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;

namespace NutritionTrackerMAUI.Views;

public partial class MainPage : TabbedPage
{
    private readonly SqliteDatabaseService _db;
    private readonly User _user;

    public MainPage(User user, SqliteDatabaseService db)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _db = db ?? throw new ArgumentNullException(nameof(db));

        InitializeComponent();

        LoadLastGoal();

        // Додаємо вкладку Планування тренувань динамічно
        var plannerPage = new TrainingPlannerPage(_user, _db)
        {
            Title = "Планування",
            IconImageSource = "dumbbell.png"
        };
        Children.Add(plannerPage);
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