using SQLite;
using NutritionTrackerMAUI.Models;
using System.IO;

namespace NutritionTrackerMAUI.Services
{
    public class SqliteDatabaseService
    {
        private readonly SQLiteAsyncConnection _database;

        public SqliteDatabaseService()
        {
            string folderPath = @"D:\курсач\XamarinProjects";
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string dbPath = Path.Combine(folderPath, "nutrition.db3");

            _database = new SQLiteAsyncConnection(dbPath);

            // --- Створення таблиць ---
            _database.CreateTableAsync<User>().Wait();
            _database.CreateTableAsync<AnthropometricData>().Wait();
            _database.CreateTableAsync<Strategy>().Wait(); // Таблиця стратегій
            _database.CreateTableAsync<Goal>().Wait();     // Таблиця цілей
            _database.CreateTableAsync<TrainingPlan>().Wait();
            _database.CreateTableAsync<UserWorkoutProgram>().Wait();
            _database.CreateTableAsync<UserCurrentProgram>().Wait();
            _database.CreateTableAsync<FoodLogEntry>().Wait();
            _database.CreateTableAsync<FoodItem>().Wait();
            _database.CreateTableAsync<UserDietarySettings>().Wait();
            _database.CreateTableAsync<Dish>().Wait();
            _database.CreateTableAsync<DishIngredient>().Wait();
            _database.CreateTableAsync<Habit>().Wait();
            _database.CreateTableAsync<HabitLog>().Wait();
        }

        // ===============================
        // 🧩 --- КОРИСТУВАЧІ ---
        // ===============================
        public Task<List<Dish>> GetUserDishesAsync(int userId) =>
    _database.Table<Dish>().Where(d => d.UserId == userId).ToListAsync();

        // Збереження страви разом з інгредієнтами
        public async Task SaveDishAsync(Dish dish, List<DishIngredient> ingredients)
        {
            await _database.InsertAsync(dish); // Спочатку зберігаємо страву, щоб отримати ID

            foreach (var ing in ingredients)
            {
                ing.DishId = dish.Id; // Прив'язуємо інгредієнт до ID страви
            }
            await _database.InsertAllAsync(ingredients);
        }

        public async Task DeleteDishAsync(Dish dish)
        {
            // Видаляємо інгредієнти
            var ingredients = await _database.Table<DishIngredient>().Where(i => i.DishId == dish.Id).ToListAsync();
            foreach (var i in ingredients) await _database.DeleteAsync(i);

            // Видаляємо саму страву
            await _database.DeleteAsync(dish);
        }
        public Task<int> AddUserAsync(User user) => _database.InsertAsync(user);

        public Task<User?> GetUserAsync(string firstName, string lastName) =>
            _database.Table<User>()
                     .Where(u => u.FirstName == firstName && u.LastName == lastName)
                     .FirstOrDefaultAsync();


        // ===============================
        // 📏 --- АНТРОПОМЕТРІЯ ---
        // ===============================

        public Task<int> AddAnthropometricDataAsync(AnthropometricData data) =>
            _database.InsertAsync(data);

        public Task<List<AnthropometricData>> GetUserDataAsync(int userId) =>
            _database.Table<AnthropometricData>()
                     .Where(d => d.UserId == userId)
                     .ToListAsync();


        // ===============================
        // 🎯 --- ЦІЛІ ---
        // ===============================

        public Task<int> AddGoalAsync(Goal goal) => _database.InsertAsync(goal);

        public Task<Goal?> GetLatestGoalAsync(int userId) =>
            _database.Table<Goal>()
                     .Where(g => g.UserId == userId)
                     .OrderByDescending(g => g.Id)
                     .FirstOrDefaultAsync();

        // Отримати ціль разом зі стратегією
        public async Task<(Goal?, Strategy?)> GetLatestGoalWithStrategyAsync(int userId)
        {
            var goal = await GetLatestGoalAsync(userId);
            if (goal == null)
                return (null, null);

            var strategy = await GetStrategyByIdAsync(goal.StrategyId);
            return (goal, strategy);
        }


