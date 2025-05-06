using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace Chackers
{
    public class GameBoard
    {
        private const int BoardSize = 8;
        private Piece[,] board;
        private bool isWhiteTurn;

        public GameBoard()
        {
            board = new Piece[BoardSize, BoardSize];
            isWhiteTurn = true;
            InitializeBoard();
        }

        private void InitializeBoard()
        {
            System.Diagnostics.Debug.WriteLine("Initializing board...");
            // Очищаем доску
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    board[row, col] = null;
                }
            }

            // Расставляем черные шашки (верхние три ряда)
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    if ((row + col) % 2 == 1)
                    {
                        board[row, col] = new Piece(PieceColor.Black, row, col);
                        System.Diagnostics.Debug.WriteLine($"Placed black piece at [{row},{col}]");
                    }
                }
            }

            // Расставляем белые шашки (нижние три ряда)
            for (int row = 5; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    if ((row + col) % 2 == 1)
                    {
                        board[row, col] = new Piece(PieceColor.White, row, col);
                        System.Diagnostics.Debug.WriteLine($"Placed white piece at [{row},{col}]");
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine("Board initialization completed");
        }

        public bool IsValidMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            if (fromRow < 0 || fromRow >= BoardSize || fromCol < 0 || fromCol >= BoardSize ||
                toRow < 0 || toRow >= BoardSize || toCol < 0 || toCol >= BoardSize)
                return false;

            Piece piece = board[fromRow, fromCol];
            if (piece == null || piece.Color != (isWhiteTurn ? PieceColor.White : PieceColor.Black))
                return false;

            if (board[toRow, toCol] != null)
                return false;

            int rowDiff = Math.Abs(toRow - fromRow);
            int colDiff = Math.Abs(toCol - fromCol);
            if (rowDiff != colDiff)
                return false;

            if (rowDiff == 1)
            {
                if (piece.IsKing)
                    return true;
                return (piece.Color == PieceColor.White && toRow < fromRow) ||
                       (piece.Color == PieceColor.Black && toRow > fromRow);
            }

            if (rowDiff == 2)
            {
                int middleRow = (fromRow + toRow) / 2;
                int middleCol = (fromCol + toCol) / 2;
                Piece middlePiece = board[middleRow, middleCol];

                if (middlePiece == null || middlePiece.Color == piece.Color)
                    return false;

                return true;
            }

            return false;
        }

        public bool MakeMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            if (!IsValidMove(fromRow, fromCol, toRow, toCol))
                return false;

            Piece piece = board[fromRow, fromCol];
            board[fromRow, fromCol] = null;
            board[toRow, toCol] = piece;
            piece.Row = toRow;
            piece.Col = toCol;

            if (!piece.IsKing && ((piece.Color == PieceColor.White && toRow == 0) ||
                                (piece.Color == PieceColor.Black && toRow == BoardSize - 1)))
            {
                piece.IsKing = true;
            }

            int rowDiff = Math.Abs(toRow - fromRow);
            if (rowDiff == 2)
            {
                int middleRow = (fromRow + toRow) / 2;
                int middleCol = (fromCol + toCol) / 2;
                board[middleRow, middleCol] = null;
            }

            isWhiteTurn = !isWhiteTurn;
            return true;
        }

        public Piece[,] GetBoard()
        {
            return board;
        }

        public bool IsWhiteTurn()
        {
            return isWhiteTurn;
        }

        public void LoadFromSaveData(GameSaveData saveData)
        {
            board = saveData.Board;
            isWhiteTurn = saveData.IsWhiteTurn;
        }

        public void ForceSwitchTurn()
        {
            isWhiteTurn = !isWhiteTurn;
        }

        public void SwitchTurn()
        {
            isWhiteTurn = !isWhiteTurn;
        }

        public void SetPiece(int row, int col, bool isWhite, bool isKing)
        {
            System.Diagnostics.Debug.WriteLine($"Setting piece at [{row},{col}]: {(isWhite ? "White" : "Black")} {(isKing ? "King" : "Regular")}");
            board[row, col] = new Piece(isWhite ? PieceColor.White : PieceColor.Black, row, col);
            board[row, col].IsKing = isKing;
        }

        public void SetWhiteTurn(bool isWhiteTurn)
        {
            this.isWhiteTurn = isWhiteTurn;
        }

        public void ClearBoard()
        {
            System.Diagnostics.Debug.WriteLine("Clearing board...");
            for (int row = 0; row < 8; row++)
                for (int col = 0; col < 8; col++)
                    board[row, col] = null;
            System.Diagnostics.Debug.WriteLine("Board cleared");
        }
    }

    public class Piece
    {
        public PieceColor Color { get; }
        public bool IsKing { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }

        public Piece(PieceColor color, int row, int col)
        {
            Color = color;
            IsKing = false;
            Row = row;
            Col = col;
        }
    }

    public enum PieceColor
    {
        White,
        Black
    }
} 