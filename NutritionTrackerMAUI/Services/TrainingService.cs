using NutritionTrackerMAUI.Models;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NutritionTrackerMAUI.Services
{
    public class TrainingService
    {
        private readonly SQLiteAsyncConnection _db;

        public TrainingService(SQLiteAsyncConnection db)
        {
            _db = db;
        }

        public async Task InitAsync()
        {
            await _db.CreateTableAsync<TrainingPlan>();
        }

        public Task<int> AddTrainingPlanAsync(TrainingPlan plan)
        {
            return _db.InsertAsync(plan);
        }

        public Task<List<TrainingPlan>> GetPlansForUserAsync(int userId)
        {
            return _db.Table<TrainingPlan>().Where(p => p.UserId == userId).ToListAsync();
        }

        public Task DeletePlanAsync(int id)
        {
            return _db.DeleteAsync<TrainingPlan>(id);
        }
    }
}
