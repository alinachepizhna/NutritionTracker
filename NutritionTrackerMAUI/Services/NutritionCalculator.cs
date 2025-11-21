using NutritionTrackerMAUI.Models;

namespace NutritionTrackerMAUI.Services
{
    public static class NutritionCalculator
    {
        public class DailyTargets
        {
            public double Calories { get; set; }
            public double Protein { get; set; }
            public double Fat { get; set; }
            public double Carbs { get; set; }
        }

        public static DailyTargets CalculateTargets(User user, AnthropometricData data, Goal goal, Strategy strategy)
        {
            // Перевірка на null, щоб програма не "впала", якщо даних ще немає
            if (user == null || data == null || goal == null)
                return new DailyTargets { Calories = 2000, Protein = 150, Fat = 60, Carbs = 220 };

            // 1. Базовий обмін речовин (BMR) - Формула Міффліна-Сан-Жеора
            double bmr;

            if (data.Gender == "Чоловіча" || data.Gender == "Male")
            {
                // Формула для чоловіків: (10 × вага) + (6.25 × зріст) − (5 × вік) + 5
                bmr = (10 * data.Weight) + (6.25 * data.Height) - (5 * data.Age) + 5;
            }
            else
            {
                // Формула для жінок: (10 × вага) + (6.25 × зріст) − (5 × вік) − 161
                bmr = (10 * data.Weight) + (6.25 * data.Height) - (5 * data.Age) - 161;
            }

            // 2. Коефіцієнт активності (TDEE)
            // Беремо середній 1.375 (легка активність)
            double tdee = bmr * 1.375;

            // 3. Коригування під ціль та стратегію
            double adjustmentFactor = 0;

            // Перевірка на null для стратегії (на випадок помилки бази)
            string strategyName = strategy?.Name ?? "Помірно";

            if (goal.Description.Contains("Схудн", StringComparison.OrdinalIgnoreCase)) // Схуднути/Схуднення
            {
                adjustmentFactor = strategyName switch
                {
                    "Агресивно" => -0.25, // Дефіцит 25%
                    "Помірно" => -0.15,   // Дефіцит 15%
                    "Повільно" => -0.10,  // Дефіцит 10%
                    _ => -0.15
                };
            }
            else if (goal.Description.Contains("Набрати", StringComparison.OrdinalIgnoreCase) ||
                     goal.Description.Contains("Маса", StringComparison.OrdinalIgnoreCase))
            {
                adjustmentFactor = strategyName switch
                {
                    "Агресивно" => 0.20, // Профіцит 20%
                    "Помірно" => 0.10,   // Профіцит 10%
                    "Повільно" => 0.05,  // Профіцит 5%
                    _ => 0.10
                };
            }

            double targetCalories = tdee * (1 + adjustmentFactor);

            // 4. Розрахунок БЖВ (Пропорція 30% білок / 30% жири / 40% вуглеводи)
            return new DailyTargets
            {
                Calories = Math.Round(targetCalories),
                Protein = Math.Round((targetCalories * 0.30) / 4), // 1г білка = 4 ккал
                Fat = Math.Round((targetCalories * 0.30) / 9),     // 1г жиру = 9 ккал
                Carbs = Math.Round((targetCalories * 0.40) / 4)    // 1г вуглеводів = 4 ккал
            };

        }

    }
}