        // ===============================
        // 🧠 --- СТРАТЕГІЇ ---
        // ===============================

        public Task<int> AddStrategyAsync(Strategy strategy) => _database.InsertAsync(strategy);

        public Task<List<Strategy>> GetAllStrategiesAsync() =>
            _database.Table<Strategy>().ToListAsync();

        public Task<Strategy?> GetStrategyByIdAsync(int id) =>
            _database.Table<Strategy>()
                     .Where(s => s.Id == id)
                     .FirstOrDefaultAsync();

        public async Task<Strategy?> GetStrategyByNameAsync(string name)
        {
            return await _database.Table<Strategy>()
                                  .Where(s => s.Name == name)
                                  .FirstOrDefaultAsync();
        }
        // Отримати всі цілі (для заповнення списку GoalPicker)
        public Task<List<Goal>> GetAllGoalsAsync()
        {
            return _database.Table<Goal>().ToListAsync();
        }

        // Отримати всі стратегії, які належать конкретній цілі
        public Task<List<Strategy>> GetStrategiesForGoalAsync(int goalId)
        {
            return _database.Table<Strategy>()
                            .Where(s => s.GoalId == goalId)
                            .ToListAsync();
        }

        // Доступ до бази SQLite (для TrainingService)
        public SQLiteAsyncConnection Database => _database;
        public SQLiteAsyncConnection GetDatabase()
        {
            return _database;
        }
        // Отримати останню ціль користувача (для TrainingPlannerPage)
        public async Task<Goal?> GetLastGoalByUserAsync(int userId)
        {
            return await _database.Table<Goal>()
                .Where(g => g.UserId == userId)
                .OrderByDescending(g => g.Id)
                .FirstOrDefaultAsync();
        }
        public Task<List<TrainingPlan>> GetTrainingsByUserAsync(int userId)
        {
            return _database.Table<TrainingPlan>()
                            .Where(t => t.UserId == userId)
                            .ToListAsync();
        }
        // ===============================
        // 📅 --- ПОТОЧНА ПРОГРАМА КОРИСТУВАЧА ---
        // ===============================

        // Отримати останню обрану програму для поточної цілі користувача
        public Task<UserCurrentProgram?> GetUserCurrentProgramAsync(int userId, int goalId) =>
            _database.Table<UserCurrentProgram>()
                     .Where(p => p.UserId == userId && p.GoalId == goalId)
                     .FirstOrDefaultAsync();

        // Зберегти або оновити поточну обрану програму
        public async Task SaveUserCurrentProgramAsync(int userId, int goalId, string programName)
        {
            var currentProgram = await GetUserCurrentProgramAsync(userId, goalId);

            if (currentProgram == null)
            {
                currentProgram = new UserCurrentProgram
                {
                    UserId = userId,
                    GoalId = goalId,
                    ProgramName = programName,
                    LastSelectedDate = DateTime.Now
                };
                await _database.InsertAsync(currentProgram);
            }
            else
            {
                currentProgram.ProgramName = programName;
                currentProgram.LastSelectedDate = DateTime.Now;
                await _database.UpdateAsync(currentProgram);
            }
        }
        public Task<int> UpdateFoodLogAsync(FoodLogEntry entry) => _database.UpdateAsync(entry);
        public Task<int> DeleteFoodLogAsync(FoodLogEntry entry) => _database.DeleteAsync(entry);
        public Task<int> AddFoodLogAsync(FoodLogEntry entry) => _database.InsertAsync(entry);

        public Task<List<FoodLogEntry>> GetFoodLogsAsync(int userId, DateTime date)
        {
            // Отримуємо записи тільки за конкретну дату
            var startOfDay = date.Date;
            var endOfDay = date.Date.AddDays(1).AddTicks(-1);

            return _database.Table<FoodLogEntry>()
                            .Where(f => f.UserId == userId && f.Date >= startOfDay && f.Date <= endOfDay)
                            .ToListAsync();
        }
        public Task<List<FoodItem>> GetAllFoodItemsAsync() =>
    _database.Table<FoodItem>().ToListAsync();

        public Task<int> AddFoodItemAsync(FoodItem item) =>
            _database.InsertAsync(item);

