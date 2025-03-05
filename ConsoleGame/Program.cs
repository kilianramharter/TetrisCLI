namespace ConsoleGame;
using Spectre.Console;
using ConsoleGame.Models;

class Program {
	
	static List<Piece> pieces = new List<Piece> {
		new Piece(new List<(int, int)> { (0, 0), (1, 0), (2, 0), (3, 0) }, 'B', (3, 0), 0), // 1x4 Line
		new Piece(new List<(int, int)> { (0, 0), (1, 0), (0, 1), (1, 1) }, 'Y', (1, 1), 1), // 2x2 Square
		new Piece(new List<(int, int)> { (0, 0), (1, 0), (2, 0), (2, 1) }, 'G', (2, 1), 2), // L
		new Piece(new List<(int, int)> { (0, 0), (1, 0), (2, 0), (0, 1) }, 'B', (2, 1), 3), // L reverse
		new Piece(new List<(int, int)> { (0, 1), (1, 1), (2, 1), (1, 0) }, 'R', (2, 1), 4), // Pyramid
		new Piece(new List<(int, int)> { (1, 0), (2, 0), (0, 1), (1, 1) }, 'P', (2, 1), 5),
		new Piece(new List<(int, int)> { (0, 0), (1, 0), (1, 1), (2, 1) }, 'G', (2, 1), 6)
	};
	
