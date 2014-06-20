using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace SudokuCS
{
	class Program
	{
		private const int MAX_NUMBER = 9;
		private const int COLUMNS = 9;
		private const int ROWS = 9;

		// 初期盤面
		// http://ja.wikipedia.org/wiki/%E6%95%B0%E7%8B%AC
		//private static int[][] initialBoard =	 
		//{
		//	new[] { 5, 3, 0, 0, 7, 0, 0, 0, 0 },
		//	new[] { 6, 0, 0, 1, 9, 5, 0, 0, 0 }, 
		//	new[] { 0, 9, 8, 0, 0, 0, 0, 6, 0 }, 
		//	new[] { 8, 0, 0, 0, 6, 0, 0, 0, 3 }, 
		//	new[] { 4, 0, 0, 8, 0, 3, 0, 0, 1 }, 
		//	new[] { 7, 0, 0, 0, 2, 0, 0, 0, 6 }, 
		//	new[] { 0, 6, 0, 0, 0, 0, 2, 8, 0 }, 
		//	new[] { 0, 0, 0, 4, 1, 9, 0, 0, 5 }, 
		//	new[] { 0, 0, 0, 0, 8, 0, 0, 7, 9 }, 
		//};
		// http://www.ac.auone-net.jp/~tagutis1/
		//private static int[][] initialBoard =
		//{
		//	new[] { 9, 0, 0, 0, 0, 8, 0, 0, 4 },
		//	new[] { 0, 1, 0, 0, 5, 0, 0, 2, 0 },
		//	new[] { 0, 0, 3, 0, 0, 0, 7, 0, 0 },
		//	new[] { 0, 0, 0, 2, 0, 9, 0, 0, 3 },
		//	new[] { 0, 6, 0, 0, 0, 0, 0, 8, 0 },
		//	new[] { 5, 0, 0, 7, 0, 4, 0, 0, 0 },
		//	new[] { 0, 0, 8, 0, 0, 0, 1, 0, 0 },
		//	new[] { 0, 2, 0, 0, 4, 0, 0, 6, 0 },
		//	new[] { 7, 0, 0, 3, 0, 0, 0, 0, 5 },
		//};
		// http://www.sudokugame.org/archive/printable.php?nd=1&y=2014&m=06&d=4
		private static int[][] initialBoard =	 
		{
			new[] { 0, 1, 0, 0, 9, 0, 0, 0, 0 },
			new[] { 5, 8, 2, 0, 0, 3, 0, 0, 4 },
			new[] { 0, 0, 0, 2, 0, 0, 3, 0, 0 },
			new[] { 0, 0, 0, 0, 0, 1, 0, 7, 0 },
			new[] { 3, 7, 8, 0, 2, 0, 5, 0, 0 },
			new[] { 0, 0, 4, 0, 0, 0, 0, 0, 2 },
			new[] { 0, 0, 1, 3, 8, 0, 0, 0, 6 },
			new[] { 7, 6, 9, 0, 0, 0, 0, 0, 0 },
			new[] { 0, 3, 0, 0, 4, 0, 0, 2, 7 },
		};
		/// <summary>
		/// 盤面の状態
		/// </summary>
		private Board board = new Board();

		/// <summary>
		/// 1マスの状態
		/// </summary>
		[Serializable]
		private class Piece : ICloneable
		{
			public Piece(int initValue)
			{
				// 可能性リストを作成
				if (initValue > 0)
					this.availableValues.Add(initValue);
				else
				{
					for (var i = 1; i <= MAX_NUMBER; ++i)
						this.availableValues.Add(i);
				}
			}
			/// <summary>
			/// 可能性リスト
			/// </summary>
			private List<int> availableValues = new List<int>(); 
			/// <summary>
			/// 今の(決定した)数値
			/// </summary>
			public int NowValue
			{
				get
				{
					if (this.availableValues.Count == 1)
						return this.availableValues[0];
					else
						return 0;
				}
				set
				{
					this.availableValues.Clear();
					this.availableValues.Add(value);
				}
			}
			/// <summary>
			/// 可能性のある数値
			/// </summary>
			public List<int> AvailableValues
			{
				get { return this.availableValues; }
				set { this.availableValues = value; }
			}

			public object Clone()
			{
				// シリアル化した内容を保持しておくためのMemoryStreamを作成
				using (MemoryStream stm = new MemoryStream())
				{
					// バイナリシリアル化を行うためのフォーマッタを作成
					BinaryFormatter formatter = new BinaryFormatter();
					// 現在のインスタンスをシリアル化してMemoryStreamに格納
					formatter.Serialize(stm, this);
					// ストリームの位置を先頭に戻す
					stm.Position = 0L;
					// MemoryStreamに格納されている内容を逆シリアル化する
					return formatter.Deserialize(stm);
				}
			}
		}

		/// <summary>
		/// ゲーム盤
		/// </summary>
		[Serializable]
		private class Board : ICloneable
		{
			private List<List<Piece>> board = new List<List<Piece>>();
 
			public Board()
			{
				// 初期盤面作成
				System.Diagnostics.Trace.Assert(initialBoard.Count() == ROWS);
				foreach (var row in initialBoard)
				{
					System.Diagnostics.Trace.Assert(row.Count() == COLUMNS);
					var pieces = new List<Piece>();
					pieces.AddRange(row.Select(piece => new Piece(piece)));
					board.Add(pieces);
				}
			}
			/// <summary>
			/// ゲームが終わったかどうか
			/// </summary>
			/// <returns></returns>
			public bool IsFinished()
			{
				// 盤上のすべての数字が埋まったか
				return board.All(line => line.All(piece => piece.NowValue > 0));
			}
			/// <summary>
			/// ゲームが不正な状態かどうか
			/// </summary>
			/// <returns></returns>
			public bool IsInvalid()
			{
				for (var y = 0; y < ROWS; ++y)
				{
					for (var x = 0; x < COLUMNS; ++x)
					{
						var piece = board[y][x];
						if (piece.NowValue > 0)
						{
							// 自分と同じ数値があったら不正
							Piece[] pieces = GetSameBoxPieces(x, y);
							if (pieces.Any(piece2 => piece.NowValue == piece2.NowValue))
								return true;
							pieces = GetSameColumnPieces(x, y);
							if (pieces.Any(piece2 => piece.NowValue == piece2.NowValue))
								return true;
							pieces = GetSameRowPieces(x, y);
							if (pieces.Any(piece2 => piece.NowValue == piece2.NowValue))
								return true;
						}
					}
				}
				return false;
			}

			/// <summary>
			/// 同じ3x3の中にいる自分以外のマスを返す
			/// </summary>
			/// <param name="x"></param>
			/// <param name="y"></param>
			/// <returns></returns>
			private Piece[] GetSameBoxPieces(int x, int y)
			{
				var r = new List<Piece>();
				for (var _x = x / 3 * 3; _x < x / 3 * 3 + 3; ++_x)
				{
					for (var _y = y / 3 * 3; _y < y / 3 * 3 + 3; ++_y)
					{
						if (_x != x || _y != y)
							r.Add(board[_y][_x]);
					}
				}
				return r.ToArray();
			}
			/// <summary>
			/// 同じ列の自分以外のマスを返す
			/// </summary>
			/// <param name="x"></param>
			/// <param name="y"></param>
			/// <returns></returns>
			private Piece[] GetSameColumnPieces(int x, int y)
			{
				var r = new List<Piece>();
				for (var _y = 0; _y < board.Count; ++_y)
				{
					if (_y != y)
						r.Add(board[_y][x]);
				}
				return r.ToArray();
			}
			/// <summary>
			/// 同じ行の自分以外のマスを返す
			/// </summary>
			/// <param name="x"></param>
			/// <param name="y"></param>
			/// <returns></returns>
			private Piece[] GetSameRowPieces(int x, int y)
			{
				var r = new List<Piece>();
				for (var _x = 0; _x < board[0].Count; ++_x)
				{
					if (_x != x)
						r.Add(board[y][_x]);
				}
				return r.ToArray();
			}
			/// <summary>
			/// 列の単純チェック
			/// </summary>
			/// <param name="x"></param>
			/// <param name="y"></param>
			/// <param name="piece"></param>
			/// <returns></returns>
			bool CheckColumn(int x, int y, Piece piece)
			{
				Piece[] sameColumPieces = GetSameColumnPieces(x, y);
				var count = piece.AvailableValues.Count;
				piece.AvailableValues = piece.AvailableValues.Except(from p in sameColumPieces select p.NowValue).ToList();
				if (count != piece.AvailableValues.Count)
					return true;
				return false;
			}
			/// <summary>
			/// 行の単純チェック
			/// </summary>
			/// <param name="x"></param>
			/// <param name="y"></param>
			/// <param name="piece"></param>
			/// <returns></returns>
			bool CheckRow(int x, int y, Piece piece)
			{
				Piece[] sameRowPieces = GetSameRowPieces(x, y);
				var count = piece.AvailableValues.Count;
				piece.AvailableValues = piece.AvailableValues.Except(from p in sameRowPieces select p.NowValue).ToList();
				if (count != piece.AvailableValues.Count)
					return true;
				return false;
			}
			/// <summary>
			/// 3x3ボックスの単純チェック
			/// </summary>
			/// <param name="x"></param>
			/// <param name="y"></param>
			/// <param name="piece"></param>
			/// <returns></returns>
			bool CheckBox(int x, int y, Piece piece)
			{
				Piece[] sameBoxPieces = GetSameBoxPieces(x, y);
				var count = piece.AvailableValues.Count;
				piece.AvailableValues = piece.AvailableValues.Except(from p in sameBoxPieces select p.NowValue).ToList();
				if (count != piece.AvailableValues.Count)
					return true;
				return false;
			}
			/// <summary>
			/// 単純チェック
			/// </summary>
			public bool CheckBoard()
			{
				bool flag = false;
				// 全部のマスを見る
				for (var y = 0; y < board.Count; ++y)
				{
					for (var x = 0; x < board[0].Count; ++x)
					{
						Piece piece = board[y][x];
						// まだ決まっていないマスについて
						if (piece.NowValue == 0)
						{
							if (CheckColumn(x, y, piece) || CheckRow(x, y, piece) || CheckBox(x, y, piece))
								flag = true;
						}
					}
				}
				return flag;
			}
			/// <summary>
			/// 総当たり
			/// </summary>
			/// <returns></returns>
			public bool SearchBoard(int nest, List<int> temp)
			{
				Print(nest);
				if (IsFinished())
				{
					Console.WriteLine("Finish");
					Print(nest);
					return true;
				}
				for (var y = 0; y < COLUMNS; ++y)
				{
					for (var x = 0; x < ROWS; ++x)
					{
						Piece piece = board[y][x];
						if (piece.NowValue == 0)
						{
							for (int n = 1; n <= 9; ++n)
							{
								piece.NowValue = n;
								if (IsInvalid())
									continue;
								// 再帰で別インスタンスを渡したいのでClone
								var board2 = (Board)Clone();
								var temp2 = new List<int>();
								temp2.AddRange(temp);
								temp2.Add(n);
								if (board2.SearchBoard(nest + 1, temp2))
									return true;
							}
						}
					}
				}
				return false;
			}
			/// <summary>
			/// 画面表示
			/// </summary>
			public void Print(int nest = 0)
			{
				var count = 0;
				for (var y = 0; y < ROWS; ++y)
				{
					if (y % 3 == 0)
						Console.WriteLine("-------------------");
					var line = board[y];
					for (var x = 0; x < COLUMNS; ++ x)
					{
						Console.Write(x % 3 == 0 ? "|" : " ");
						var piece = line[x];
						if (piece.NowValue > 0)
						{
							Console.Write(piece.NowValue.ToString());
							++count;
						}
						else
							Console.Write(".");
					}
					Console.WriteLine("|");
				}
				Console.WriteLine("------------------- -> nest = {0}, count = {1}\n", nest, count);
			}

			/// <summary>
			/// 現在のインスタンスのコピーである新しいオブジェクトを作成します。
			/// </summary>
			/// <returns>
			/// このインスタンスのコピーである新しいオブジェクト。
			/// </returns>
			public object Clone()
			{
				// シリアル化した内容を保持しておくためのMemoryStreamを作成
				using (MemoryStream stm = new MemoryStream())
				{
					// バイナリシリアル化を行うためのフォーマッタを作成
					BinaryFormatter formatter = new BinaryFormatter();
					// 現在のインスタンスをシリアル化してMemoryStreamに格納
					formatter.Serialize(stm, this);
					// ストリームの位置を先頭に戻す
					stm.Position = 0L;
					// MemoryStreamに格納されている内容を逆シリアル化する
					return formatter.Deserialize(stm);
				}
			}
		}

		static void Main(string[] args)
		{
			// ゲーム初期化
			var sw = System.Diagnostics.Stopwatch.StartNew();
			var board = new Board();
			board.Print();
			// ゲーム開始
			// 単純ロジックのあと、残りは総当たりで
			bool flag = true;
			while (flag)
			{
				flag = board.CheckBoard();
				board.Print();
			}
			board.SearchBoard(0, new List<int>());

			Console.WriteLine("[END] : {0}", sw.ElapsedMilliseconds);
			Console.ReadKey();
		}
	}
}
