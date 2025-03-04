using System.Collections;
using System.Drawing;

namespace ConsoleGame;
using Spectre.Console;
using ConsoleGame.Models;

class Program {
	
	enum GameControl { Left, Right, Down, Rotate, None, Exit }
	
	static List<Piece> pieces = new List<Piece> {
		new Piece(new List<(int, int)> { (0, 0), (1, 0), (2, 0), (3, 0) }, 'B', (3, 0)), // 1x4 Line
		new Piece(new List<(int, int)> { (0, 0), (1, 0), (0, 1), (1, 1) }, 'Y', (1, 1)), // 2x2 Square
		new Piece(new List<(int, int)> { (0, 0), (1, 0), (2, 0), (2, 1) }, 'G', (2, 1)), // L
		new Piece(new List<(int, int)> { (0, 0), (1, 0), (2, 0), (0, 1) }, 'B', (2, 1)), // L reverse
		new Piece(new List<(int, int)> { (0, 1), (1, 1), (2, 1), (1, 0) }, 'R', (2, 1)), // Pyramid
		new Piece(new List<(int, int)> { (1, 0), (2, 0), (0, 1), (1, 1) }, 'Y', (2, 1)),
		new Piece(new List<(int, int)> { (0, 0), (1, 0), (1, 1), (2, 1) }, 'G', (2, 1))
	};
	
	/* L 270deg
	 * (0,0) (1,0)
	 *       (1,1)
	 *       (1,2)
	 *       
	 */
	
