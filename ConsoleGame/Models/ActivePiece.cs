namespace ConsoleGame.Models;

public struct ActivePiece(Piece piece, (int, int) position, int rotation) {
	public List<(int, int)> Structure { get; set; } = piece.Structure; // how the piece is formed
	public char Color { get; set; } = piece.Color; // a single character representing the color of the piece
	public (int, int) RectangularSize { get; set; } = piece.RectangularSize; // the offset from the top left to the center in x and y coordinates
	public (int, int) Position { get; set; } = position;
	public int Rotation { get; set; } = rotation;
}