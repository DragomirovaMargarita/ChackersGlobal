using System;
using System.Collections.Generic;
using System.Linq;

namespace Chackers
{
    public class ComputerPlayer
    {
        private readonly GameBoard gameBoard;
        private readonly bool isWhite;
        private readonly Random random;

        public ComputerPlayer(GameBoard gameBoard, bool isWhite)
        {
            this.gameBoard = gameBoard;
            this.isWhite = isWhite;
            this.random = new Random();
        }

        public Move GetNextMove()
        {
            var moves = GetAllPossibleMoves();
            if (moves.Count > 0)
            {
                var move = moves[random.Next(moves.Count)];
                return new Move(move.Item1, move.Item2, move.Item3, move.Item4);
            }
            return null;
        }

        private List<Tuple<int, int, int, int>> GetAllPossibleMoves()
        {
            var moves = new List<Tuple<int, int, int, int>>();
            Piece[,] board = gameBoard.GetBoard();

            
            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    Piece piece = board[row, col];
                    if (piece != null && piece.Color == (isWhite ? PieceColor.White : PieceColor.Black))
                    {
                        
                        int[] rowOffsets = piece.IsKing ? new int[] { -1, 1 } : new int[] { isWhite ? -1 : 1 };
                        int[] colOffsets = new int[] { -1, 1 };

                        foreach (int rowOffset in rowOffsets)
                        {
                            foreach (int colOffset in colOffsets)
                            {
                                
                                int newRow = row + rowOffset;
                                int newCol = col + colOffset;
                                if (IsValidMove(row, col, newRow, newCol))
                                {
                                    moves.Add(new Tuple<int, int, int, int>(row, col, newRow, newCol));
                                }

                                
                                newRow = row + (rowOffset * 2);
                                newCol = col + (colOffset * 2);
                                if (IsValidCapture(row, col, newRow, newCol))
                                {
                                    moves.Add(new Tuple<int, int, int, int>(row, col, newRow, newCol));
                                }
                            }
                        }
                    }
                }
            }

            
            var capturingMoves = moves.Where(move => 
                Math.Abs(move.Item3 - move.Item1) == 2).ToList();
            
            return capturingMoves.Count > 0 ? capturingMoves : moves;
        }

        private bool IsValidMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            if (toRow < 0 || toRow >= 8 || toCol < 0 || toCol >= 8)
                return false;

            Piece[,] board = gameBoard.GetBoard();
            Piece piece = board[fromRow, fromCol];

            
            if (!piece.IsKing)
            {
                int direction = isWhite ? -1 : 1;
                if ((toRow - fromRow) * direction <= 0)
                    return false;
            }

            return board[toRow, toCol] == null;
        }

        private bool IsValidCapture(int fromRow, int fromCol, int toRow, int toCol)
        {
            if (toRow < 0 || toRow >= 8 || toCol < 0 || toCol >= 8)
                return false;

            Piece[,] board = gameBoard.GetBoard();
            int jumpRow = (fromRow + toRow) / 2;
            int jumpCol = (fromCol + toCol) / 2;

            Piece jumpedPiece = board[jumpRow, jumpCol];
            Piece movingPiece = board[fromRow, fromCol];

            
            if (!movingPiece.IsKing)
            {
                int direction = isWhite ? -1 : 1;
                if ((toRow - fromRow) * direction <= 0)
                    return false;
            }

            return jumpedPiece != null && 
                   jumpedPiece.Color != movingPiece.Color && 
                   board[toRow, toCol] == null;
        }
    }

    public class Move
    {
        public int FromRow { get; }
        public int FromCol { get; }
        public int ToRow { get; }
        public int ToCol { get; }
        public bool IsCapture => Math.Abs(ToRow - FromRow) == 2;

        public Move(int fromRow, int fromCol, int toRow, int toCol)
        {
            FromRow = fromRow;
            FromCol = fromCol;
            ToRow = toRow;
            ToCol = toCol;
        }
    }
} 