        public Task<int> DeleteFoodItemAsync(FoodItem item) =>
            _database.DeleteAsync(item);

        // Метод для початкового наповнення (Сідінг бази)
        public async Task<UserDietarySettings> GetDietarySettingsAsync(int userId)
        {
            var settings = await _database.Table<UserDietarySettings>()
                                    .Where(s => s.UserId == userId)
                                    .FirstOrDefaultAsync();

            // Якщо налаштувань ще немає, створюємо дефолтні
            if (settings == null)
            {
                settings = new UserDietarySettings { UserId = userId };
                await _database.InsertAsync(settings);
            }
            return settings;
        }

        public Task<int> SaveDietarySettingsAsync(UserDietarySettings settings) =>
            _database.UpdateAsync(settings);

        // --- Оновлений Seeding (оновіть існуючий метод) ---
        public async Task SeedDatabaseAsync()
        {
            var count = await _database.Table<FoodItem>().CountAsync();
            if (count == 0)
            {
                var initialFoods = new List<FoodItem>
        {
            // --- Глютен ---
            new FoodItem { Name = "Вівсянка", Category = "Крупи", Calories = 88, Protein = 3, Fat = 1.7, Carbs = 15, HasGluten = true },
            new FoodItem { Name = "Макарони", Category = "Крупи", Calories = 157, Protein = 5.8, Fat = 0.9, Carbs = 30, HasGluten = true },
            new FoodItem { Name = "Хліб білий", Category = "Крупи", Calories = 265, Protein = 9, Fat = 3.2, Carbs = 49, HasGluten = true },
            
            // --- Без глютену ---
            new FoodItem { Name = "Гречка", Category = "Крупи", Calories = 101, Protein = 3.6, Fat = 2.2, Carbs = 17.1, HasGluten = false },
            new FoodItem { Name = "Рис білий", Category = "Крупи", Calories = 116, Protein = 2.2, Fat = 0.5, Carbs = 24.9, HasGluten = false },

            // --- Лактоза ---
            new FoodItem { Name = "Молоко 2.5%", Category = "Молочні", Calories = 52, Protein = 2.8, Fat = 2.5, Carbs = 4.7, HasLactose = true },
            new FoodItem { Name = "Сир кисломолочний", Category = "Молочні", Calories = 121, Protein = 17.2, Fat = 5, Carbs = 1.8, HasLactose = true },
            
            // --- Горіхи ---
            new FoodItem { Name = "Волоський горіх", Category = "Горіхи", Calories = 654, Protein = 15, Fat = 65, Carbs = 7, HasNuts = true },
            new FoodItem { Name = "Арахіс", Category = "Горіхи", Calories = 567, Protein = 26, Fat = 49, Carbs = 16, HasNuts = true },

            // --- Цукор / Фрукти ---
            new FoodItem { Name = "Яблуко", Category = "Фрукти", Calories = 52, Protein = 0.3, Fat = 0.2, Carbs = 11.4, HasSugar = true },
            new FoodItem { Name = "Банан", Category = "Фрукти", Calories = 96, Protein = 1.5, Fat = 0.5, Carbs = 21, HasSugar = true },

            // --- М'ясо / Риба (Чисті) ---
            new FoodItem { Name = "Куряче філе", Category = "М'ясо", Calories = 113, Protein = 23.6, Fat = 1.9, Carbs = 0.4 },
            new FoodItem { Name = "Яловичина", Category = "М'ясо", Calories = 187, Protein = 18.9, Fat = 12.4, Carbs = 0 },
            new FoodItem { Name = "Лосось (запечений)", Category = "Риба", Calories = 206, Protein = 22, Fat = 12, Carbs = 0 },
            
            // --- Овочі ---
            new FoodItem { Name = "Огірок", Category = "Овочі", Calories = 15, Protein = 0.8, Fat = 0.1, Carbs = 3 },
            new FoodItem { Name = "Помідор", Category = "Овочі", Calories = 20, Protein = 1.1, Fat = 0.2, Carbs = 3.7 },
            new FoodItem { Name = "Картопля", Category = "Овочі", Calories = 82, Protein = 2, Fat = 0.4, Carbs = 16.7 }
        };

                await _database.InsertAllAsync(initialFoods);
            }
        }
        // --- ЗВИЧКИ ---

