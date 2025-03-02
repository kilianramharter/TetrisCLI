using System.Collections;

namespace ConsoleGame;
using Spectre.Console;

class Program {
	
	public struct Piece {
		public List<(int, int)> Structure { get; set; } // how the piece is formed
		public char Color { get; set; } // a single character representing the color of the piece
		public (int, int) RectangularSize { get; set; } // the offset from the top left to the center in x and y coordinates

		public Piece(List<(int, int)> structure, char color, (int, int) rectangularSize) {
			Structure = structure; Color = color; RectangularSize = rectangularSize;
		}
	}

	public struct ActivePiece(Piece piece, (int, int) position, int rotation) {
		public List<(int, int)> Structure { get; set; } = piece.Structure; // how the piece is formed
		public char Color { get; set; } = piece.Color; // a single character representing the color of the piece
		public (int, int) RectangularSize { get; set; } = piece.RectangularSize; // the offset from the top left to the center in x and y coordinates
		public (int, int) Position { get; set; } = position;
		public int Rotation { get; set; } = rotation;
	}
	
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
	
	static void Main(string[] args) {

		// Original Tetris is 10x20
		
		/* GAME SETTINGS */
		const int boardSizeX = 10;
		const int boardSizeY = 20;
		int gameTick = 300;
		
		/* SCORE SYSTEM VARS */
		int level = 1;
		int points = 0;
		int linesCleared = 0;
		
		/* GAME LOGIC VARS */
		char[,] board = new char[boardSizeX, boardSizeY];
		var canvas = new Canvas(boardSizeX, boardSizeY);
		ActivePiece activePiece = new ActivePiece();
		bool gameOver = false;
		bool generateNewPiece = true;
		bool gameFieldChanged = true;
		
		/* GAME TICKER VARS */
		DateTime lastDownMovement = DateTime.Now;
		DateTime currentTime;
		TimeSpan timeDiff;
		
		initBoard();
		
		do {
			if (generateNewPiece) {
				insertPiece( selectPiece() );
				activePiece.Rotation = 0;
				generateNewPiece = false;
			}

			if (gameFieldChanged) {
				drawBoard();
				gameFieldChanged = false;
			}
			
			currentTime = DateTime.Now;
			timeDiff = currentTime - lastDownMovement;
			
			if (timeDiff.Milliseconds > gameTick) {
				movePiece(0, 1);
				lastDownMovement = DateTime.Now;
			}

			checkFullLines();
			level = (linesCleared / 10) + 1;
			
			GameControl input = readUserInput();

			switch (input) {
				case GameControl.Exit:
					Console.WriteLine("GAME EXIT");
					return;
				case GameControl.Left:
					movePiece(-1,0);
					break;
				case GameControl.Down:
					movePiece(0,1);
					break;
				case GameControl.Rotate:
					rotatePiece();
					break;
				case GameControl.Right:
					movePiece(1,0);
					break;
			}
			
			
			//gameOver = checkGameOver();
			
		} while (!gameOver);


		void debugActivePiece() {
			// Place anywhere in the code to debug the active piece
			// Active piece will be marked yellow and the position anchor will be marked red
			board[activePiece.Position.Item1, activePiece.Position.Item2] = 'R';
			foreach ((int row, int col) in activePiece.Structure) {
				if (board[activePiece.Position.Item1 + row, activePiece.Position.Item2 + col] == 'A') {
					board[activePiece.Position.Item1 + row, activePiece.Position.Item2 + col] = 'Y';
				}
			}
		}
		
		bool checkGameOver() {
			// TODO: Check if player can't continue - store highscore and exit to main screen
			return false;
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
					for (int y3 = y; y >= 1; y--) {
						for (int x3 = 0; x3 < boardSizeX; x3++) {
							board[x3, y] = board[x3, y-1];
						}
					}

					points += level * 100;
					linesCleared++;
				}
			}

