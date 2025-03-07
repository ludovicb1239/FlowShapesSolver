using System.Diagnostics;
using System.Numerics;
using System.Xml.Linq;

namespace FlowShapesSolver
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            RunTests();

            // Create a thread to run the puzzle solver
            Thread t = new(() => BackgroundWorkerSolver());
            t.Start();
        }
        private void RunTests()
        {
            Test("tests/1.png");
            Test("tests/2.jpg");
            Test("tests/4.jpg");
            Test("tests/7.jpg");
            Test("tests/8.jpg");
            Test("tests/9.jpg");
            Test("tests/11.jpg");
            Test("tests/original1.png");
            Test("tests/original3.png");
            Test("tests/original4.png");
            Test("tests/original5.png");
            Test("tests/original6.png");
            Test("tests/original8.png");

            // These tests are too slow to run on the CI
            Test("tests/original7.png");
            Test("tests/original2.png");
            Test("tests/3.jpg");
        }

        private void Test(string s)
        {
            string name = Path.GetFileNameWithoutExtension(s);
            Bitmap baseImage = Bitmap.FromFile(s) as Bitmap;
            if (baseImage == null)
            {
                Console.WriteLine("Failed to load image");
                return;
            }
            SolvePuzzle(name, baseImage);
            baseImage.Dispose();
        }
        private void SolvePuzzle(string name, Bitmap img)
        {
            Directory.CreateDirectory(name);
            Puzzle puzzle = new Puzzle(name);

            puzzle.Cells = CellDetector.DetectCells(img, name);
            puzzle.size = img.Size;
            puzzle.SavePuzzle();

            Stopwatch profiler = new();
            profiler.Start();

            bool res = puzzle.Solve();
            Console.WriteLine("Solving " + name + " was " + (res ? "success" : "fail"));

            profiler.Stop();
            double elapsedMicroseconds = (double)profiler.ElapsedTicks / Stopwatch.Frequency * 1_000_000;
            Console.WriteLine($"Solve Time: {elapsedMicroseconds:F3} µs");

            puzzle.SavePuzzle(img);
        }
        private void PlayPuzzle()
        {
            Vector2 topLeft = new(1286, 140);
            Bitmap s = ScreenReader.TakeScreenshotRegion(topLeft, new Vector2(1875, 922));
            Directory.CreateDirectory("Screen");
            s.Save("Screen/original.png");
            Puzzle puzzle = new Puzzle("Screen");
            puzzle.Cells = CellDetector.DetectCells(s, "Screen");
            puzzle.size = s.Size;
            puzzle.SavePuzzle();
            puzzle.Solve();
            puzzle.SavePuzzle(s);
            Player p = new();
            p.Play(puzzle, topLeft);
            s.Dispose();
        }

        private void BackgroundWorkerSolver()
        {
            while (true)
            {
                Thread.Sleep(100);

                Vector2 topLeft = new(1286, 140);
                Bitmap s = ScreenReader.TakeScreenshotRegion(topLeft, new Vector2(1875, 922));
                if (s == null)
                    continue;
                Puzzle puzzle = new Puzzle("");
                puzzle.Cells = CellDetector.DetectCells(s, "");
                puzzle.size = s.Size;
                bool res;
                try
                {
                    res = puzzle.Solve();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                if (res)
                {
                    // Run on main UI thread
                    Invoke(new Action(() =>
                    {
                        pictureBox1.Image = s.Clone() as Image;
                        pictureBox2.Image = puzzle.DrawPuzzle(s.Clone() as Bitmap);
                        pictureBox2.Visible = true;
                    }));
                }
                else
                {
                    // Run on main UI thread
                    Invoke(new Action(() =>
                    {
                        pictureBox1.Image = s.Clone() as Image;
                        pictureBox2.Visible = false;
                    }));
                }
                s.Dispose();
            }
        }

        private void SolveButton_Click(object sender, EventArgs e)
        {
            PlayPuzzle();
        }
    }
}
