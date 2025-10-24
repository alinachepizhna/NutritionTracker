using SQLite;
using NutritionTrackerMAUI.Models;

namespace NutritionTrackerMAUI.Services
{
    public class SqliteDatabaseService
    {
        private readonly SQLiteAsyncConnection _database;

        public SqliteDatabaseService(string dbPath)
        {
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