			return 1;
		}

		void rotatePiece() {

			// Pyramid normal: (0, 1), (1, 1), (2, 1), (1, 0)
			// Pyramid inverted: (-1, 0), (-1, 1), (-2, 1), (-1, 0)
			
			
			int offsetX = 0;
			int offsetY = 0;
			var rotatedPieceStructure = activePiece.Structure.Select(coord => (-coord.Item2, coord.Item1)).ToList();
			
			// Return if piece is a square
			if (activePiece.RectangularSize.Item1 == activePiece.RectangularSize.Item2) return;
			
			
			if (activePiece.RectangularSize.Item1 > activePiece.RectangularSize.Item2 && activePiece.Rotation == 0) { // Check if piece x > y
				// x+1 & y+0 -> this is the new activePiece.Position of the piece and where it should be drawn
				offsetX = 1;
			} else if (activePiece.RectangularSize.Item1 < activePiece.RectangularSize.Item2 && activePiece.Rotation == 90) { // Check if piece x < y
				offsetY = -1;
			} else if (activePiece.RectangularSize.Item1 > activePiece.RectangularSize.Item2 && activePiece.Rotation == 180) {
				offsetX = -1;
			} else if (activePiece.RectangularSize.Item1 < activePiece.RectangularSize.Item2 && activePiece.Rotation == 270) {
				offsetY = 1;
			} else {
				return;
			}
			
			// Normalize the inverted coordinates (because right now they can contain negative values which fucks the game completely up)
			// Find the minimum x and y values
			int minX = rotatedPieceStructure.Min(coord => coord.Item1);
			int minY = rotatedPieceStructure.Min(coord => coord.Item2);
			rotatedPieceStructure = rotatedPieceStructure
				.Select(coord => (coord.Item1 - minX, coord.Item2 - minY))
				.ToList();
			
			
			// Collision check
			foreach (var coord in rotatedPieceStructure) {
				int x = activePiece.Position.Item1 + coord.Item1 + offsetX;
				int y = activePiece.Position.Item2 + coord.Item2 + offsetY;

				if (x >= boardSizeX || x < 0) return;
				if (y >= boardSizeY || y < 0) return;

				if (board[x, y] != ' ' && board[x, y] != 'A') return;
			}
			
			removeActivePiece();
			
			// Draw rotated piece
			foreach (var coord in rotatedPieceStructure) {
				board[activePiece.Position.Item1 + coord.Item1 + offsetX, activePiece.Position.Item2 + coord.Item2 + offsetY] = 'A';
			}
			
			// updateActivePiecePosition(activePiece.Position.Item1 + offsetX, activePiece.Position.Item2 + offsetY);
			activePiece.Position = (activePiece.Position.Item1 + offsetX, activePiece.Position.Item2 + offsetY);
			activePiece.Structure = rotatedPieceStructure;
			activePiece.Rotation += 90;
			if (activePiece.Rotation == 360) activePiece.Rotation = 0;
			activePiece.RectangularSize = (activePiece.RectangularSize.Item2, activePiece.RectangularSize.Item1);
			
			gameFieldChanged = true;
			

		}

		void removeActivePiece() {
			// Remove active piece
			for (int y = activePiece.Position.Item2; y <= activePiece.Position.Item2 + activePiece.RectangularSize.Item2; y++) {
				for (int x = activePiece.Position.Item1; x <= activePiece.Position.Item1 + activePiece.RectangularSize.Item1; x++) {
					board[x, y] = ' ';
				}
			}
		}

		void updateActivePiecePosition(int moveX, int moveY) {
			var pos = activePiece.Position; // Make a copy of the position
			pos.Item1 = moveX;
			pos.Item2 = moveY;
			activePiece.Position = pos; // Write back the modified copy
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
				bool freeze = false; // if true, piece will be "frozen" and a new one will spawn on top of the map
				
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
				
			updateActivePiecePosition(activePiece.Position.Item1 + moveX, activePiece.Position.Item2 + moveY);
			gameFieldChanged = true;
		}
		

		Piece selectPiece() {
			int pieceId = new Random().Next(0, pieces.Count);
			return pieces[pieceId];
		}

		void insertPiece(Piece piece) {
			foreach ((int row, int col) in piece.Structure) {
				board[row + ( (boardSizeX/2) - piece.RectangularSize.Item1 ), col] = 'A';
			}
			activePiece = new ActivePiece(piece, ((boardSizeX/2)-piece.RectangularSize.Item1 , 0), 0);
		}
		

		void drawBoard() {
			Console.Clear();
			
			/* BAREBONES_RENDERER */
			/*for (int y = 0; y < boardSizeY; y++) {
				for (int x = 0; x < boardSizeX; x++) {
					Console.Write(board[x, y]);
				}
				Console.WriteLine();
			}*/
			
			/* CANVAS_RENDERER */
			for (int y = 0; y < boardSizeY; y++) {
				for (int x = 0; x < boardSizeX; x++) {
					// Console.Write(board[x, y]);
					
					/*switch (board[x, y]) {
						case ' ':
							break;
						case 'A':
							break;
						case 'B':
							break;
						case 'C':
							break;
					}*/
					
					if (board[x, y] == ' ') {
						canvas.SetPixel(x, y, Color.White);	
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
				// Console.WriteLine();
			}
			AnsiConsole.Write(canvas);
			Console.WriteLine("Points: " + points);
			Console.WriteLine("Level: " + level);
			Console.WriteLine("Lines: " + linesCleared);
			Console.WriteLine("ActivePieceRotation: " + activePiece.Rotation);
			Console.WriteLine("ActivePiecePosition: (X:" + activePiece.Position.Item1 + ", Y:" + activePiece.Position.Item2 + ")");
			Console.WriteLine("RectangularSize: (X:" + activePiece.RectangularSize.Item1 + ", Y:" + activePiece.RectangularSize.Item2 + ")");
		}

		void initBoard() {
			for (int y = 0; y < boardSizeY; y++) {
				for (int x = 0; x < boardSizeX; x++) {
					board[x, y] = ' ';
				}
			}
		}
		
		GameControl readUserInput() {
			ConsoleKeyInfo input;

			if (Console.KeyAvailable) {
				input = Console.ReadKey(true);
				
				switch (input.Key) {
					case ConsoleKey.LeftArrow:
						return GameControl.Left;
						break;
					case ConsoleKey.RightArrow:
						return GameControl.Right;
						break;
					case ConsoleKey.DownArrow:
						return GameControl.Down;
						break;
					case ConsoleKey.Spacebar:
						return GameControl.Rotate;
						break;
					case ConsoleKey.Q:
						return GameControl.Exit;
						break;
				}
			}

			return GameControl.None;
		}
		
	}
}