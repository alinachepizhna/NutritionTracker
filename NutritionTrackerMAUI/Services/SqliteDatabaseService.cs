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

        }

        // ===============================
        // 🧩 --- КОРИСТУВАЧІ ---
        // ===============================

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


    }

}
