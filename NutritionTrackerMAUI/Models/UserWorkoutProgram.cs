using SQLite;

public class UserWorkoutProgram
{
    [PrimaryKey, AutoIncrement] // Первинний ключ, автоматично збільшується
    public int Id { get; set; }

    public int UserId { get; set; } // ID користувача, до якого належить програма
    public string Name { get; set; } = string.Empty; // Назва програми
    public string Description { get; set; } = string.Empty; // Опис програми
    public string DailyWorkouts { get; set; } = string.Empty; // Дні тренувань у форматі CSV (через кому)
}