        public Task<List<Habit>> GetUserHabitsAsync(int userId) =>
            _database.Table<Habit>().Where(h => h.UserId == userId).ToListAsync();

        public Task<int> SaveHabitAsync(Habit habit) => _database.InsertAsync(habit);

        public Task<int> DeleteHabitAsync(Habit habit)
        {
            // Видаляємо і історію
            var logs = _database.Table<HabitLog>().Where(l => l.HabitId == habit.Id).ToListAsync().Result;
            foreach (var log in logs) _database.DeleteAsync(log);
            return _database.DeleteAsync(habit);
        }

        // Перевірити, чи виконана звичка сьогодні
        public async Task<bool> IsHabitCompletedTodayAsync(int habitId)
        {
            var today = DateTime.Today;
            var log = await _database.Table<HabitLog>()
                                     .Where(l => l.HabitId == habitId && l.Date == today)
                                     .FirstOrDefaultAsync();
            return log != null && log.IsCompleted;
        }

        // Перемикач виконання (Toggle)
        public async Task ToggleHabitAsync(int habitId, DateTime date)
        {
            var cleanDate = date.Date;
            var existingLog = await _database.Table<HabitLog>()
                                             .Where(l => l.HabitId == habitId && l.Date == cleanDate)
                                             .FirstOrDefaultAsync();

            if (existingLog != null)
            {
                // Якщо вже було - видаляємо (зняти галочку)
                await _database.DeleteAsync(existingLog);
            }
            else
            {
                // Якщо не було - додаємо
                await _database.InsertAsync(new HabitLog
                {
                    HabitId = habitId,
                    Date = cleanDate,
                    IsCompleted = true
                });
            }
        }

        public async Task<int> GetHabitStreakAsync(int habitId)
        {
            // Отримуємо саму звичку, щоб знати її графік
            var habit = await _database.Table<Habit>().Where(h => h.Id == habitId).FirstOrDefaultAsync();
            if (habit == null) return 0;

            // Отримуємо всі записи виконання
            var logs = await _database.Table<HabitLog>()
                                      .Where(l => l.HabitId == habitId)
                                      .OrderByDescending(l => l.Date)
                                      .ToListAsync();

            int streak = 0;
            DateTime checkDate = DateTime.Today;

            // Парсимо дні виконання (якщо це не "Щодня")
            List<DayOfWeek> allowedDays = new List<DayOfWeek>();
            if (!string.IsNullOrEmpty(habit.TargetDays))
            {
                foreach (var dayStr in habit.TargetDays.Split(','))
                {
                    if (Enum.TryParse(dayStr, out DayOfWeek day)) allowedDays.Add(day);
                }
            }

            // Перевіряємо 365 днів назад (більше року серії навряд чи треба рахувати миттєво)
            for (int i = 0; i < 365; i++)
            {
                // 1. Якщо звичка має графік, і checkDate не входить в графік -> пропускаємо цей день, серію НЕ збиваємо
                if (habit.FrequencyType == 2 && allowedDays.Count > 0 && !allowedDays.Contains(checkDate.DayOfWeek))
                {
                    checkDate = checkDate.AddDays(-1);
                    continue;
                }

                // 2. Шукаємо, чи було виконання в цей день
                bool isDone = logs.Any(l => l.Date.Date == checkDate.Date);

                if (isDone)
                {
                    streak++;
                }
                else
                {
                    // Якщо це СЬОГОДНІ і ми ще не зробили -> серія не переривається, вона просто ще не збільшилась
                    if (checkDate.Date == DateTime.Today)
                    {
                        // нічого не робимо, йдемо перевіряти вчора
                    }
                    else
                    {
                        // Якщо це минулий день і ми пропустили -> кінець серії
                        break;
                    }
                }

                checkDate = checkDate.AddDays(-1);
            }

            return streak;
        }
    }
    }
        
    
