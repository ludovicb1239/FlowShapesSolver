using Emgu.CV.Reg;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace FlowShapesSolver
{
    public struct Point
    {
        // Representation of a point in a 2D grid.
        public int x { get; set; }
        public int y { get; set; }

        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
    public class Cell
    {
        // Representation of a cell in a Flow Free puzzle.
        // Each cell has a color, the two other cells it is connected to, the direction of the connection, and the possible directions it can go.
        public Color Color { get; set; }
        public bool isCellDot { get; set; }
        public bool isRecievingCellDot { get; set; }
        public Cell? ConnectedCell { get; set; }
        public Cell? PrevConnectedCell { get; set; }
        public List<Cell> Neighbors { get; }
        public Point Position { get; set; }
        public bool isOrphan => (isCellDot && Neighbors.Count == 0) || Neighbors.Count == 1;

        public Cell(Color color, Point position)
        {
            Color = color;
            Position = position;
            isCellDot = color != Color.Black;
            Neighbors = new();
            isRecievingCellDot = false;
        }
        public void AddNeighbor(Cell neighbor)
        {
            if (neighbor == this)
                return;
            if (Neighbors.Contains(neighbor))
                return;
            Neighbors.Add(neighbor);
            neighbor.AddNeighbor(this);
        }
        public bool IsValid()
        {
            // Check if the cell is valid.
            if (isCellDot)
            {
                return (isRecievingCellDot || ConnectedCell != null);
            }
            else
            {
                return (ConnectedCell != null);
            }
        }
        public bool CanConnect(Cell cell)
        {
            if (this.Color != Color.Black)
            {
                if (cell.Color != Color.Black && cell.Color != this.Color)
                    return false; // Illegal connection
            }
            if (!Neighbors.Contains(cell))
                return false; // Cell is not a neighbor
            if (cell == ConnectedCell)
                return false; // Existing connection
            if (cell.PrevConnectedCell == this)
                return false; // Loop
            if (cell.ConnectedCell == this)
                return false; // Loop
            if (cell.PrevConnectedCell != null)
                return false; // Cell is already connected to another one
            if (cell.isCellDot && !cell.isRecievingCellDot)
                return false; // Illegal connection
            if (isCellDot)
            {
                return (ConnectedCell == null) && !isRecievingCellDot; // Cell is already connected to another one
            }
            else
            {
                return (ConnectedCell == null);
            }
        }
        public bool tryConnect(Cell cell)
        {
            if (!CanConnect(cell))
                return false;
            Connect(cell);
            return true;
        }
        public void Connect(Cell cell)
        {
            if (cell == ConnectedCell)
                return; // Existing connection
            ConnectedCell = cell;
            cell.PrevConnectedCell = this;
            if (cell.isCellDot || (Color == Color.Black && cell.Color != Color.Black))
                ChangeColorBackwards(cell.Color);
            else if (Color != Color.Black)
                cell.ChangeColor(Color);
        }
        public void Disconnect()
        {
            if (ConnectedCell == null)
                return;
            ConnectedCell.ChangeColor(Color.Black);
            ConnectedCell.PrevConnectedCell = null;
            ConnectedCell = null;
            ChangeColorBackwards(Color.Black);
        }
        public void ChangeColor(Color color)
        {
            if (isCellDot)
            {
                if (PrevConnectedCell != null)
                    PrevConnectedCell.ChangeColorBackwards(Color);
            }
            else if (color != this.Color)
            {
                this.Color = color;
                if (ConnectedCell != null)
                    ConnectedCell.ChangeColor(color);
            }
        }
        public void ChangeColorBackwards(Color color)
        {
            if (isCellDot)
            {
                if (ConnectedCell != null)
                    ConnectedCell.ChangeColor(Color);
            }
            else if (color != this.Color)
            {
                this.Color = color;
                if (PrevConnectedCell != null)
                    PrevConnectedCell.ChangeColorBackwards(color);
            }
        }
        public bool HasEscape()
        {
            // Checks if it can go to any of its neighbors if it isnt connected
            if (isCellDot && isRecievingCellDot)
                return true;
            if (ConnectedCell != null)
                return true;
            foreach (Cell cell in Neighbors)
            {
                if (CanConnect(cell))
                    return true;
            }
            return false;
        }
        public bool HasIncome()
        {
            // Checks if it can go to any of its neighbors if it isnt connected
            if (isCellDot && !isRecievingCellDot)
                return true;
            if (PrevConnectedCell != null)
                return true;
            foreach (Cell cell in Neighbors)
            {
                if (cell.CanConnect(this))
                    return true;
            }
            return false;
        }
    }
    internal class Puzzle
    {
        // Representation of a Flow Free puzzle.
        // First each cell has a color, the two other cells it is connected to, the direction of the connection, and the possible directions it can go.
        // The puzzle is represented as a list of cells, and a list of colors.
        // The puzzle is solved when all cells are connected and there are no loops.
        public List<Cell> Cells { get; set; }
        public Size size { get; set; }
        private int c;
        private string name;
        public Puzzle(string name)
        {
            Cells = new();
            this.name = name;
            c = 0;
        }
        public bool Solve()
        {
            if (Cells.Count == 0)
                return false;

            // Solve the puzzle.
            Dictionary<Color, int> seenC = new();
            foreach (Cell cell in Cells)
            {
                if (cell.Color != Color.Black && !seenC.ContainsKey(cell.Color))
                {
                    seenC.Add(cell.Color, 1);
                    cell.isRecievingCellDot = false;
                }
                else if (seenC.ContainsKey(cell.Color))
                {
                    seenC[cell.Color]++;
                    cell.isRecievingCellDot = true;

                    if (seenC[cell.Color] > 2)
                        return false;
                }
            }

            bool res = recurse(0);
            // Console.WriteLine("Solving " + name + " was " + (res ? "success" : "fail"));
            return res;
        }

        public bool recurse(int target)
        {
            // SavePuzzle();

            // End condition
            if (target == Cells.Count)
            {
                foreach (Cell cell in Cells)
                {
                    if (!cell.IsValid())
                        return false;
                }
                return true;
            }

            Cell targetCell = Cells[target];

            if (targetCell.IsValid())
            {
                if (recurse(target + 1))
                    return true;
            }

            foreach (Cell n in targetCell.Neighbors)
            {
                if (TailBack(targetCell, n))
                    continue;
                if (targetCell.tryConnect(n))
                {
                    if (TailHasEscape(targetCell) && HeadHasEscape(n) && !Tail(targetCell, n))
                    {
                        if (recurse(target + 1))
                            return true;
                    }
                    targetCell.Disconnect();
                }
            }
            return false;
        }

        public static bool TailBack(Cell current, Cell next)
        {
            List<Cell> tail = new();
            tail.Add(current);
            while (tail.Last().PrevConnectedCell != null)
            {
                tail.Add(tail.Last().PrevConnectedCell);
            }
            foreach (Cell n2 in next.Neighbors)
            {
                if (n2 != current && tail.Contains(n2))
                {
                    return true;
                }
            }
            return false;
        }
        public static bool Tail(Cell current, Cell next)
        {
            List<Cell> tailFront = new();
            List<Cell> tailBack = new();
            tailFront.Add(next);
            while (tailFront.Last().ConnectedCell != null)
            {
                tailFront.Add(tailFront.Last().ConnectedCell);
            }
            tailBack.Add(current);
            while (tailBack.Last().PrevConnectedCell != null)
            {
                tailBack.Add(tailBack.Last().PrevConnectedCell);
            }
            foreach (Cell n1 in tailFront)
            {
                if (n1 == next)
                    continue;
                foreach (Cell n2 in tailBack)
                {
                    if (n1.Neighbors.Contains(n2))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool HeadHasEscape(Cell cell)
        {
            while (cell.ConnectedCell != null)
            {
                cell = cell.ConnectedCell;
            }
            return (cell.HasEscape());
        }
        public static bool TailHasEscape(Cell cell)
        {
            while (cell.PrevConnectedCell != null)
            {
                cell = cell.PrevConnectedCell;
            }
            return (cell.HasIncome());
        }
        public void SavePuzzle(Bitmap? underlay = null)
        {
            if (c > 500)
                return;
            c++;
            Bitmap bitmap = DrawPuzzle(underlay);
            // Save the image to a file.
            bitmap.Save(name + "/" + c + ".png");
            bitmap.Dispose();
        }

        public Bitmap DrawPuzzle(Bitmap? underlay = null)
        {
            Graphics g;
            Bitmap bitmap;

            if (underlay == null)
            {
                bitmap = new Bitmap(size.Width, size.Height);
                g = Graphics.FromImage(bitmap);
                g.Clear(Color.DarkGray);
            }
            else
            {
                bitmap = underlay;
                g = Graphics.FromImage(bitmap);
            }
            double scale = bitmap.Size.Width / 1600.0f;


            foreach (Cell cell in Cells)
            {
                // Draw the cell.
                int x = cell.Position.x;
                int y = cell.Position.y;

                // Draw the possible connection with neighbors.
                foreach (Cell neighbor in cell.Neighbors)
                {
                    int x1 = neighbor.Position.x;
                    int y1 = neighbor.Position.y;
                    g.DrawLine(new Pen(Color.Gray, (int)(5 * scale) + 1), x, y, x1, y1);
                }
            }
            foreach (Cell cell in Cells)
            {
                // Draw the cell.
                int x = cell.Position.x;
                int y = cell.Position.y;

                // Draw the connections.
                if (cell.ConnectedCell != null)
                {
                    int x1 = cell.ConnectedCell.Position.x;
                    int y1 = cell.ConnectedCell.Position.y;
                    g.DrawLine(new Pen(cell.Color, (int)(30 * scale) + 1), x, y, x1, y1);
                }
                // Draw a circle for the cell.
                DrawCircle(g, cell.Color, cell.isCellDot ? (int)(70 * scale + 1) : (int)(30 * scale + 1), cell.Position.x, cell.Position.y);
            }
            g.Dispose();
            return bitmap;
        }

        public static void DrawCircle(Graphics g, Color color, int size, int x, int y)
        {
            g.FillEllipse(new SolidBrush(color), x - size / 2, y - size / 2, size, size);
        }
    }
}