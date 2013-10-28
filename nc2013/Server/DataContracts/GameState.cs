﻿using System;
using System.Linq;
using Newtonsoft.Json;

namespace Server.DataContracts
{
	[JsonObject]
	public class GameState
	{
		[JsonProperty]
		public Guid GameId { get; set; }

		[JsonProperty]
		public CellState[] MemoryState { get; set; }

		[JsonProperty]
		public ProgramState[] ProgramStates { get; set; }

		[JsonProperty]
		public int CurrentProgram { get; set; }

		[JsonProperty]
		public int? Winner { get; set; }

		[JsonProperty]
		public int CurrentStep { get; set; } 

		public static GameState FromCore(Guid gameId, Core.Game.GameState gameState)
		{
			return new GameState
			{
				GameId = gameId,
				CurrentProgram = gameState.CurrentProgram,
				MemoryState = gameState.MemoryState.Select(CellState.FromCore).ToArray(),
				ProgramStates = gameState.ProgramStates.Select(ProgramState.FromCore).ToArray(),
				CurrentStep = gameState.CurrentStep,
				Winner = gameState.Winner
			};
		}
	}
}