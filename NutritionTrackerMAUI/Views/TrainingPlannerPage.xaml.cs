using Microsoft.Maui.Controls;
using NutritionTrackerMAUI.Models;
using NutritionTrackerMAUI.Services;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.ObjectModel;

namespace NutritionTrackerMAUI.Views;

public partial class TrainingPlannerPage : ContentPage
{
    private readonly SqliteDatabaseService _db;
    private readonly User _user;

    private Goal? _goal;
    private Strategy? _strategy;

    public ObservableCollection<CalendarDay> CalendarDays { get; set; } = new();

    // Конструктор з параметрами (обов'язково передаємо User та Db)
    public TrainingPlannerPage(User user, SqliteDatabaseService db)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _db = db ?? throw new ArgumentNullException(nameof(db));

        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadGoalAndStrategyAsync();
    }

    private async System.Threading.Tasks.Task LoadGoalAndStrategyAsync()
    {
        _goal = await _db.GetLatestGoalAsync(_user.Id);
        if (_goal == null)
        {
            await DisplayAlert("ℹ️", "У вас ще немає збережених цілей.", "OK");
            return;
        }

        _strategy = await _db.GetStrategyByIdAsync(_goal.StrategyId);

        GoalLabel.Text = _goal?.Description ?? "Ціль не задана";
        StrategyLabel.Text = _strategy?.Name ?? "Невідомо";

        GenerateCalendar(_goal.StartDate, _goal.EndDate);
    }

    private void GenerateCalendar(DateTime startDate, DateTime endDate)
    {
        CalendarDays.Clear();
        int totalDays = (endDate - startDate).Days + 1;

        for (int i = 0; i < totalDays; i++)
        {
            var date = startDate.AddDays(i);
            bool isTrainingDay = IsTrainingDay(date);

            CalendarDays.Add(new CalendarDay
            {
                Date = date,
                DateText = date.Day.ToString(),
                BackgroundColor = isTrainingDay ? Colors.DarkRed : Colors.Gray,
                WorkoutType = isTrainingDay ? "Руки" : "Відпочинок"
            });
        }

        CalendarCollection.ItemsSource = CalendarDays;
    }

    private bool IsTrainingDay(DateTime date)
    {
        if (_strategy == null || _goal == null)
            return false;

        return _strategy.Name switch
        {
            "Повільно" => (date - _goal.StartDate).Days % 3 == 0,
            "Помірно" => (date - _goal.StartDate).Days % 2 == 0,
            "Агресивно" => true,
            _ => (date - _goal.StartDate).Days % 2 == 0,
        };
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_goal == null || _strategy == null)
        {
            await DisplayAlert("Помилка", "Не знайдено ціль або стратегію.", "OK");
            return;
        }

        foreach (var day in CalendarDays)
        {
            var plan = new TrainingPlan
            {
                UserId = _user.Id,
                GoalId = _goal.Id,
                StrategyId = _strategy.Id,
                DayOfWeek = day.Date.DayOfWeek.ToString(),
                WorkoutType = day.WorkoutType
            };
            await _db.Database.InsertAsync(plan);
        }

        await DisplayAlert("✅ Успіх", "План тренувань збережено!", "OK");
    }
}

public class CalendarDay
{
    public DateTime Date { get; set; }
    public string DateText { get; set; } = string.Empty;
    public Color BackgroundColor { get; set; } = Colors.Gray;
    public string WorkoutType { get; set; } = "Відпочинок";
}
