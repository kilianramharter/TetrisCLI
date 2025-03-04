namespace ConsoleGame.Models;

public struct Piece {
	public List<(int, int)> Structure { get; set; } // how the piece is formed
	public char Color { get; set; } // a single character representing the color of the piece
	public (int, int) RectangularSize { get; set; } // the offset from the top left to the center in x and y coordinates

	public Piece(List<(int, int)> structure, char color, (int, int) rectangularSize) {
		Structure = structure; Color = color; RectangularSize = rectangularSize;
	}
}