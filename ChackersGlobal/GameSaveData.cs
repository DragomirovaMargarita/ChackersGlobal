using System;
using System.Collections.Generic;

namespace Chackers
{
    public class GameSaveData
    {
        public DateTime SaveDate { get; set; }
        public string SaveName { get; set; }
        public bool IsWhiteTurn { get; set; }
        public bool IsVsComputer { get; set; }
        public bool IsPlayerWhite { get; set; }
        public int WhiteActive { get; set; }
        public int BlackActive { get; set; }
        public Piece[,] Board { get; set; }
        public List<PieceData> Pieces { get; set; }
        public List<PieceData> CapturedPieces { get; set; }

        public GameSaveData()
        {
            Pieces = new List<PieceData>();
            SaveDate = DateTime.Now;
            Board = new Piece[8, 8];
        }
    }

    public class PieceData
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public bool IsWhite { get; set; }
        public bool IsKing { get; set; }
    }
} 