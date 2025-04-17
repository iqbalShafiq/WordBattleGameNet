using Microsoft.AspNetCore.SignalR;
using WordBattleGame.Repositories;
using WordBattleGame.Models;

namespace WordBattleGame.Hubs
{
    public class GameHub : Hub
    {
        private readonly IGameRepository _gameRepository;
        private readonly IPlayerRepository _playerRepository;

        public GameHub(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }
        private static readonly List<string> MatchMakingQueue = new();
        private static readonly object QueueLock = new();

        public async Task JoinGame(string gameId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("PlayerJoined", Context.ConnectionId);
        }

        // Contoh: broadcast pesan ke semua player di game
        public async Task SendMessageToGame(string gameId, string message)
        {
            await Clients.Group(gameId).SendAsync("ReceiveMessage", Context.ConnectionId, message);
        }

        public async Task JoinMatchMaking(string playerId)
        {
            lock (QueueLock)
            {
                if (!MatchMakingQueue.Contains(playerId))
                    MatchMakingQueue.Add(playerId);
            }
            await Clients.Caller.SendAsync("MatchMakingJoined", playerId);

            string[] matchedPlayers = null;
            lock (QueueLock)
            {
                if (MatchMakingQueue.Count >= 2)
                {
                    matchedPlayers = MatchMakingQueue.Take(2).ToArray();
                    MatchMakingQueue.RemoveRange(0, 2);
                }
            }
            if (matchedPlayers != null)
            {
                var players = _playerRepository.GetByIdsAsync(matchedPlayers).Result;

                // Buat game baru lewat repository
                var game = new Game
                {
                    Id = Guid.NewGuid().ToString(),
                    MaxRound = 5, // default, bisa diatur sesuai kebutuhan
                    CreatedAt = DateTime.UtcNow,
                    Players = new List<Player>() // relasi akan diisi di controller/service sesuai kebutuhan
                };
                await _gameRepository.AddAsync(game);
                var gameId = game.Id;
                foreach (var pid in matchedPlayers)
                {
                    await Clients.User(pid).SendAsync("MatchFound", gameId, matchedPlayers);
                }
            }
        }

        public async Task LeaveMatchMaking(string playerId)
        {
            // Logic untuk keluar dari antrean matchmaking
            await Clients.Caller.SendAsync("MatchMakingLeft", playerId);
        }

        public async Task LeaveGame(string gameId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("PlayerLeft", Context.ConnectionId);
        }

        public async Task StartRound(string gameId, int roundNumber)
        {
            // Logic untuk mulai round baru
            await Clients.Group(gameId).SendAsync("RoundStarted", roundNumber);
        }

        public async Task EndRound(string gameId, int roundNumber)
        {
            // Logic untuk mengakhiri round
            await Clients.Group(gameId).SendAsync("RoundEnded", roundNumber);
        }

        public async Task SendChat(string gameId, string playerId, string message)
        {
            await Clients.Group(gameId).SendAsync("ReceiveChat", playerId, message);
        }

        // Tambahkan method lain sesuai kebutuhan event game (start round, submit answer, dsb)
    }
}
