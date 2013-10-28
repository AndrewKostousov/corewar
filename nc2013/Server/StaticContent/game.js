var Game = Base.extend({
	constructor: function (options) {
		this.programs = options.programs;
		this.memory = options.memory;
		this.$currentStep = options.$currentStep;
	},
	start: function (programStartInfos) {
		var that = this;
		return server.post("start", programStartInfos)
			.pipe(function (gameId) {
				that.gameId = gameId;
				return server.get("state", { gameId: gameId });
			})
			.pipe(function (gameState) {
				return that._setGameState(gameState);
			});
	},
	stepToEnd: function () {
		var that = this;
		return server.get("step/end", { gameId: this.gameId })
			.pipe(function (gameState) {
				return that._setGameState(gameState);
			});
	},
	step: function (stepCount) {
		var that = this;
		return server.get("step", { gameId: this.gameId, count: stepCount })
			.pipe(function (diff) {
				if (diff.gameState) {
					return that._setGameState(diff.gameState);
				} else {
					that.$currentStep.text(diff.currentStep);
					that.memory.applyDiffs(diff.memoryDiffs);
					for (var i = 0; i < diff.programStateDiffs.length; ++i) {
						var programStateDiff = diff.programStateDiffs[i];
						that.programs[programStateDiff.program].applyDiff(programStateDiff);
					}
					if (diff.winner) {
						that.programs[diff.winner].win();
						return $.Deferred().reject("gameover");
					}
				}
			});
	},
	reset: function () {
		this.$currentStep.text("");
		this.memory.reset();
		for (var i = 0; i < this.programs.length; ++i) {
			this.programs[i].reset();
		}
	},
	_setGameState: function (gameState) {
		this.$currentStep.text(gameState.currentStep);
		this.memory.setCellStates(gameState.memoryState);
		for (var i = 0; i < gameState.programStates.length; ++i) {
			this.programs[i].setProgramState(gameState.programStates[i]);
		}
		if (gameState.winner) {
			this.programs[gameState.winner].win();
			return $.Deferred().reject("gameover");
		}
	}
});

var GameRunner = Base.extend({
	constructor: function (options) {
		this.game = options.game;
		this.programs = options.programs;
		this.onGameStartedHandlers = [];
	},
	onGameStarted: function (callback) {
		this.onGameStartedHandlers.push(callback);
	},
	_play: function (action) {
		if (!this.gameQueue) {
			this.game.reset();
			var programStartInfos = [];
			for (var i = 0; i < this.programs.length; ++i) {
				programStartInfos.push({ program: this.programs[i].$program.val() });
			}
			this.gameQueue = this.game.start(programStartInfos);
			for (var i = 0; i < this.onGameStartedHandlers.length; i++)
				this.onGameStartedHandlers[i].call();
		}
		else if (!this.gameQueue.isResolved())
			return this.gameQueue.isRejected() ? this.gameQueue : $.Deferred().reject("busy");
		var that = this;
		return this.gameQueue = this.gameQueue
			.pipe(function () {
				return action(that.game);
			})
			.fail(function (err) {
				that.pause();
				alert(err);
			});
	},
	reset: function () {
		var that = this;
		function reset() {
			that.game.reset();
			that.gameQueue = null;
		}
		if (this.gameQueue) {
			this.pause();
			this.gameQueue.pipe(reset, reset);
		}
	},
	step: function (stepCount) {
		this.pause();
		this._play(function (game) {
			return game.step(stepCount);
		});
	},
	stepToEnd: function () {
		this.pause();
		this._play(function (game) {
			return game.stepToEnd();
		});
	},
	run: function (speed) {
		if (this.speed != speed) {
			var that = this;
			function iteration() {
				if (that.speed)
					that._play(function (game) {
						if (that.speed)
							return game.step(that.speed).pipe(function () {
								setTimeout(iteration, 0);
							});
					});
			}
			if (this.speed)
				this.speed = speed;
			else {
				this.speed = speed;
				iteration();
			}
		}
	},
	pause: function () {
		this.speed = 0;
	}
});
