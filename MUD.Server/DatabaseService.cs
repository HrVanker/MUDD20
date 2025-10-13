using Microsoft.EntityFrameworkCore;
using MUD.Core;
using MUD.Server.Data;
using System;
using System.Linq;

namespace MUD.Server
{
    /// <summary>
    /// The DbContext is the main class from EF Core that represents our connection to the database.
    /// </summary>
    public class GameDbContext : DbContext
    {
        // This correctly defines the "Players" table.
        public DbSet<PlayerCharacter> Players { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=game.db");
        }
    }

    /// <summary>
    /// A service to manage the game's database connection and logic.
    /// This now correctly implements the IDatabaseService interface.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        public void InitializeDatabase()
        {
            Console.WriteLine("Initializing database connection...");
            using (var db = new GameDbContext())
            {
                db.Database.EnsureCreated();
            }
            Console.WriteLine("Database is ready.");
        }

        public IPlayerRecord GetPlayerRecord(ulong accountId)
        {
            using (var db = new GameDbContext())
            {
                // This will find the player or return null if not found.
                return db.Players.FirstOrDefault(p => p.AccountId == accountId);
            }
        }
    }
}