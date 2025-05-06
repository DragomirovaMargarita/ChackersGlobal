using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Chackers
{
    public class GameResult
    {
        public int UserId { get; set; }
        public int DurationSeconds { get; set; }
        public string Mode { get; set; }
        public int Points { get; set; }
        public string Status { get; set; }
        public int MovesCount { get; set; }
    }

    public class UserStat
    {
        public string UserName { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
    }

    public class DatabaseService
    {
        private string connectionString = "server=localhost;user=root;password=345403;database=Chackers;";

        public bool Register(string username, string password)
        {
            try
            {
                string hash = HashPassword(password);
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand("INSERT INTO users (username, password_hash) VALUES (@username, @hash)", conn);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@hash", hash);
                    cmd.ExecuteNonQuery();
                    return true;
                }
            }
            catch (MySqlException ex)
            {
                
                System.Diagnostics.Debug.WriteLine($"Database error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General error: {ex.Message}");
                return false;
            }
        }

        public int? Login(string username, string password)
        {
            try
            {
                string hash = HashPassword(password);
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT id FROM users WHERE username=@username AND password_hash=@hash", conn);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@hash", hash);
                    var result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : (int?)null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
                return null;
            }
        }

        public void SaveGameResult(GameResult gameResult)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        "INSERT INTO games (user_id, duration_seconds, mode, points, status, moves_count, game_date) " +
                        "VALUES (@user_id, @duration, @mode, @points, @status, @moves, @date)", conn);
                    
                    cmd.Parameters.AddWithValue("@user_id", gameResult.UserId);
                    cmd.Parameters.AddWithValue("@duration", gameResult.DurationSeconds);
                    cmd.Parameters.AddWithValue("@mode", gameResult.Mode);
                    cmd.Parameters.AddWithValue("@points", gameResult.Points);
                    cmd.Parameters.AddWithValue("@status", gameResult.Status);
                    cmd.Parameters.AddWithValue("@moves", gameResult.MovesCount);
                    cmd.Parameters.AddWithValue("@date", DateTime.Now);
                    
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save game error: {ex.Message}");
            }
        }

        public GameResult[] GetUserGameHistory(int userId, int limit = 10)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        "SELECT duration_seconds, mode, points, status, moves_count, game_date " +
                        "FROM games WHERE user_id = @user_id ORDER BY game_date DESC LIMIT @limit", conn);
                    
                    cmd.Parameters.AddWithValue("@user_id", userId);
                    cmd.Parameters.AddWithValue("@limit", limit);
                    
                    var results = new System.Collections.Generic.List<GameResult>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new GameResult
                            {
                                UserId = userId,
                                DurationSeconds = reader.GetInt32(0),
                                Mode = reader.GetString(1),
                                Points = reader.GetInt32(2),
                                Status = reader.GetString(3),
                                MovesCount = reader.GetInt32(4)
                            });
                        }
                    }
                    return results.ToArray();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get game history error: {ex.Message}");
                return new GameResult[0];
            }
        }

        public GameStatistics GetUserStatistics(int userId)
        {
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        "SELECT " +
                        "COUNT(*) as total_games, " +
                        "SUM(CASE WHEN status = 'win' THEN 1 ELSE 0 END) as wins, " +
                        "SUM(CASE WHEN status = 'lose' THEN 1 ELSE 0 END) as losses, " +
                        "SUM(CASE WHEN status = 'draw' THEN 1 ELSE 0 END) as draws, " +
                        "AVG(points) as avg_points, " +
                        "AVG(duration_seconds) as avg_duration " +
                        "FROM games WHERE user_id = @user_id", conn);
                    
                    cmd.Parameters.AddWithValue("@user_id", userId);
                    
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new GameStatistics
                            {
                                TotalGames = reader.GetInt32(0),
                                Wins = reader.GetInt32(1),
                                Losses = reader.GetInt32(2),
                                Draws = reader.GetInt32(3),
                                AveragePoints = reader.IsDBNull(4) ? 0 : reader.GetDouble(4),
                                AverageDuration = reader.IsDBNull(5) ? 0 : reader.GetDouble(5)
                            };
                        }
                    }
                    return new GameStatistics();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get statistics error: {ex.Message}");
                return new GameStatistics();
            }
        }

        public List<string> GetAllUsers()
        {
            var users = new List<string>();
            try
            {
                using (var conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT username FROM users", conn);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(reader.GetString(0));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAllUsers error: {ex.Message}");
            }
            return users;
        }

        public void AddWin(int userId)
        {
            System.Diagnostics.Debug.WriteLine($"[DB] AddWin: userId={userId}");
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                // Проверяем, есть ли запись
                var checkCmd = new MySqlCommand("SELECT id FROM user_stats WHERE user_id = @user_id", conn);
                checkCmd.Parameters.AddWithValue("@user_id", userId);
                var exists = checkCmd.ExecuteScalar();
                if (exists != null)
                {
                    var updateCmd = new MySqlCommand("UPDATE user_stats SET wins = wins + 1 WHERE user_id = @user_id", conn);
                    updateCmd.Parameters.AddWithValue("@user_id", userId);
                    updateCmd.ExecuteNonQuery();
                }
                else
                {
                    var insertCmd = new MySqlCommand("INSERT INTO user_stats (user_id, wins, losses) VALUES (@user_id, 1, 0)", conn);
                    insertCmd.Parameters.AddWithValue("@user_id", userId);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        public void AddLoss(int userId)
        {
            System.Diagnostics.Debug.WriteLine($"[DB] AddLoss: userId={userId}");
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                // Проверяем, есть ли запись
                var checkCmd = new MySqlCommand("SELECT id FROM user_stats WHERE user_id = @user_id", conn);
                checkCmd.Parameters.AddWithValue("@user_id", userId);
                var exists = checkCmd.ExecuteScalar();
                if (exists != null)
                {
                    var updateCmd = new MySqlCommand("UPDATE user_stats SET losses = losses + 1 WHERE user_id = @user_id", conn);
                    updateCmd.Parameters.AddWithValue("@user_id", userId);
                    updateCmd.ExecuteNonQuery();
                }
                else
                {
                    var insertCmd = new MySqlCommand("INSERT INTO user_stats (user_id, wins, losses) VALUES (@user_id, 0, 1)", conn);
                    insertCmd.Parameters.AddWithValue("@user_id", userId);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }

        public UserStats GetUserStats(int userId)
        {
            var stats = new UserStats();
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT wins, losses FROM user_stats WHERE user_id = @user_id", conn);
                cmd.Parameters.AddWithValue("@user_id", userId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        stats.Wins = reader.GetInt32(0);
                        stats.Losses = reader.GetInt32(1);
                    }
                }
            }
            return stats;
        }

        public List<UserStat> GetAllUserStats()
        {
            var stats = new List<UserStat>();
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new MySqlCommand(
                    "SELECT u.username, s.wins, s.losses " +
                    "FROM user_stats s " +
                    "JOIN users u ON s.user_id = u.id " +
                    "ORDER BY s.wins DESC, s.losses ASC", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        stats.Add(new UserStat
                        {
                            UserName = reader.GetString(0),
                            Wins = reader.GetInt32(1),
                            Losses = reader.GetInt32(2)
                        });
                    }
                }
            }
            return stats;
        }

        private string HashPassword(string password)
        {
            try
            {
                using (var sha = SHA256.Create())
                {
                    var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                    return BitConverter.ToString(bytes).Replace("-", "").ToLower();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Hash error: {ex.Message}");
                return string.Empty;
            }
        }
    }

    public class GameStatistics
    {
        public int TotalGames { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public double AveragePoints { get; set; }
        public double AverageDuration { get; set; }
    }

    public class UserStats
    {
        public int Wins { get; set; }
        public int Losses { get; set; }
    }
} 