namespace ConsoleGame;
using Spectre.Console;

class Program {
	enum GameControl { Left, Right, Down, Rotate, None, Exit }
	
	static void Main(string[] args) {

		// Original Tetris is 10x20
		
		/* SETTINGS */
		int boardSizeX = 10;
		int boardSizeY = 20;
		const int gameTick = 300;
		
		/* SCORE SYSTEM VARS */
		int level = 1;
		int points = 0;
		int linesCleared = 0;
		
		/* GAME LOGIC VARS */
		char[,] board = new char[boardSizeX, boardSizeY];
		List<(int, int)> activePiece = new List<(int, int)>();
		int activePieceRotation = 0;
		
		var canvas = new Canvas(boardSizeX, boardSizeY);
		bool gameOver = false;
		bool generateNewBlock = true;
		bool gameFieldChanged = true;
		
		/* GAME TICKER VARS */
		DateTime lastDownMovement = DateTime.Now;
		DateTime currentTime;
		TimeSpan timeDiff;
		
		initBoard();
		
		do {
			if (generateNewBlock) {
				insertBlock( selectBlock() );
				generateNewBlock = false;
			}

			if (gameFieldChanged) {
				draw();
				gameFieldChanged = false;
			}
			
			GameControl input = readUserInput();

			switch (input) {
				case GameControl.Exit:
					Console.WriteLine("GAME EXIT");
					return;
				case GameControl.Left:
					move(-1,0);
					break;
				case GameControl.Down:
					move(0,1);
					break;
				case GameControl.Rotate:
					rotate();
					break;
				case GameControl.Right:
					move(1,0);
					break;
			}
			
			currentTime = DateTime.Now;
			timeDiff = currentTime - lastDownMovement;
			
			if (timeDiff.Milliseconds > gameTick) {
				move(0, 1);
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

				int blockCount = 0;
				
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
		
		void rotate() {
			List<(int, int)> tempActivePiece = activePiece.Select(coord => (coord.Item2, -coord.Item1)).ToList();; // Create a temporary copy of the active moving piece
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
			
			
			for (int y = 0; y < boardSizeY; y++) {
				// if (firstBlockOfPiece != null) break;
				for (int x = 0; x < boardSizeX; x++) {
					if (board[x, y] == 'A') {
						if (firstBlockOfPiece == null) firstBlockOfPiece = (x, y); // This sets the coordinate for the rotated piece
						break;
					}
				}
			}
			
			// Redraw rotated active pieces and check if they work
			bool rotationSuccess = true;
			foreach ((int row, int col) in tempActivePiece) {
				
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
			
			activePiece = tempActivePiece;
			gameFieldChanged = true;
		}
		
		
		void move(int moveX, int moveY) {
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
								generateNewBlock = true;
							} else if ( board[x, y + moveY] != ' ' && board[x, y + moveY] != 'A' ) { // If piece touches other piece below it
								lockAllPieces = true;
								generateNewBlock = true;
							} else { // If piece can move down 
								// Move piece further down
								tempBoard[x, y + moveY] = board[x, y];
							}
						}

					}
				}
			}
			
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
		
		void draw() {
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
					
					switch (board[x, y]) {
						case ' ':
							break;
						case 'A':
							break;
						case 'B':
							break;
						case 'C':
							break;
						
					}
					
					if (board[x, y] == ' ') {
						canvas.SetPixel(x, y, Color.White);	
					} else if (board[x, y] == 'A') {
						canvas.SetPixel(x, y, Color.Red);
					} else if (board[x, y] == 'B') {
						canvas.SetPixel(x, y, Color.Blue);
					}
					
				}
				// Console.WriteLine();
			}
			AnsiConsole.Write(canvas);
			Console.WriteLine("Points: " + points);
			Console.WriteLine("Level: " + level);
			Console.WriteLine("Lines: " + linesCleared);
		}
		

		List<(int, int)> selectBlock() {
			// Structure = how the piece is formed
			// Color = a single character representing the color of the piece
			// CenterOffset = the offset from the top left to the center in x and y coordinates
			/* var blocks = new List<(List<(int, int)> Structure, char Color, (int, int) CenterOffset)>();
			blocks.Add( (new List<(int, int)> { (0,0), (1,0), (2,0), (3,0) }, 'A', (1,1)) ); // 1x4 Line
			blocks.Add( (new List<(int, int)> { (0,0), (1,0), (0,1), (1,1) }, 'B', (1,1)) ); // 2x2 Square
			blocks.Add( (new List<(int, int)> { (0,0), (1,0), (2,0), (2,1) }, 'C', (1,1)) ); // L
			blocks.Add( (new List<(int, int)> { (0,0), (1,0), (2,0), (0,1) }, 'D', (1,1)) ); // L reverse
			blocks.Add( (new List<(int, int)> { (0,1), (1,1), (2,1), (1,0) }, 'E', (1,1)) ); // Pyramid
			blocks.Add( (new List<(int, int)> { (1,0), (2,0), (0,1), (1,1) }, 'F', (1,1)) ); 
			blocks.Add( (new List<(int, int)> { (0,0), (1,0), (1,1), (2,1) }, 'G', (1,1)) ); */ 
			
			List<List<(int, int)>> blocks = new List<List<(int, int)>>();
			blocks.Add(new List<(int, int)> { (0,0), (1,0), (2,0), (3,0) }); // 1x4 Line
			blocks.Add(new List<(int, int)> { (0,0), (1,0), (0,1), (1,1) }); // 2x2 Square
			blocks.Add(new List<(int, int)> { (0,0), (1,0), (2,0), (2,1) }); // L
			blocks.Add(new List<(int, int)> { (0,0), (1,0), (2,0), (0,1) }); // L reverse
			blocks.Add(new List<(int, int)> { (0,1), (1,1), (2,1), (1,0) }); // Pyramid
			blocks.Add(new List<(int, int)> { (1,0), (2,0), (0,1), (1,1) }); 
			blocks.Add(new List<(int, int)> { (0,0), (1,0), (1,1), (2,1) });
				 
			int blockId = new Random().Next(0, blocks.Count);
			activePiece = blocks[blockId];
			return blocks[blockId];
		}

		void insertBlock(List<(int, int)> piece) {
			foreach ((int row, int col) in piece) {
				board[row, col] = 'A';
			}
		}

		GameControl readUserInput() {
			ConsoleKeyInfo input;

			if (Console.KeyAvailable) {
				input = Console.ReadKey(true);
				
				switch (input.Key) {
					case ConsoleKey.LeftArrow:
						// Console.WriteLine("Left");
						return GameControl.Left;
						break;
					case ConsoleKey.RightArrow:
						// Console.WriteLine("Right");
						return GameControl.Right;
						break;
					case ConsoleKey.DownArrow:
						return GameControl.Down;
						break;
					case ConsoleKey.Spacebar:
						// Console.WriteLine("Spacebar");
						return GameControl.Rotate;
						break;
					case ConsoleKey.Q:
						return GameControl.Exit;
						break;
				}
			}

			return GameControl.None;
		}

		void initBoard() {
			for (int y = 0; y < boardSizeY; y++) {
				for (int x = 0; x < boardSizeX; x++) {
					board[x, y] = ' ';
				}
			}
		}
		
	}
}