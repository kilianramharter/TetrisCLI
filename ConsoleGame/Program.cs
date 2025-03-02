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
		new Piece(new List<(int, int)> { (0, 0), (1, 0), (2, 0), (3, 0) }, 'B', (3, 1)), // 1x4 Line
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
			
			currentTime = DateTime.Now;
			timeDiff = currentTime - lastDownMovement;
			
			if (timeDiff.Milliseconds > gameTick) {
				movePiece(0, 1);
				lastDownMovement = DateTime.Now;
			}

			checkFullLines();
			level = (linesCleared / 10) + 1;
			//gameOver = checkGameOver();
			
		} while (!gameOver);


		bool checkGameOver() {

			
			
			return false;
		}
		
		int checkFullLines() {

			// Start from bottom Y line
			for (int y = boardSizeY - 1; y >= 0; y--) {
				int blockCount = 0; // this stores how many blocks are in a single y axis line
				
				for (int x = 0; x < boardSizeX; x++) { // Count the blocks in the current line
					if (board[x, y] == 'B') blockCount++;
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

		void rotatePieceNew2() {
			
		}

		void rotatePieceNew() {
			
			// Invert the rectangular size of the piece
			(int,int) invertedRectSize = (activePiece.RectangularSize.Item2, activePiece.RectangularSize.Item1);
			
			// Create temporary copy of the active piece, only 90 deg rotated
			var rotatedTempActivePiece = new ActivePiece(
				new Piece(
					activePiece.Structure.Select(coord => (coord.Item2, -coord.Item1)).ToList(), // Invert the coordinates
					activePiece.Color,
					invertedRectSize
				),
				activePiece.Position,
				activePiece.Rotation
			);
			
			
			// Remove current active piece - WORKING AND OPTIMIZED AF
			for (int y = activePiece.Position.Item2; y <= activePiece.Position.Item2 + activePiece.RectangularSize.Item2; y++) {
				for (int x = activePiece.Position.Item1; x <= activePiece.Position.Item1 + activePiece.RectangularSize.Item1; x++) {
					board[x, y] = ' ';
				}
			}
			
			/*for (int y = rotatedTempActivePiece.Position.Item2; y <= rotatedTempActivePiece.Position.Item2 + rotatedTempActivePiece.RectangularSize.Item2; y++) {
				for (int x = rotatedTempActivePiece.Position.Item1; x <= rotatedTempActivePiece.Position.Item1 + rotatedTempActivePiece.RectangularSize.Item1; x++) {
					board[x, y] = rotatedTempActivePiece.Structure;
				}
			}*/
			
			
			foreach ((int row, int col) in rotatedTempActivePiece.Structure) {
				board[ rotatedTempActivePiece.Position.Item1 + row , rotatedTempActivePiece.Position.Item2 + col ] = 'A';
			}
			
			
			
			
			
			

			/*(int, int) activePieceCenter = (0,0);
			
			if (activePiece.Rotation == 0 && activePiece.RectangularSize.Item2 < activePiece.RectangularSize.Item1) { // is y > x
				activePieceCenter = (
					activePiece.RectangularSize.Item1 - 1,
					activePiece.RectangularSize.Item2
				);
			} else if (activePiece.Rotation == 90 && activePiece.RectangularSize.Item2 > activePiece.RectangularSize.Item1) {
				activePieceCenter = (
					activePiece.RectangularSize.Item1 - 1,
					activePiece.RectangularSize.Item2 - 1
				);
			} else if (activePiece.Rotation == 180 && activePiece.RectangularSize.Item2 < activePiece.RectangularSize.Item1) {
				activePieceCenter = (
					activePiece.RectangularSize.Item1 - 1,
					activePiece.RectangularSize.Item2 - 1
				);
			} else if (activePiece.Rotation == 270 && activePiece.RectangularSize.Item2 > activePiece.RectangularSize.Item1) {
				activePieceCenter = (
					activePiece.RectangularSize.Item1,
					activePiece.RectangularSize.Item2 - 1
				);
			}

			for (int y = activePiece.Position.Item2 + activePieceCenter.Item2 - 1; y <= activePiece.Position.Item2 + activePieceCenter.Item2 + 1; y++) {
				for (int x = activePiece.Position.Item1 + activePieceCenter.Item1 - 1; x <= activePiece.Position.Item1 + activePieceCenter.Item1 + 1; x++) {
					
					board[x, y] = rotatedTempActivePiece.Structure.
					
				}
			}
			
			bool rotationSuccess = true;
			foreach ((int row, int col) in rotatedTempActivePiece.Structure) {
				

				Console.WriteLine("X:" + (row + activePieceCenter.Item1) + ", Y:" + (col + activePieceCenter.Item2) + ")");
				board[ activePieceCenter.Item1, col + activePieceCenter.Item2] = 'A';
			}

			return;*/
			
			//   =
			// ===
			//
			// =
			// =
			// ==
			//
			// (int, int) activePieceCenter = (
			// 	activePiece.RectangularSize.Item1 - 1,
			// 	activePiece.RectangularSize.Item2
			// );
			//
			//
			// (int, int) rotatedPieceCenter = (
			// 	activePiece.RectangularSize.Item1,
			// 	activePiece.RectangularSize.Item2
			// );
			
			if (activePiece.Rotation == 0 || activePiece.Rotation == 180) { // Piece has default given coordinates
			} else { // piece is turned 90 or 270 degrees -> invert activePiece.RectangularSize
			}
			
			
			
			
			
			
			
			
		}
		
		void rotatePiece() {
			var tempActivePiece = new ActivePiece(
				new Piece(
					activePiece.Structure.Select(coord => (coord.Item2, -coord.Item1)).ToList(), // Invert the coordinates
					activePiece.Color,
					activePiece.RectangularSize
				),
				activePiece.Position,
				activePiece.Rotation
			);
			char[,] tempBoard = new char[boardSizeX, boardSizeY]; // Create an empty, temporary copy of the board
			(int, int)? firstBlockOfPiece = null; // this is the "anchor" where the piece rotates
			(int, int)? lastBlockOfPiece = null;
			
			/*foreach ((int row, int col) in activePiece) {
				board[row, col] = 'A';
			}*/
			
			// Get anchor
			for (int y = 0; y < boardSizeY; y++) {
				if (firstBlockOfPiece != null) break;
				for (int x = 0; x < boardSizeX; x++) {
					if (board[x, y] == 'A') {
						if (firstBlockOfPiece == null) firstBlockOfPiece = (x, y); // This sets the coordinate for the rotated piece
						break;
					}
				}
			}
			
			/*
			for (int y = 0; y < boardSizeY; y++) {
				// if (firstBlockOfPiece != null) break;
				for (int x = 0; x < boardSizeX; x++) {
					if (board[x, y] == 'A') {
						if (firstBlockOfPiece == null) firstBlockOfPiece = (x, y); // This sets the coordinate for the rotated piece
						break;
					}
				}
			} */
			
			// Redraw rotated active pieces and check if they work
			bool rotationSuccess = true;
			foreach ((int row, int col) in tempActivePiece.Structure) {
				
				if ((row + firstBlockOfPiece.Value.Item1) < 0 || (row + firstBlockOfPiece.Value.Item1) > boardSizeX - 1) {
					rotationSuccess = false;
					break;
				}
				if ( (col + firstBlockOfPiece.Value.Item2) < 0 || (col + firstBlockOfPiece.Value.Item2) > boardSizeY - 1 ) {
					rotationSuccess = false;
					break;
				}
				tempBoard[row + firstBlockOfPiece.Value.Item1, col + firstBlockOfPiece.Value.Item2] = 'A';
			}

			if (!rotationSuccess) return;
			
			
			// Clear active pieces
			for (int y = 0; y < boardSizeY; y++) {
				for (int x = 0; x < boardSizeX; x++) {
					if (board[x, y] == 'A') {
						if (board[x, y] == 'A') board[x, y] = ' '; // This clears the active pieces from the board
					}
				}
			}
			
			// Copy tempBoard to mainBoard haha funny IT joke lmao
			for (int y = 0; y < boardSizeY; y++) {
				for (int x = 0; x < boardSizeX; x++) {
					if (board[x, y] == 'A') board[x, y] = ' ';
					if (tempBoard[x, y] == 'A') board[x, y] = 'A';
				}
			}
			
			// Replace the old activePiece with the modified copy
			activePiece = tempActivePiece;
			
			// Set the piece rotation variable
			if (activePiece.Rotation + 90 >= 360) {
				activePiece.Rotation = 0;
			} else {
				activePiece.Rotation += 90;
			}
			
			gameFieldChanged = true;
		}
		
		
		void movePiece(int moveX, int moveY) {
			char[,] tempBoard = new char[boardSizeX, boardSizeY]; // Empty copy of the game board
			bool lockAllPieces = false; // "Locking" refers to making the current active piece solid and not movable
			
			for (int y = 0; y < boardSizeY; y++) {
				if (lockAllPieces) break;
				for (int x = 0; x < boardSizeX; x++) {
					if (lockAllPieces) break;
					
					if (board[x, y] == 'A') { // Check if the cell is an "active" cell (cell char value == A)
						
						/* HORIZONTAL MOVEMENT */
						if (moveX != 0) { // Check if the user wants to move left or right
							// if (x+moveX >= boardSizeX-1 && x-moveX < 0) break;

							if (!((x - moveX) < 0)) { // Check if movements will be out of bounds (e.g. -1)
								tempBoard[x, y] = board[x - moveX, y];
							} else { // If movement is out of bounds, just set to empty
								tempBoard[x, y] = ' ';	
							}
							tempBoard[x + moveX, y] = board[x, y];
						}

						/* VERTICAL MOVEMENT */
						if (moveY != 0) {
							if ((y + moveY) >= boardSizeY) { // If piece touches bottom
								lockAllPieces = true;
								generateNewPiece = true;
							} else if ( board[x, y + moveY] != ' ' && board[x, y + moveY] != 'A' ) { // If piece touches other piece below it
								lockAllPieces = true;
								generateNewPiece = true;
							} else { // If piece can move down 
								// Move piece further down
								tempBoard[x, y + moveY] = board[x, y];
							}
						}

					}
				}
			}
			
			// Update x + y position 
			var pos = activePiece.Position; // Make a copy of the position
			pos.Item1 += moveX;
			pos.Item2 += moveY;
			activePiece.Position = pos; // Write back the modified copy
			
			// Check if piecelock is enabled
			if (lockAllPieces) { // if yes, lock moving pieces
				for (int y = 0; y < boardSizeY; y++) {
					for (int x = 0; x < boardSizeX; x++) {
						if (board[x, y] == 'A') board[x, y] = 'B';
					}
				}
			} else { // if no, copy tempBoard to mainBoard
				// Copy tempBoard to mainBoard haha funny IT joke lmao
				for (int y = 0; y < boardSizeY; y++) {
					for (int x = 0; x < boardSizeX; x++) {
						if (board[x, y] == 'A') board[x, y] = ' ';
						if (tempBoard[x, y] == 'A') board[x, y] = 'A';
					}
				}
			}
			
			gameFieldChanged = true;

		}
		
		
		

		Piece selectPiece() {
			int pieceId = new Random().Next(0, pieces.Count);
			return pieces[pieceId];
		}

		void insertPiece(Piece piece) {
			foreach ((int row, int col) in piece.Structure) {
				//board[row, col] = 'A';
				board[row + ( (boardSizeX/2) - piece.RectangularSize.Item1 ), col] = 'A';
			}
			activePiece = new ActivePiece(piece, (0, 0), 0);
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