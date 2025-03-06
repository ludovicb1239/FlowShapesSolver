using System.Numerics;
using WindowsInput; //InputSimulator

namespace FlowShapesSolver
{
    internal class Player
    {
        InputSimulator sim;
        public Player()
        {
            sim = new();
        }
        public void Play(Puzzle puzzle, Vector2 topLeft)
        {
            int delayms = 100;
            foreach (Cell cell in puzzle.Cells)
            {
                if (cell.isCellDot && !cell.isRecievingCellDot)
                {
                    MoveCursor((int)(cell.Position.x + topLeft.X), (int)(cell.Position.y + topLeft.Y));
                    ClickDown();
                    Cell? n = cell;
                    while (true)
                    {
                        n = n.ConnectedCell;
                        if (n == null)
                            break;
                        Thread.Sleep(delayms);
                        MoveCursor((int)(n.Position.x + topLeft.X), (int)(n.Position.y + topLeft.Y));
                    }
                    ClickUp();
                }
            }
        }
        public static double MapToAbsolute(double value, double screenSize)
        {
            return value * 65535.0 / screenSize;
        }
        public void MoveCursor(int x, int y)
        {
            // Screen resolution
            double screenWidth = 1920;
            double screenHeight = 1080;

            // Convert to absolute mouse units
            double absoluteX = MapToAbsolute(x, screenWidth);
            double absoluteY = MapToAbsolute(y, screenHeight);

            // Move the mouse to the specified coordinates
            sim.Mouse.MoveMouseTo(absoluteX, absoluteY);
        }
        private void ClickDown()
        {
            sim.Mouse.LeftButtonDown();
        }
        private void ClickUp()
        {
            sim.Mouse.LeftButtonUp();
        }
    }
}
