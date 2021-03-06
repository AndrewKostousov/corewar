﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.Arena;
using JetBrains.Annotations;
using Server.Handlers;

namespace Server.Arena
{
	public class ArenaPlayerHandler : StrictPathHttpHandlerBase
	{
		private readonly ArenaState arenaState;

		public ArenaPlayerHandler([NotNull] ArenaState arenaState)
			: base("arena/player")
		{
			this.arenaState = arenaState;
		}

		public override void Handle([NotNull] GameHttpContext context)
		{
			var playerName = context.GetStringParam("name");
			var version = context.GetOptionalIntParam("version");
			var playerVersions = arenaState.PlayersRepo.LoadPlayerVersions(playerName);
			var botVersionInfos = playerVersions.Select(p => new BotVersionInfo
			{
				Name = p.Name,
				Version = p.Version,
				Timestamp = p.Timestamp
			})
			.OrderByDescending(x => x.Version)
			.ToArray();
			TournamentRanking ranking = null;
			var playerInfo = new PlayerInfo();
			var lastVersion = playerVersions.TryGetLastVersion();
			if (lastVersion != null)
			{
				ArenaPlayer arenaPlayer;
				if (version.HasValue)
				{
					arenaPlayer = playerVersions.FirstOrDefault(p => p.Version == version.Value);
					if (arenaPlayer != null)
					{
						arenaPlayer.Authors = lastVersion.Authors;
						var tournamentId = "last";
						if (arenaPlayer.Version != lastVersion.Version)
						{
							var nextVersion = playerVersions.First(p => p.Version > version.Value);
							tournamentId = arenaState.GamesRepo.GetAllTournamentIds()
								.Select(id => new DateTime(long.Parse(id), DateTimeKind.Utc))
								.OrderBy(ts => ts)
								.LastOrDefault(ts => ts < nextVersion.Timestamp)
								.Ticks.ToString();
						}
						ranking = arenaState.GamesRepo.TryLoadRanking(tournamentId);
					}
				}
				else
				{
					arenaPlayer = lastVersion;
					ranking = arenaState.GamesRepo.TryLoadRanking("last");
				}
				if (ranking != null)
					playerInfo = CreatePlayerInfo(arenaPlayer, ranking, botVersionInfos, context.GodMode || arenaState.EnableDeepNavigation);
			}
			context.SendResponse(playerInfo);
		}

		[NotNull]
		private PlayerInfo CreatePlayerInfo([NotNull] ArenaPlayer arenaPlayer, [NotNull] TournamentRanking ranking, [NotNull] BotVersionInfo[] botVersionInfos, bool deepNavigationEnabled)
		{
			var rankingEntry = ranking.Places.FirstOrDefault(r => r.Name == arenaPlayer.Name && r.Version == arenaPlayer.Version) ?? new RankingEntry
			{
				Name = arenaPlayer.Name,
				Version = arenaPlayer.Version,
			};
			var games = arenaState.GamesRepo.LoadGames(ranking.TournamentId);
			var gamesByEnemy = GetFinishedGamesWithEnemies(games, arenaPlayer.Name, arenaPlayer.Version, deepNavigationEnabled);
			var playerInfo = new PlayerInfo
			{
				RankingEntry = rankingEntry,
				Authors = arenaPlayer.Authors,
				SubmitTimestamp = arenaPlayer.Timestamp,
				GamesByEnemy = gamesByEnemy,
				BotVersionInfos = botVersionInfos,
				DeepNavigationEnabled = deepNavigationEnabled,
				Program = deepNavigationEnabled ? arenaPlayer.Program : null,
			};
			return playerInfo;
		}

		[NotNull]
		public static FinishedGamesWithEnemy[] GetFinishedGamesWithEnemies([NotNull] List<BattleResult> games, [NotNull] string arenaPlayerName, int arenaPlayerVersion, bool deepNavigationEnabled)
		{
			var gamesByEnemy = games
				.Select(x => Tuple.Create(x.Player1Result, x.Player2Result, true)).Concat(games.Select(x => Tuple.Create(x.Player2Result, x.Player1Result, false)))
				.Where(x => x.Item1.Player.Name == arenaPlayerName && x.Item1.Player.Version == arenaPlayerVersion)
				.GroupBy(x => x.Item2.Player)
				.Select(g => new FinishedGamesWithEnemy
				{
					Enemy = g.Key.Name,
					EnemyVersion = g.Key.Version,
					Wins = g.Count(x => x.Item1.ResultType == BattlePlayerResultType.Win),
					Draws = g.Count(x => x.Item1.ResultType == BattlePlayerResultType.Draw),
					Loses = g.Count(x => x.Item1.ResultType == BattlePlayerResultType.Loss),
					GameInfos = deepNavigationEnabled ? GetGameInfos(g.ToList()) : null,
				})
				.OrderByDescending(x => x.Wins)
				.ToArray();
			return gamesByEnemy;
		}

		[NotNull]
		private static FinishedGameInfo[] GetGameInfos([NotNull] IEnumerable<Tuple<BattlePlayerResult, BattlePlayerResult, bool>> games)
		{
			return games.Select(x => new FinishedGameInfo
			{
				Player1Result = x.Item3 ? x.Item1 : x.Item2,
				Player2Result = x.Item3 ? x.Item2 : x.Item1,
				Label = x.Item1.ResultType.ToString(),
			}).ToArray();
		}
	}
}