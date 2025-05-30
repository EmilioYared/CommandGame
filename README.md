# CommandGame 🚀

A fun and educational command-based programming game where players control a ship to collect stars using various commands. This game helps teach programming concepts through an interactive and engaging interface.

## 🎮 Game Overview

CommandGame is a web-based puzzle game where players write sequences of commands to navigate a ship through a grid-based world. The goal is to collect all stars while avoiding obstacles and staying within command limits.

### Key Features

- **Command-Based Movement**: Control your ship using commands like:
  - Move Forward
  - Turn Left
  - Turn Right
  - Color-Conditional Commands
- **Command Limits**: Each level has a maximum command limit to encourage efficient solutions
- **User Accounts**: Create an account to track your progress
- **Level Creation**: Users can create and share their own levels
- **Real-time Execution**: Step through your commands or run them all at once
- **Visual Feedback**: Clear visual representation of the game state

## 🛠️ Technical Stack

- **Backend**: ASP.NET Core
- **Frontend**: Tailwind CSS, JavaScript, and HTML
- **Database**: MySQL
- **Authentication**: Custom user authentication system

## 📋 Database Schema

### Users Table
```sql
CREATE TABLE users (
    id INT PRIMARY KEY AUTO_INCREMENT,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    created_at DATETIME(6),
    updated_at DATETIME(6)
);
```

### Levels Table
```sql
CREATE TABLE levels (
    LevelId INT PRIMARY KEY AUTO_INCREMENT,
    Name VARCHAR(100),
    Description TEXT,
    Width INT NOT NULL,
    Height INT NOT NULL,
    ShipStartX INT NOT NULL,
    ShipStartY INT NOT NULL,
    MaxCommands INT NOT NULL,
    TilesJson JSON NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ShipStartOrientation VARCHAR(10) NOT NULL DEFAULT 'North',
    UserId INT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES users(id)
);
```

## 🚀 Getting Started

### Prerequisites

- .NET 6.0 SDK or later
- MySQL Server
- Visual Studio 2022 or Visual Studio Code

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/CommandGame.git
   cd CommandGame
   ```

2. Update the database connection string in `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=commandgame;User=your_username;Password=your_password;"
     }
   }
   ```

3. Run database migrations:
   ```bash
   dotnet ef database update
   ```

4. Build and run the application:
   ```bash
   dotnet build
   dotnet run
   ```

5. Open your browser and navigate to `https://localhost:5001` or `http://localhost:5000`

## 🎯 How to Play

1. Create an account or log in
2. Select a level from the level selection screen
3. Add commands to your command list:
   - Use the command buttons to add movement commands
   - Set color conditions for commands if needed
4. Test your solution:
   - Use "Step" to execute commands one at a time
   - Use "Run" to execute all commands at once
   - Use "Reset" to start over
5. Collect all stars to complete the level!

## Origin

This is a simplified version of the game provided by 42 Beirut as part of their cognitive skills test. I really enjoyed playing it while submitting my application, so I decided to turn it into a project.