	static void Main(string[] args) {

		// Original Tetris is 10x20
		
		/* GAME SETTINGS */
		const int boardSizeX = 10;
		const int boardSizeY = 20;
		int gameTick = 300;
		bool debug = true;
		
		/* SCORE SYSTEM VARS */
		int level = 1;
		int points = 0;
		int linesCleared = 0;
		
		/* GAME LOGIC VARS */
		char[,] board = new char[boardSizeX, boardSizeY];
		var canvas = new Canvas(boardSizeX, boardSizeY);
		var nextPieceCanvas = new Canvas(4, 4);
		ActivePiece activePiece = new ActivePiece();
		Piece nextPiece = selectRandomPiece();
		bool gameOver = false;
		bool generateNewPiece = true;
		bool gameFieldChanged = true;
		
		/* GAME TICKER VARS */
		DateTime lastDownMovement = DateTime.Now;
		TimeSpan timeDiff;
		
		showMenu();
		
		initBoard();
		
		do {
			if (generateNewPiece) {
				checkFullLines();
				insertPiece( nextPiece );
				nextPiece = selectRandomPiece();
				drawNextPieceCanvas(nextPiece);
				generateNewPiece = false;
			}

			if (gameFieldChanged) {
				drawBoard();
				gameFieldChanged = false;
			}
			
			timeDiff = DateTime.Now - lastDownMovement;
			
			if (timeDiff.Milliseconds > gameTick) {
				movePiece(0, 1);
				lastDownMovement = DateTime.Now;
			}
			
			readUserInput();
			
		} while (!gameOver);
		
		showGameOverScreen();


		void showMenu() {
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
			
			/* var selection = AnsiConsole.Prompt(
				new SelectionPrompt<string>()
					.PageSize(10)
					.AddChoices(new[] {
						"Start Game", "Exit"
					})); */
			
			// Console.WriteLine("test");
		}

		void showGameOverScreen() {
			Console.Clear();
			
			var panel = new Panel(
				new Rows(
					//new Panel(nextPieceCanvas).Expand().Border(BoxBorder.Rounded).Header("[bold]NEXT[/]"),
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
		
		void debugActivePiece() {
			// Place anywhere in the code to debug the active piece
			// Active piece will be marked yellow and the position anchor will be marked red
			if (debug) {
				board[activePiece.Position.Item1, activePiece.Position.Item2] = 'R';
				foreach ((int row, int col) in activePiece.Structure) {
					if (board[activePiece.Position.Item1 + row, activePiece.Position.Item2 + col] == 'A') {
						board[activePiece.Position.Item1 + row, activePiece.Position.Item2 + col] = 'Y';
					}
				}
			}
		}
		
		int checkFullLines() {

			// Start from bottom Y line
			for (int y = boardSizeY - 1; y >= 0; y--) {
				int blockCount = 0; // this stores how many blocks are in a single y axis line
				
				for (int x = 0; x < boardSizeX; x++) { // Count the blocks in the current line
					if (board[x, y] != 'A' && board[x, y] != ' ' ) blockCount++;
				}

				if (blockCount == boardSizeX) { // Line of blocks is full
					
					// Remove full line
					for (int x2 = 0; x2 < boardSizeX; x2++) {
						board[x2, y] = ' ';
					}
					
					// Move upper blocks 1 downward
					for (int y3 = y; y3 >= 1; y3--) {
                    	for (int x3 = 0; x3 < boardSizeX; x3++) {
                    		board[x3, y3] = board[x3, y3-1];
                    	}
                    }

					y++;
					points += level * 100;
					linesCleared++;
					level = (linesCleared / 10) + 1;
				}
			}

			return 1;
		}

		void rotatePiece() {
			
			if (activePiece.RectangularSize.Item1 == activePiece.RectangularSize.Item2) return; // Return if piece is a square
			
			int offsetX = 0;
			int offsetY = 0;
			int collisionCorrectionOffsetX = 0;
			int collisionCorrectionOffsetY = 0;
			
			var rotatedPieceStructure = activePiece.Structure.Select(coord => (-coord.Item2, coord.Item1)).ToList();
			
			if (activePiece.RectangularSize.Item1 > activePiece.RectangularSize.Item2 && activePiece.Rotation == 0) { // Check if piece x > y
				// x+1 & y+0 -> this is the new activePiece.Position of the piece and where it should be drawn
				offsetX = 1;
			} else if (activePiece.RectangularSize.Item1 < activePiece.RectangularSize.Item2 && activePiece.Rotation == 90) { // Check if piece x < y
				//offsetY = -1;
			} else if (activePiece.RectangularSize.Item1 > activePiece.RectangularSize.Item2 && activePiece.Rotation == 180) {
				offsetX = -1;
			} else if (activePiece.RectangularSize.Item1 < activePiece.RectangularSize.Item2 && activePiece.Rotation == 270) {
				//offsetY = 1;
			} else {
				return;
			}
			
			// Normalize the inverted coordinates (because right now they can contain negative values which fucks the game completely up)
			int minX = rotatedPieceStructure.Min(coord => coord.Item1);
			int minY = rotatedPieceStructure.Min(coord => coord.Item2);
			rotatedPieceStructure = rotatedPieceStructure.Select(coord => (coord.Item1 - minX, coord.Item2 - minY)).ToList();
			
			// Wall collision check & avoidance
			foreach (var coord in rotatedPieceStructure) {
				int x = activePiece.Position.Item1 + coord.Item1 + offsetX;
				int y = activePiece.Position.Item2 + coord.Item2 + offsetY;
				
				if (y >= boardSizeY || y < 0) return; // Return if y coord out of bounds

				// When rotating next to a wall, offset the rotation if it would go out of bounds
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
				new Piece(rotatedPieceStructure, activePiece.Color, (activePiece.RectangularSize.Item2, activePiece.RectangularSize.Item1)),
				(activePiece.Position.Item1 + offsetX + collisionCorrectionOffsetX, activePiece.Position.Item2 + offsetY),
				(activePiece.Rotation+90 >= 360) ? 0 : activePiece.Rotation += 90
			);
			
			gameFieldChanged = true;
		}

		void removeActivePiece() {
			for (int y = activePiece.Position.Item2; y <= activePiece.Position.Item2 + activePiece.RectangularSize.Item2; y++) {
				for (int x = activePiece.Position.Item1; x <= activePiece.Position.Item1 + activePiece.RectangularSize.Item1; x++) {
					board[x, y] = ' ';
				}
			}
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
				
				// Check if piece is going out of bounds on y axis
				freeze = activePiece.Position.Item2 + activePiece.RectangularSize.Item2 + moveY >= boardSizeY;
				
				// Check if there is a piece below
				if (!freeze) {
					foreach ((int row, int col) in activePiece.Structure) {
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
		
		Piece selectRandomPiece() {
			int pieceId = new Random().Next(0, pieces.Count);
			return pieces[pieceId];
		}

		void insertPiece(Piece piece) {
			foreach ((int row, int col) in piece.Structure) {
				board[row + ( (boardSizeX/2) - piece.RectangularSize.Item1 ), col] = 'A';
			}
			activePiece = new ActivePiece(piece, ((boardSizeX/2)-piece.RectangularSize.Item1 , 0), 0);
			// activePiece.Rotation = 0;
		}

		void drawNextPieceCanvas(Piece piece) {
			nextPieceCanvas = new Canvas(4, 4);
			int xOffset = 0;
			if (piece.RectangularSize.Item1 < 2) xOffset = 1; 
			foreach ((int row, int col) in piece.Structure) {
				nextPieceCanvas.SetPixel(row+xOffset, col+1, Color.Yellow);
			}
		}

		void drawBoard() {
			Console.Clear();
			
			for (int y = 0; y < boardSizeY; y++) {
				for (int x = 0; x < boardSizeX; x++) {
					// Console.Write(board[x, y]);
					
					if (board[x, y] == ' ') {
						// canvas.SetPixel(x, y, Color.White);
						canvas.SetPixel(x, y, Color.Default);
					} else if (board[x, y] == 'A') {
						canvas.SetPixel(x, y, Color.Red);
					} else if (board[x, y] == 'B') {
						canvas.SetPixel(x, y, Color.Blue);
					} else if (board[x, y] == 'Y') {
						canvas.SetPixel(x, y, Color.Yellow);
					} else if (board[x, y] == 'G') {
						canvas.SetPixel(x, y, Color.Green);
					} else if (board[x, y] == 'R') {
						canvas.SetPixel(x, y, Color.Red);
					} 
				}
			}
			
			Panel debugPanel = new Panel("").Border(BoxBorder.None); 
			
			if (debug) {
				debugPanel = new Panel(
					new Rows(
						new Markup("[bold yellow]POSITION[/]"),
						new Markup("X:" + activePiece.Position.Item1 + ",Y:" + activePiece.Position.Item2),
						new Markup("[bold yellow]RATIO[/]"),
						new Markup("X:" + activePiece.RectangularSize.Item1 + ",Y:" + activePiece.RectangularSize.Item2),
						new Markup("[bold yellow]ROTATION[/]"),
						new Markup(activePiece.Rotation.ToString() + " deg")
					)
				).Expand().Border(BoxBorder.Rounded).Header("[bold]DEBUG[/]");
			}
			
			var cols = new Columns(
				new Panel(canvas).Border(BoxBorder.Rounded),

				new Panel(
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
				).Border(BoxBorder.None)
			);
			
			var view = new Panel(cols).Border(BoxBorder.None);

			AnsiConsole.Write(Align.Center(view));
		}

		void initBoard() {
			for (int y = 0; y < boardSizeY; y++) {
				for (int x = 0; x < boardSizeX; x++) {
					board[x, y] = ' ';
				}
			}
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
					case ConsoleKey.Q:
						break;
				}
			}
		}
		
	}
}