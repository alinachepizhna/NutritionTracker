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

            // Створення таблиць
            _database.CreateTableAsync<User>().Wait();
            _database.CreateTableAsync<AnthropometricData>().Wait();
            _database.CreateTableAsync<Goal>().Wait(); // Таблиця для цілей
        }

        // Додати користувача
        public Task<int> AddUserAsync(User user) => _database.InsertAsync(user);

        // Отримати користувача за ім'ям та прізвищем
        public Task<User?> GetUserAsync(string firstName, string lastName) =>
            _database.Table<User>()
                     .Where(u => u.FirstName == firstName && u.LastName == lastName)
                     .FirstOrDefaultAsync();

        // Додати антропометричні дані
        public Task<int> AddAnthropometricDataAsync(AnthropometricData data) =>
            _database.InsertAsync(data);

        // Отримати всі дані користувача
        public Task<List<AnthropometricData>> GetUserDataAsync(int userId) =>
            _database.Table<AnthropometricData>()
                     .Where(d => d.UserId == userId)
                     .ToListAsync();

        // Додати ціль
        public Task<int> AddGoalAsync(Goal goal) => _database.InsertAsync(goal);

        // Отримати останню ціль користувача
        public Task<Goal?> GetLatestGoalAsync(int userId) =>
            _database.Table<Goal>()
                     .Where(g => g.UserId == userId)
                     .OrderByDescending(g => g.Id)
                     .FirstOrDefaultAsync();
    }
}
