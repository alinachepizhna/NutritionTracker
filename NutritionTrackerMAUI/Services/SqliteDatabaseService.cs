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
 
            string folderPath = @"D:\������\XamarinProjects";
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string dbPath = Path.Combine(folderPath, "nutrition.db3");

            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<User>().Wait();
            _database.CreateTableAsync<AnthropometricData>().Wait();
        }

        // ������ �����������
        public Task<int> AddUserAsync(User user) => _database.InsertAsync(user);

        // �������� ����������� �� ��'�� �� ��������
        public Task<User?> GetUserAsync(string firstName, string lastName) =>
            _database.Table<User>()
                     .Where(u => u.FirstName == firstName && u.LastName == lastName)
                     .FirstOrDefaultAsync();

        // ������ �������������� ���
        public Task<int> AddAnthropometricDataAsync(AnthropometricData data) =>
            _database.InsertAsync(data);

        // �������� �� ��� �����������
        public Task<List<AnthropometricData>> GetUserDataAsync(int userId) =>
            _database.Table<AnthropometricData>()
                     .Where(d => d.UserId == userId)
                     .ToListAsync();
    }
}