	static void Main(string[] args) {
		
		/* GAME SETTINGS */
		const int boardSizeX = 10;
		const int boardSizeY = 20; // Original Tetris is 10x20
		bool debug = false; // Toggle debug mode during game with "D"
		
		/* SCORE SYSTEM VARS */
		int level = 1;
		int points = 0;
		int linesCleared = 0;
		
		/* GAME LOGIC VARS */
		char[,] board = initBoard(boardSizeX, boardSizeY);
		var canvas = new Canvas(boardSizeX, boardSizeY);
		ActivePiece activePiece = new ActivePiece();
		Piece nextPiece = selectRandomPiece();
		bool gameOver = false;
		bool generateNewPiece = true;
		bool gameFieldChanged = true;
		
		/* GAME TICKER VARS */
		DateTime lastDownMovement = DateTime.Now;
		TimeSpan timeDiff;
		int gameTick = 0;
		
		renderMenuView();
		
		var view = renderGameView();
		
		AnsiConsole.Live(view).Start(ctx => {
			do {
				if (generateNewPiece) {
					checkFullLines();
					gameTick = calculateGameTickSpeed(level);
					activePiece = insertPiece(nextPiece);
					nextPiece = selectRandomPiece();
					view = renderGameView();
					ctx.UpdateTarget(view);
					generateNewPiece = false;
				}

				if (gameFieldChanged) {
					canvas = renderGameCanvas(canvas);
					ctx.Refresh();
					gameFieldChanged = false;
				}
				
				timeDiff = DateTime.Now - lastDownMovement;
				
				if (timeDiff.Milliseconds > gameTick) {
					movePiece(0, 1);
					lastDownMovement = DateTime.Now;
				}
				
				readUserInput();
			} while (!gameOver);
		});
		
		renderGameOverView(points, level, linesCleared);
		
		int calculateGameTickSpeed(int currentLevel) { // Original NES Tetris tick rates for 60Hz systems
			int frames;
			switch (currentLevel) {
				case < 9: frames = ((48 - ((currentLevel - 1) * 5))); break;
				case < 10: frames = 6; break;
				case < 13: frames = 5; break;
				case < 16: frames = 4; break;
				case < 19: frames = 3; break;
				case < 29: frames = 2; break;
				default: frames = 1; break;
			}
			
			return (int)(((float)frames/60) * 1000);
		} 

		Color getColor(char identifier) {
			switch (identifier) {
				case 'A': return getColor(activePiece.Color);
				case 'B': return Color.Blue;
				case 'Y': return Color.Yellow;
				case 'P': return Color.Purple;
				case 'G': return Color.Green;
				case 'R': return Color.Red;
				default: return Color.Default;
			}
		}
		
		Align renderGameView() {
			return Align.Center(new Panel(
				new Columns(
					new Panel(canvas).Border(BoxBorder.Rounded),
					renderInfoPanel()
				)
			).Border(BoxBorder.None));
		}
		
		Canvas renderGameCanvas(Canvas gameCanvas) {
			
			// TODO: Replace with partial rendering only for changed blocks 
			for (int y = 0; y < boardSizeY; y++) {
				for (int x = 0; x < boardSizeX; x++) {
					gameCanvas.SetPixel(x, y, getColor(board[x, y]));
				}
			}
			
			if (debug) { // shows the y-axis collider + position anchor
				foreach ((int row, int col) in activePiece.Structure) {
					gameCanvas.SetPixel(row + activePiece.Position.Item1, col + activePiece.Position.Item2, Color.Black);
					if (col + activePiece.Position.Item2 + 1 < boardSizeY) {
						if (board[activePiece.Position.Item1 + row, activePiece.Position.Item2 + col + 1] == ' ' && board[activePiece.Position.Item1 + row, activePiece.Position.Item2 + col + 1] != 'A') {
							gameCanvas.SetPixel(row + activePiece.Position.Item1, col + activePiece.Position.Item2 + 1, Color.LightYellow3);
						}
					}
				}
				gameCanvas.SetPixel(activePiece.Position.Item1, activePiece.Position.Item2, Color.Grey74);
			}

			return gameCanvas;
		}

		Panel renderInfoPanel() {
			var debugPanel = renderDebugPanel();
			var nextPieceCanvas = renderNextPieceCanvas(nextPiece);
			
			var infoPanel = new Panel(
				new Rows(
					new Panel(nextPieceCanvas).Expand().Border(BoxBorder.Rounded).Header("[bold]NEXT[/]"),
					new Panel(
						new Rows(
							new Markup("[bold yellow]LINES[/]"),
							new Markup(linesCleared.ToString()),
							new Markup("[bold yellow]LEVEL[/]"),
							new Markup(level.ToString()),
							new Markup("[bold yellow]SCORE[/]"),
							new Markup(points.ToString())
						)
					).Expand().Border(BoxBorder.Rounded),
					debugPanel
				)
			).Border(BoxBorder.None);

			return infoPanel;
		}

		Canvas renderNextPieceCanvas(Piece piece) {
			var nextPieceCanvas = new Canvas(4, 4);
			int xOffset = 0;
			if (piece.RectangularSize.Item1 < 2) xOffset = 1; 
			foreach ((int row, int col) in piece.Structure) {
				nextPieceCanvas.SetPixel(row+xOffset, col+1, getColor(piece.Color));
			}
			return nextPieceCanvas;
		}

		Panel renderDebugPanel() {
			Panel debugPanel = new Panel("").Border(BoxBorder.None); 
			
			if (debug) {
				debugPanel = new Panel(
					new Rows(
						new Markup("[bold yellow]TICKS[/]"),
						new Markup(gameTick.ToString() + " ms"),
						//new Markup("[bold yellow]POSITION[/]"),
						//new Markup("X:" + activePiece.Position.Item1 + ",Y:" + activePiece.Position.Item2),
						new Markup("[bold yellow]RATIO[/]"),
						new Markup("X:" + activePiece.RectangularSize.Item1 + ",Y:" + activePiece.RectangularSize.Item2),
						new Markup("[bold yellow]ROTATION[/]"),
						new Markup(activePiece.Rotation.ToString() + " deg")
					)
				).Expand().Border(BoxBorder.Rounded).Header("[bold]DEBUG[/]");
			}

			return debugPanel;
		}
		

		void renderMenuView() {
			ConsoleKeyInfo input;
			string titleText = "sh>TRIS";
			string promptText = "ENTER TO START";
			
			var gameTitle = new Padder(new FigletText(titleText).Centered().Color(Color.Default)).PadBottom(4).PadTop(6);
			var gamePrompt = new Markup("[bold yellow]" + promptText + "[/]");
			
			Console.Clear();
			AnsiConsole.Write(new Rows(
				gameTitle,
				Align.Center(gamePrompt)
			));
			
			do {
				input = Console.ReadKey(true);
				Thread.Sleep(50);
			} while (input.Key != ConsoleKey.Enter);
			
			AnsiConsole.Clear();
		}

		void renderGameOverView(int points, int level, int linesCleared) {
			Console.Clear();
			
			var panel = new Panel(
				new Rows(
					new Padder(new Markup("[bold]GAME OVER[/]").Centered()).PadBottom(1).PadTop(6),
					new Panel(
						new Rows(
							new Markup("[bold yellow]LINES: [/]" + linesCleared.ToString()),
							new Markup("[bold yellow]LEVEL: [/]" + level.ToString()),
							new Markup("[bold yellow]SCORE: [/]" + points.ToString())
						)
					).Expand().Border(BoxBorder.Rounded),
					new Padder(new Markup("")).PadTop(1)
				)	
			).Border(BoxBorder.None);
			
			var view = new Panel(panel).Border(BoxBorder.None);

			AnsiConsole.Write(Align.Center(view));
			
			int inputOffsetX = (Console.WindowWidth/2) - 10;
			for (int i = 0; i < inputOffsetX; i++) {
				Console.Write(" ");
			}
			
			var name = AnsiConsole.Prompt(new TextPrompt<string>("[bold]YOUR NAME: [/]"));

		}
		
		void checkFullLines() {
			int newLinesCleared = 0;

			// Start from bottom Y line
			for (int y = boardSizeY - 1; y >= 0; y--) {
				int blockCount = 0;
				
				for (int x = 0; x < boardSizeX; x++) { // Count the blocks in the current line
					if (board[x, y] != 'A' && board[x, y] != ' ' ) blockCount++;
				}

				if (blockCount == boardSizeX) { // Line of blocks is full
					
					for (int x2 = 0; x2 < boardSizeX; x2++) { // Remove full line
						board[x2, y] = ' ';
					}
					
					for (int y3 = y; y3 >= 1; y3--) { // Move upper blocks 1 downward
                    	for (int x3 = 0; x3 < boardSizeX; x3++) {
                    		board[x3, y3] = board[x3, y3-1];
                    	}
                    }

					y++;
					newLinesCleared++;
				}
			}
			
			
			linesCleared += newLinesCleared;
			level = (linesCleared / 10) + 1;

			switch (newLinesCleared) { // Original NES Tetris point system
				case 1: points += (40 * level); break;
				case 2: points += (100 * level); break;
				case 3: points += (300 * level); break;
				case 4: points += (1200 * level); break;
			}
		}

		void rotatePiece() {
			if (activePiece.RectangularSize.Item1 == activePiece.RectangularSize.Item2) return; // Return if piece is a square
			
			int offsetX = 0;
			int offsetY = 0;
			int collisionCorrectionOffsetX = 0;
			
			var rotatedPieceStructure = activePiece.Structure.Select(coord => (-coord.Item2, coord.Item1)).ToList();
			
			if (activePiece.RectangularSize.Item1 > activePiece.RectangularSize.Item2 && activePiece.Rotation == 0) {
				offsetX = 1;
			} else if (activePiece.RectangularSize.Item1 < activePiece.RectangularSize.Item2 && activePiece.Rotation == 90) {
				offsetX = -1;
				offsetY = 1;
			} else if (activePiece.RectangularSize.Item1 > activePiece.RectangularSize.Item2 && activePiece.Rotation == 180) {
				offsetY = -1;
			} else if (activePiece.RectangularSize.Item1 < activePiece.RectangularSize.Item2 && activePiece.Rotation == 270) {
				// no offset necessary
			} else {
				return;
			}
			
			// Normalize the inverted coordinates (because at this stage they can contain negative values which fucks the game completely up)
			int minX = rotatedPieceStructure.Min(coord => coord.Item1);
			int minY = rotatedPieceStructure.Min(coord => coord.Item2);
			rotatedPieceStructure = rotatedPieceStructure.Select(coord => (coord.Item1 - minX, coord.Item2 - minY)).ToList();
			
			// Wall collision check & avoidance
			foreach (var coord in rotatedPieceStructure) {
				int x = activePiece.Position.Item1 + coord.Item1 + offsetX;
				int y = activePiece.Position.Item2 + coord.Item2 + offsetY;
				
				if (y >= boardSizeY || y < 0) return; // Return if y coord out of bounds

				// When rotating next to a wall, offset the rotation on the x-axis if it would go out of bounds
				// This is called "Wall kick" and is NOT a part of the original NES Tetris
				if (x < 0) {
					if (x * -1 > collisionCorrectionOffsetX) collisionCorrectionOffsetX = x * -1; 
				} else if (x >= boardSizeX) {
					if (boardSizeX - x - 1 < collisionCorrectionOffsetX ) collisionCorrectionOffsetX = boardSizeX - x - 1;
				}
				
				if (board[x + collisionCorrectionOffsetX, y] != ' ' && board[x + collisionCorrectionOffsetX, y] != 'A') return; 	
			}
			
			removeActivePiece();
			
			// Draw rotated piece
			foreach (var coord in rotatedPieceStructure) {
				board[activePiece.Position.Item1 + coord.Item1 + offsetX + collisionCorrectionOffsetX, activePiece.Position.Item2 + coord.Item2 + offsetY] = 'A';
			}

			activePiece = new ActivePiece(
				new Piece(rotatedPieceStructure, activePiece.Color, (activePiece.RectangularSize.Item2, activePiece.RectangularSize.Item1), activePiece.PieceId),
				(activePiece.Position.Item1 + offsetX + collisionCorrectionOffsetX, activePiece.Position.Item2 + offsetY),
				(activePiece.Rotation+90 >= 360) ? 0 : activePiece.Rotation += 90
			);
			
			gameFieldChanged = true;
		}

		void movePiece(int moveX, int moveY) {
			
			// x-axis collision check
			if (moveX != 0) {
				
				if (activePiece.Position.Item1 + activePiece.RectangularSize.Item1 + moveX >= boardSizeX || activePiece.Position.Item1 + moveX < 0) {
					return; // piece is going out of bounds on the x axis
				}
				
				foreach ((int row, int col) in activePiece.Structure) {
					if (board[activePiece.Position.Item1 + row + moveX, col + activePiece.Position.Item2] != ' ' && board[activePiece.Position.Item1 + row + moveX, col + activePiece.Position.Item2] != 'A') {
						return; // Collision detected, abort further checking
					}
				}
			}

			// y-axis collision check
			if (moveY != 0) {
				bool freeze = false; // if true, piece will be "frozen" and a new piece will spawn on top of the map
				
				freeze = activePiece.Position.Item2 + activePiece.RectangularSize.Item2 + moveY >= boardSizeY; // Check if piece is going out of bounds on y axis
				
				if (!freeze) {
					foreach ((int row, int col) in activePiece.Structure) { // Check if there is a piece below
						if (board[activePiece.Position.Item1 + row, activePiece.Position.Item2 + col + moveY] != ' ' && board[activePiece.Position.Item1 + row, activePiece.Position.Item2 + col + moveY] != 'A') {
							freeze = true;
							break;
						}
					}
				}
				
				if (freeze) { // Freeze the peece
					foreach ((int row, int col) in activePiece.Structure) {
						if (board[activePiece.Position.Item1 + row, activePiece.Position.Item2 + col] == 'A') {
							board[activePiece.Position.Item1 + row, activePiece.Position.Item2 + col] = activePiece.Color;
						}
						if (activePiece.Position.Item2 + col == 0) gameOver = true; // If freezing occurs on y axis line 0 -> game over
					}

					generateNewPiece = true;
					gameFieldChanged = true;
				
					return;
				}
			}
			
			removeActivePiece();
				
			foreach ((int row, int col) in activePiece.Structure) {
				board[ activePiece.Position.Item1 + row + moveX, activePiece.Position.Item2 + col + moveY] = 'A';
			}
				
			activePiece.Position = (activePiece.Position.Item1 + moveX, activePiece.Position.Item2 + moveY);
			gameFieldChanged = true;
		}
		
		void removeActivePiece() {
			foreach ((int row, int col) in activePiece.Structure) {
				board[row + activePiece.Position.Item1, col + activePiece.Position.Item2] = ' ';
			}
		}
		
		Piece selectRandomPiece() {
			int pieceId;
			
			do {
				pieceId = new Random().Next(0, pieces.Count);	
			} while(pieceId == activePiece.PieceId);
			
			return pieces[pieceId];
		}

		ActivePiece insertPiece(Piece piece) {
			foreach ((int row, int col) in piece.Structure) {
				board[row + ( (boardSizeX/2) - piece.RectangularSize.Item1 ), col] = 'A';
			}
			return new ActivePiece(piece, ((boardSizeX/2)-piece.RectangularSize.Item1 , 0), 0);
		}

		char[,] initBoard(int boardSizeX, int boardSizeY) {
			char[,] emptyBoard = new char[boardSizeX, boardSizeY];
			for (int y = 0; y < boardSizeY; y++) {
				for (int x = 0; x < boardSizeX; x++) {
					emptyBoard[x, y] = ' ';
				}
			}
			return emptyBoard;
		}
		
		 void readUserInput() {
			ConsoleKeyInfo input;

			if (Console.KeyAvailable) {
				input = Console.ReadKey(true);
				
				switch (input.Key) {
					case ConsoleKey.LeftArrow:
						movePiece(-1, 0);
						break;
					case ConsoleKey.RightArrow:
						movePiece(1, 0);
						break;
					case ConsoleKey.DownArrow:
						movePiece(0, 1);
						break;
					case ConsoleKey.Spacebar:
						rotatePiece();
						break;
					case ConsoleKey.UpArrow:
						rotatePiece();
						break;
					case ConsoleKey.Q:
						break;
					case ConsoleKey.D:
						debug = !debug;
						break;
				}
			}
		}
	}
}