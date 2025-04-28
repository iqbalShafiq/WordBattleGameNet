# WordBattleGame

WordBattleGame is a multiplayer game application where players compete to guess words in real-time. The backend is built with ASP.NET Core, uses SignalR for real-time communication, and PostgreSQL as the database.

## Features

- Registration & login with email confirmation
- Automatic matchmaking to find opponents
- Word guessing game with multiple rounds
- In-game chat between players
- Password reset via email
- Player statistics (wins, losses, scores, etc.)
- Generated words by using OpenAI API with personalized words.
- RESTful API with Swagger documentation

## Technology Stack

- ASP.NET Core Web API
- SignalR (real-time communication)
- Entity Framework Core (PostgreSQL)
- JWT Authentication
- Serilog (logging)
- Docker Compose (monitoring stack: ELK)

## Directory Structure

- Controllers: API controllers (Auth, Players, etc.)
- Hubs: SignalR hubs (GameHub)
- Models: Data models and DTOs
- Repositories: Data access layer
- Services: Business services (WordGenerator, Email, etc.)
- Data: DbContext and database configuration
- Migrations: Database migration files
- Extensions: Extension methods for service configuration

## Configuration

1. **Database**  
   Ensure PostgreSQL is running and a database is created as specified in `appsettings.Development.json`:

   ```
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=word_battle_net;Username=postgres;Password=postgres"
   }
   ```

2. **Environment Variables**  
   Create a `.env` file in the project root and fill in:

   ```
   JWT_KEY=your_jwt_secret
   JWT_ISSUER=your_issuer
   JWT_AUDIENCE=your_audience
   OPENAI_API_KEY=your_openai_key
   ```

3. **Email**  
   Set up SMTP configuration in `appsettings.Development.json` under the `Email` section.

4. **Database Migration**  
   Run the migration:
   ```
   dotnet ef database update
   ```

## Running the Application

```
dotnet run
```

The application will run at `https://localhost:5098` (or another port as configured).

## API Documentation

Swagger UI is available at `/swagger` after the application is running.

## Monitoring (Optional)

A docker-compose file for the ELK stack is available in the `Monitor` folder:

```
cd Monitor
docker-compose up -d
```

## Contribution

Pull requests and issues are welcome for further development.
