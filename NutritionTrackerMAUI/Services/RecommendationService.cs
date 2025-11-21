using Microsoft.Maui.Graphics; // Не забудь про цей using для Colors
using NutritionTrackerMAUI.Models;

namespace NutritionTrackerMAUI.Services
{
    public static class RecommendationService
    {
        public static (string Title, string Message, Color Color) GetAdvice(DailyActivity activity, string userGoalStrategy)
        {
            // --- Константи цілей ---
            int targetSteps = 8000;
            int maxSittingHours = 6;
            int minWaterMl = 1500; // Мінімум води
            int currentHour = DateTime.Now.Hour; // Поточна година

            // 1. 🌙 ВЕЧІРНІЙ РЕЖИМ (Після 22:00)
            // Якщо вже пізно, не треба мотивувати бігати, треба спати.
            if (currentHour >= 22)
            {
                if (activity.Steps < 3000)
                {
                    return ("😴 Час відпочивати",
                            "День був малоактивним, але зараз краще вже лягати спати. Завтра почніть ранок з розминки!",
                            Colors.Indigo);
                }
                else
                {
                    return ("🌜 Гарних снів",
                            "Ви добре попрацювали сьогодні! Якісний сон — запорука відновлення.",
                            Colors.DarkSlateBlue);
                }
            }

            // 2. 📵 ПЕРЕВІРКА СИДІННЯ (Пріоритет №1 - Здоров'я спини)
            if (activity.SittingHours > maxSittingHours)
            {
                return ("📵 Встаньте розім'ятись!",
                        $"Ви сидите вже {activity.SittingHours} годин. Кровообіг сповільнився. Зробіть 5-хвилинну перерву просто зараз!",
                        Colors.OrangeRed);
            }

            // 3. 💧 ПЕРЕВІРКА ВОДИ (Гідратація)
            // Якщо води дуже мало (менше 1 літра), це критично
            if (activity.WaterMilliliters < 1000)
            {
                return ("💧 Ви зневоднені!",
                        "Ваш організм потребує води. Випийте склянку води прямо зараз, це покращить самопочуття та роботу мозку.",
                        Colors.DeepSkyBlue);
            }

            // 4. 🛑 ЗАХИСТ ВІД ПЕРЕТРЕНОВАНОСТІ
            // Якщо людина пройшла марафон або тренувалась 3 години
            if (activity.Steps > 20000 || activity.ActiveMinutes > 150)
            {
                return ("🛑 Обережно з навантаженням",
                        "Ви сьогодні справжня машина! Але не забувайте про відновлення. З'їжте достатньо білка і лягайте раніше.",
                        Colors.Purple);
            }

            // 5. 🎯 ПЕРСОНАЛІЗАЦІЯ ПІД ЦІЛЬ
            if (userGoalStrategy.Contains("Схуднення") || userGoalStrategy.Contains("Сушка"))
            {
                if (activity.Steps < targetSteps)
                {
                    int left = targetSteps - activity.Steps;
                    return ("🔥 Спалюємо калорії",
                            $"Для цілі схуднення не вистачає {left} кроків. Пройдіться сходами замість ліфта або зробіть прогулянку перед сном.",
                            Colors.Orange);
                }
                else
                {
                    return ("✅ Метаболізм працює!",
                            "Норма кроків виконана! Ви на правильному шляху до своєї ваги. Тримайте темп!",
                            Colors.Green);
                }
            }
            else if (userGoalStrategy.Contains("М'язи") || userGoalStrategy.Contains("Маса"))
            {
                // Для набору маси важливі силові тренування, а не тільки кроки
                if (activity.ActiveMinutes < 40)
                {
                    return ("💪 Стимул для росту",
                            "М'язи ростуть від навантаження. Якщо сьогодні не день залу, зробіть віджимання або планку вдома.",
                            Colors.BlueViolet);
                }
                else
                {
                    return ("🥩 Час підкріпитись",
                            "Гарне тренування! Тепер вашим м'язам потрібен будівельний матеріал. Переконайтесь, що ви з'їли свою норму білка.",
                            Colors.ForestGreen);
                }
            }

            // 6. 🚶‍♂️ ЗАГАЛЬНА АКТИВНІСТЬ (Якщо ціль "Підтримка" або не визначена)
            if (activity.Steps < 4000)
            {
                return ("🚶‍♂️ Трохи більше руху",
                        "Сьогодні ви мало рухались. Коротка прогулянка на свіжому повітрі покращить настрій.",
                        Colors.Gray);
            }

            // Додаткова порада про воду, якщо кроки в нормі, але води малувато (1-1.5л)
            if (activity.WaterMilliliters < minWaterMl)
            {
                return ("🥤 Водний баланс",
                       "З активністю все супер! Але спробуйте допити свою норму води до кінця дня.",
                       Colors.CornflowerBlue);
            }

            // Дефолтна позитивна відповідь
            return ("🌟 Ви в чудовій формі!",
                    "Ваші показники активності та води в нормі. Продовжуйте в тому ж дусі, ваш організм вам вдячний!",
                    Colors.Teal);
        }
    }
}