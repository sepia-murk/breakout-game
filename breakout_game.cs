using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;

namespace Breakout
{
    public class Breakout : Form
    {
        /*
        |--------------------------------------------------------------------------
        | ANCHOR Declarations
        |--------------------------------------------------------------------------
        */
        private Label instructions = new Label();
        private Label score = new Label();
        private Label numberOfLives = new Label();
        private Label timeCounter = new Label();
        private Label brickCounter = new Label();
        private Label youWon = new Label();
        private Label youLost = new Label();
        private Label finalScore = new Label();
        private Label displayLeaderboard = new Label();
        private Label submitName = new Label();
        private Label author = new Label();
        private Button greeting = new Button();
        private Button quitGame = new Button();
        private Button playGame = new Button();
        private PictureBox gameLogo = new PictureBox();
        private Leaderboard leaderboard = new Leaderboard();
        private List<Brick> gameBricks = new List<Brick>();
        private List<Color> gameColors = new List<Color> { Color.Red, Color.Orange, Color.Green, Color.Yellow };
        private int[] gameColorsValues = new int[]{ 7, 5, 3, 1 };
        public int Score = 0;
        public int NumberOfLives = 5;
        public string PlayerName;
        private bool hasWon;
        private Paddle paddle;
        private Ball ball;
        private KeyboardManager keyboardManager;
        public BoxRestrictions restrictions;
        private Stopwatch stopwatch;
        private TextBox nameInput;
        private Timer timer;
        /*
        |--------------------------------------------------------------------------
        | ANCHOR Declarations - Constants
        |--------------------------------------------------------------------------
        */
        public int NumberOfBricksInRow = 16;
        public int NumberOfRowsPerColor = 2;
        public int StripSize = 50;
        private int tickPeriod = 16;
        private Font font = new Font("Monospace", 20, FontStyle.Bold);
        private Color objectForeColor = Color.White;
        private Color objectBackColor = Color.Red;
        private Color backgroundColor = Color.Black;

        /*
        |--------------------------------------------------------------------------
        | ANCHOR Utils
        |--------------------------------------------------------------------------
        */
        private void ClearWindow()
        {
            this.Controls.Clear();
        }

        private void Quit(object sender, EventArgs e)
        {
            this.Close();
        }
        /*
        |--------------------------------------------------------------------------
        | ANCHOR Custom classes
        |--------------------------------------------------------------------------
        */
        public class KeyboardManager
        {
            private TextBox textBox;
            private bool leftKey;
            private bool rightKey;
            public bool LeftKey
            {
                get
                {
                    return this.leftKey;
                }
            }
            public bool RightKey
            {
                get
                {
                    return this.rightKey;
                }
            }
            public void Focus()
            {
                this.textBox.Focus();
            }
            private void OnKeyChange(Keys keyCode, bool delta)
            {
                switch (keyCode)
                {
                    case Keys.Left:
                        this.leftKey = delta;
                        break;

                    case Keys.Right:
                        this.rightKey = delta;
                        break;
                }
            }
            private void OnKeyDown(object sender, KeyEventArgs e)
            {
                this.OnKeyChange(e.KeyCode, true);
            }

            private void OnKeyUp(object sender, KeyEventArgs e)
            {
                this.OnKeyChange(e.KeyCode, false);
            }

            public KeyboardManager(Breakout breakout)
            {
                this.textBox = new TextBox();
                this.textBox.Location = new Point(99999, 99999);

                this.textBox.Parent = breakout;

                this.textBox.KeyDown += this.OnKeyDown;
                this.textBox.KeyUp += this.OnKeyUp;
            }
        }
        
        public class BoxRestrictions
        {
            public double Left;
            public double Top;
            public double Width;
            public double Height;
            public enum BoxParts
            {
                Upper, Lower, Left, Right
            };

            public enum CollisionLocation
            {
                Top, Right, Bottom, Left, NoCollision
            };

            public double Right
            {
                get
                {
                    return this.Left + this.Width;
                }
            }

            public double Bottom
            {
                get
                {
                    return this.Top + this.Height;
                }
            }

            public double XCenter
            {
                get
                {
                    return (this.Left + (this.Width / 2.0));
                }
            }

            public double YCenter
            {
                get
                {
                    return (this.Top + (this.Height / 2.0));
                }
            }

            public double RectangleArea
            {
                get
                {
                    return (this.Width * this.Height);
                }
            }

            public BoxRestrictions(double left, double top, double width, double height)
            {
                this.Left = left;
                this.Top = top;
                this.Width = width;
                this.Height = height;
            }

            private static BoxRestrictions Intersection(BoxRestrictions box1, BoxRestrictions box2)
            {

                if (!HasCollided(box1, box2))
                {
                    return null;
                }

                // Collisions
                double[] xCoordinates = new double[] { box1.Left, box1.Right, box2.Left, box2.Right };
                double[] yCoordinates = new double[] { box1.Top, box1.Bottom, box2.Top, box2.Bottom };

                Array.Sort(xCoordinates);
                Array.Sort(yCoordinates);

                return new BoxRestrictions(xCoordinates[1], yCoordinates[1], xCoordinates[2] - xCoordinates[1], yCoordinates[2] - yCoordinates[1]);
            }
            private BoxRestrictions BoxBoundaries(BoxParts part)
            {
                switch (part)
                {
                    case BoxParts.Upper: return new BoxRestrictions(this.Left, this.Top, this.Width, this.Height / 2.0);
                    case BoxParts.Lower: return new BoxRestrictions(this.Left, this.YCenter, this.Width, this.Height / 2.0);
                    case BoxParts.Left: return new BoxRestrictions(this.Left, this.Top, this.Width / 2.0, this.Height);
                    case BoxParts.Right: return new BoxRestrictions(this.XCenter, this.Top, this.Width / 2.0, this.Height);
                    default: return null;
                }
            }

            public override string ToString()
            {
                return string.Join(" ", this.Left, this.Top, this.Width, this.Height);
            }

            public static bool HasCollided(BoxRestrictions box1, BoxRestrictions box2)
            {
                // No collisions
                if (box1.Left < box2.Left)
                {
                    if (box1.Right <= box2.Left)
                    {
                        return false;
                    }
                }
                else
                {
                    if (box2.Right <= box1.Left)
                    {
                        return false;
                    }
                }

                if (box1.Top < box2.Top)
                {
                    if (box1.Bottom <= box2.Top)
                    {
                        return false;
                    }
                }
                else
                {
                    if (box2.Bottom <= box1.Top)
                    {
                        return false;
                    }
                }

                // Some collision
                return true;
            }
            public static double IntersectionAreaPercentage(BoxRestrictions primary, BoxRestrictions secondary)
            {

                BoxRestrictions intersection = Intersection(primary, secondary);

                if (intersection != null)
                {
                    return intersection.RectangleArea / primary.RectangleArea;
                }
                return 0.0;
            }

            public static CollisionLocation GetCollisionLocation(BoxRestrictions primary, BoxRestrictions secondary)
            {
                if (!HasCollided(primary, secondary))
                {
                    return CollisionLocation.NoCollision;
                }

                double intersectionRightPrimary = IntersectionAreaPercentage(primary.BoxBoundaries(BoxParts.Right), secondary);
                double intersectionLeftPrimary = IntersectionAreaPercentage(primary.BoxBoundaries(BoxParts.Left), secondary);
                double intersectionTopPrimary = IntersectionAreaPercentage(primary.BoxBoundaries(BoxParts.Upper), secondary);
                double intersectionBottomPrimary = IntersectionAreaPercentage(primary.BoxBoundaries(BoxParts.Lower), secondary);

                double intersectionRightSecondary = IntersectionAreaPercentage(secondary.BoxBoundaries(BoxParts.Right), primary);
                double intersectionLeftSecondary = IntersectionAreaPercentage(secondary.BoxBoundaries(BoxParts.Left), primary);
                double intersectionTopSecondary = IntersectionAreaPercentage(secondary.BoxBoundaries(BoxParts.Upper), primary);
                double intersectionBottomSecondary = IntersectionAreaPercentage(secondary.BoxBoundaries(BoxParts.Lower), primary);

                double intersectionRight = intersectionRightPrimary + intersectionLeftSecondary;
                double intersectionLeft = intersectionLeftPrimary + intersectionRightSecondary;
                double intersectionTop = intersectionTopPrimary + intersectionBottomSecondary;
                double intersectionBottom = intersectionBottomPrimary + intersectionTopSecondary;

                if (intersectionRight >= intersectionLeft && intersectionRight >= intersectionTop && intersectionRight >= intersectionBottom)
                {
                    return CollisionLocation.Right;
                }

                if (intersectionLeft >= intersectionRight && intersectionLeft >= intersectionTop && intersectionLeft >= intersectionBottom)
                {
                    return CollisionLocation.Left;
                }

                if (intersectionTop >= intersectionRight && intersectionTop >= intersectionLeft && intersectionTop >= intersectionBottom)
                {
                    return CollisionLocation.Top;
                }

                return CollisionLocation.Bottom;
            }

            public static bool SideWallsCollisions(BoxRestrictions mainWindow, BoxRestrictions ball)
            {
                return (ball.Left <= 0) || ((ball.Left + ball.Width) >= mainWindow.Width);
            }

            public static bool TopWallCollisions(BoxRestrictions mainWindow, BoxRestrictions ball, int StripSize)
            {
                return (mainWindow.Top + StripSize >= ball.Top);
            }

            public static bool BottomWallCollisions(BoxRestrictions mainWindow, BoxRestrictions ball, int StripSize, int paddleWidth)
            {
                return ((ball.Top + ball.Height) >= (mainWindow.Height - StripSize + paddleWidth));
            }
        }

        public class XY
        {
            public double X;
            public double Y;
            public XY(double x, double y)
            {
                this.X = x;
                this.Y = y;
            }

            public void RotateHorizontally()
            {
                //paddle and top wall
                this.Y *= -1.0;
            }

            public void RotateVertically()
            {
                this.X *= -1.0;
            }
        }
        
        public class LeaderboardEntry
        {
            private string playerName;
            private int playerScore;
            public LeaderboardEntry(string name, int highscore)
            {
                this.playerName = name;
                this.playerScore = highscore;
            }

            public override string ToString()
            {
                return string.Format("{0}     {1}", this.playerName, this.playerScore);
            }

            public string FileEntry
            {
                get
                {
                    return string.Format("{0},{1}", this.playerName, this.playerScore);
                }
            }

            public string PlayerName
            {
                get
                {
                    return this.playerName;
                }
            }
            public int PlayerScore
            {
                get
                {
                    return this.playerScore;
                }            
            }
        }
        
        public class Leaderboard
        {
            private int leaderboardEntries = 5;
            private LeaderboardEntry emptyEntry = new LeaderboardEntry("---", 0);
            private LeaderboardEntry[] leaderboard;
            public Leaderboard()
            {
                this.leaderboard = new LeaderboardEntry[this.leaderboardEntries + 1];
                try
                {
                    using (StreamReader file = new StreamReader("leaderboard.txt"))
                    {
                        int i = 0;
                        while (!file.EndOfStream)
                        {
                            string[] line = file.ReadLine().Split(',');
                            this.leaderboard[i++] = new LeaderboardEntry(line[0], Int32.Parse(line[1]));
                        }
                        if(i < this.leaderboardEntries)
                        {
                            this.TopOffLeaderboard(i);
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    this.TopOffLeaderboard(0);
                }
            }

            private void TopOffLeaderboard(int startAt)
            {
                for (int i = startAt; i < this.leaderboardEntries; i++)
                {
                    this.leaderboard[i] = this.emptyEntry;
                }
            }

            private void SaveLeaderboardToFile()
            {
                try
                {
                    using (StreamWriter file = new StreamWriter("leaderboard.txt"))
                    {
                        for (int i = 0; i < this.leaderboardEntries; i++)
                        {
                            file.WriteLine(this.leaderboard[i].FileEntry);
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    var fs = new FileStream("leaderboard.txt", FileMode.CreateNew);
                    this.SaveLeaderboardToFile();
                }
            }

            private void BubbleEntries()
            {
                int compareTo = this.leaderboardEntries;
                while((compareTo > 0) && (this.leaderboard[compareTo].PlayerScore > this.leaderboard[compareTo-1].PlayerScore))
                {
                    var temp = this.leaderboard[compareTo];
                    this.leaderboard[compareTo] = this.leaderboard[compareTo - 1];
                    this.leaderboard[compareTo - 1] = temp;
                    compareTo--;
                }
            }

            public void MaybeUpdateLeaderboard(string playerName, int playerScore)
            {
                this.leaderboard[this.leaderboard.Length - 1] = new LeaderboardEntry(playerName, playerScore);
                this.BubbleEntries();
                this.SaveLeaderboardToFile();
            }

            public string LeaderboardContents()
            {
                string res = "";
                this.BubbleEntries();
                for (int i = 0; i < this.leaderboardEntries; i++)
                {
                    res += string.Format("{0}.{1}   {2}\n", i + 1, this.leaderboard[i].PlayerName, this.leaderboard[i].PlayerScore);
                }
                return res;
            }
        }
        
        public class Brick
        {
            private PictureBox brick;
            private Breakout breakout;
            public BoxRestrictions restrictions;
            private int colorValue;
            public int ColorValue
            {
                get 
                {
                    return this.colorValue;
                }
            }

            public void Dispose()
            {
                this.brick.Dispose();
            }

            public Brick(Breakout breakout, int x, int y, int brickWidth, int brickHeight, Color brickColor, int colorValue)
            {
                this.breakout = breakout;
                this.brick = new PictureBox();

                this.brick.Parent = this.breakout;
                this.brick.Location = new Point(x, y);
                this.brick.Height = brickHeight;
                this.brick.Width = brickWidth;
                this.brick.BackColor = brickColor;
                this.colorValue = colorValue;
                this.restrictions = new BoxRestrictions(this.brick.Left, this.brick.Top, this.brick.Width, this.brick.Height);
            }
        }

        public class Paddle
        {
            private PictureBox paddle;
            private Breakout breakout;
            private Color paddleColor = Color.White;
            public BoxRestrictions restrictions;
            private int oneMove = 10;

            public Paddle(Breakout breakout, int x, int y, int paddleWidth, int paddleHeight)
            {
                this.breakout = breakout;
                this.paddle = new PictureBox();
                this.paddle.Parent = this.breakout;
                this.paddle.Location = new Point(x, y);
                this.paddle.Height = paddleHeight;
                this.paddle.Width = paddleWidth;
                this.paddle.BackColor = this.paddleColor;
                this.restrictions = new BoxRestrictions(this.paddle.Left, this.paddle.Top, this.paddle.Width, this.paddle.Height);
            }

            public void MoveLeft()
            {
                this.restrictions.Left = Math.Max(0,this.paddle.Left-this.oneMove);
                this.paddle.Left = (int)Math.Round(this.restrictions.Left);
            }

            public void MoveRight()
            {
                this.restrictions.Left = Math.Min(this.breakout.ClientRectangle.Width-this.restrictions.Width, this.paddle.Left + this.oneMove);
                this.paddle.Left = (int)Math.Round(this.restrictions.Left);

            }
        }
        
        public class Ball
        {
            private PictureBox ball;
            private Breakout breakout;
            private Bitmap ballImage;
            private XY velocity;
            public BoxRestrictions restrictions;

            public Ball(Breakout breakout, int x, int y, int ballWidth, int ballHeight)
            {
                this.breakout = breakout;
                this.ball = new PictureBox();
                this.ball.Parent = this.breakout;
                this.ball.Left = x;
                this.ball.Top = y;
                this.ball.Height = ballHeight;
                this.ball.Width = ballWidth;
                this.ballImage = new Bitmap(Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"blue.png")), this.ball.Height, this.ball.Width);
                this.ball.Image = this.ballImage;
                this.velocity = new XY(3.0, -4.0);
                this.restrictions = new BoxRestrictions(this.ball.Left, this.ball.Top, this.ball.Width, this.ball.Height);
            }

            public void Dispose()
            {
                this.ball.Dispose();
            }

            public XY Velocity
            {
                get
                {
                    return velocity;
                }
            }

            public bool HasHitPaddle(Paddle paddle)
            {
                if (BoxRestrictions.HasCollided(paddle.restrictions, this.restrictions))
                {
                    double xDeviation = (this.restrictions.XCenter - paddle.restrictions.XCenter) / ((paddle.restrictions.Width+this.restrictions.Width) / 2.0);

                    this.velocity.Y *= -1.0;
                    this.velocity.X += xDeviation * 0.70 * (Math.Abs(this.velocity.X)+(9/(3+Math.Abs(this.velocity.X))));
                    this.velocity.X = Math.Max(-15, Math.Min(15, this.velocity.X));

                    this.BallMovement();

                    return true;
                }
                return false;
            }

            public bool HasHitSideWalls()
            {
                if (BoxRestrictions.SideWallsCollisions(this.breakout.restrictions, this.restrictions))
                {
                    this.velocity.RotateVertically();
                    this.BallMovement();
                    return true;
                }
                return false;
            }

            public bool HasHitTopWall()
            {
                if (BoxRestrictions.TopWallCollisions(this.breakout.restrictions, this.restrictions, this.breakout.StripSize))
                {
                    this.velocity.RotateHorizontally();
                    this.BallMovement();
                    return true;
                }
                return false;
            }

            public bool HasHitBottomWall(Paddle paddle)
            {
                if (BoxRestrictions.BottomWallCollisions(this.breakout.restrictions, this.restrictions, this.breakout.StripSize, (int)paddle.restrictions.Height))
                {
                    if(this.breakout.NumberOfLives > 1)
                    {
                        this.breakout.NumberOfLives -= 1;
                        this.ball.Dispose();
                        this.breakout.CreateBall();
                        return false;
                    }
                    else
                    {
                        this.Velocity.X = 0.0;
                        this.Velocity.Y = 0.0;
                        this.breakout.GameOver();
                        return true;
                    }
                }
                return false;
            }
        
        public Brick HasHitBricks(List<Brick> gameBricks)
            {
                foreach (Brick brick in gameBricks)
                {
                    
                    switch(BoxRestrictions.GetCollisionLocation(brick.restrictions, this.restrictions))
                    {
                        case BoxRestrictions.CollisionLocation.Right:
                        case BoxRestrictions.CollisionLocation.Left:
                            this.velocity.RotateVertically();
                            this.BallMovement();
                            return brick;
                        case BoxRestrictions.CollisionLocation.Top:
                        case BoxRestrictions.CollisionLocation.Bottom:
                            this.velocity.RotateHorizontally();
                            this.BallMovement();
                            return brick;
                    }
                }
                return null;
            }

            public void BallMovement()
            {
                this.restrictions.Left += this.velocity.X;
                this.restrictions.Top += this.velocity.Y;
                this.ball.Left = (int)Math.Round(this.restrictions.Left);
                this.ball.Top = (int)Math.Round(this.restrictions.Top);
            }
        }
        /*
        |--------------------------------------------------------------------------
        | ANCHOR Create labels
        |--------------------------------------------------------------------------
        */
        private void InstructionsLabel()
        {
            this.instructions.Location = new Point(200, 250);
            this.instructions.MinimumSize = new Size(730, 50);
            this.instructions.Font = this.font;
            this.instructions.TextAlign = ContentAlignment.TopCenter;
            this.instructions.Text = "Use left and right arrow to move the paddle.";
            this.instructions.ForeColor = this.objectForeColor;
            this.Controls.Add(this.instructions);
        }
        
        private void ScoreLabel()
        {
            this.score.Location = new Point(5, 5);
            this.score.Font = this.font;
            this.score.AutoSize = true;
            this.score.Text = string.Format("Score: {0}", this.Score);
            this.score.ForeColor = this.objectForeColor;
            this.Controls.Add(this.score);
        }
        
        private void NumberOfLivesLabel()
        {
            this.numberOfLives.Location = new Point(490, 5);
            this.numberOfLives.Font = this.font;
            this.numberOfLives.AutoSize = true;
            this.numberOfLives.Text = string.Format("Lives {0}", this.NumberOfLives);
            this.numberOfLives.ForeColor = this.objectForeColor;
            this.Controls.Add(this.numberOfLives);
        }
        
        private void TimeCounterLabel()
        {
            this.timeCounter.Location = new Point(925, 5);
            this.timeCounter.Font = this.font;
            this.timeCounter.AutoSize = true;
            TimeSpan elapsed = this.stopwatch.Elapsed;
            this.timeCounter.Text = String.Format("Time {0:00}:{1:00}", elapsed.Minutes, elapsed.Seconds);
            this.timeCounter.ForeColor = this.objectForeColor;
            this.Controls.Add(this.timeCounter);
        }
        
        private void BrickCounterLabel()
        {
            this.brickCounter.Location = new Point(925, 765);
            this.brickCounter.Font = this.font;
            this.brickCounter.AutoSize = true;
            this.brickCounter.Text = String.Format("Bricks {0}", this.gameBricks.Count);
            this.brickCounter.ForeColor = this.objectForeColor;
            this.Controls.Add(this.brickCounter);
        }
        
        private void YouWonLabel()
        {
            this.youWon.Location = new Point(300, 100);
            this.youWon.Font = this.font;
            this.youWon.AutoSize = true;
            this.youWon.Text = String.Format("Congratulations, {0}, you won!", this.PlayerName);
            this.youWon.ForeColor = this.objectForeColor;
            this.Controls.Add(this.youWon);
        }

        private void YouLostLabel()
        {
            this.youLost.Location = new Point(300, 100);
            this.youLost.Font = this.font;
            this.youLost.AutoSize = true;
            this.youLost.Text = String.Format("I'm sorry, {0}, you lost!", this.PlayerName);
            this.youLost.ForeColor = this.objectForeColor;
            this.Controls.Add(this.youLost);
        }

        private void DisplayScore()
        {
            this.finalScore.Location = new Point(440, 150);
            this.finalScore.Font = this.font;
            this.finalScore.AutoSize = true;
            this.finalScore.Text = String.Format("Your score is {0}", this.Score);
            this.finalScore.ForeColor = this.objectForeColor;
            this.Controls.Add(this.finalScore);
            this.leaderboard.MaybeUpdateLeaderboard(this.PlayerName, this.Score);
        }
        
        private void LeaderboardLabel()
        {
            this.displayLeaderboard.Text = this.leaderboard.LeaderboardContents();
            this.displayLeaderboard.Location = new Point(440, 250);
            this.displayLeaderboard.AutoSize = true;
            this.displayLeaderboard.Font = this.font;
            this.displayLeaderboard.ForeColor = this.objectForeColor;
            this.Controls.Add(this.displayLeaderboard);
        }

        private void SubmitInformationLabel()
        {
            this.submitName.Text = "Press enter to log your name";
            this.submitName.Width = 450;
            this.submitName.Height = 50;
            this.submitName.Left = (this.ClientRectangle.Width - this.submitName.Width) / 2;
            this.submitName.Top = 350;
            this.submitName.Font = this.font;
            this.submitName.ForeColor = this.objectForeColor;
            this.Controls.Add(this.submitName);
        }
        private void AuthorLabel()
        {
            this.author.Text = "Miriam Supeková";
            this.author.Location = new Point(450, 750);
            this.author.AutoSize = true;
            this.author.Font = this.font;
            this.author.ForeColor = this.objectForeColor;
            this.Controls.Add(this.author);
        }
        /*
        |--------------------------------------------------------------------------
        | ANCHOR Create PictureBox
        |--------------------------------------------------------------------------
        */
        private void GameLogo()
        {
            this.gameLogo.Location = new Point(450,250);
            this.gameLogo.Width = 250;
            this.gameLogo.Height = 250;
            this.gameLogo.Image = new Bitmap(Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "atari_logo.jpg")), this.gameLogo.Width, this.gameLogo.Height);
            this.Controls.Add(this.gameLogo);
        }

        /*
        |--------------------------------------------------------------------------
        | ANCHOR Create TextBox
        |--------------------------------------------------------------------------
        */
        private void OnKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13 && this.nameInput.Text != "")
            {
                // Prevents multiple invocations of the game
                this.nameInput.Dispose();

                this.BeforeGame(sender, e);
            }
        }

        private void NameInputBox()
        {
            this.nameInput = new TextBox();
            this.nameInput.Location = new Point(480, 400);
            this.Controls.Add(this.nameInput);
            this.nameInput.Focus();
            this.nameInput.KeyPress += this.OnKeyPress;
        }
        /*
        |--------------------------------------------------------------------------
        | ANCHOR Create buttons
        |--------------------------------------------------------------------------
        */
        private void GreetingButton()
        {
            this.greeting.Text = "Begin the game!";
            this.greeting.Location = new Point(450, 510);
            this.greeting.AutoSize = true;
            this.greeting.Font = this.font;
            this.greeting.ForeColor = this.objectForeColor;
            this.greeting.BackColor = this.objectBackColor;
            this.greeting.Click += this.LogIn;
            this.Controls.Add(this.greeting);
        }

       private void QuitGameButton()
        {
            this.quitGame.Text = "Quit the game";
            this.quitGame.Location = new Point(440, 500);
            this.quitGame.AutoSize = true;
            this.quitGame.Font = this.font;
            this.quitGame.ForeColor = this.objectForeColor;
            this.quitGame.BackColor = this.objectBackColor;
            this.quitGame.Click += this.Quit;
            this.Controls.Add(this.quitGame);
        }

        private void PlayGameButton()
        {
            this.playGame.Text = "Play game";
            this.playGame.Location = new Point(500, 300);
            this.playGame.AutoSize = true;
            this.playGame.Font = this.font;
            this.playGame.ForeColor = this.objectForeColor;
            this.playGame.BackColor = this.objectBackColor;
            this.Controls.Add(this.playGame);
            this.playGame.Click += this.PlayGame;
        }

        /*
        |--------------------------------------------------------------------------
        | ANCHOR Windows        
        |--------------------------------------------------------------------------
        */
        private void LogIn(object sender, EventArgs e)
        {
            this.ClearWindow();

            this.SubmitInformationLabel();
            this.NameInputBox();
        }
       
        private void BeforeGame(object sender, EventArgs e)
        {
            this.PlayerName = this.nameInput.Text;

            this.ClearWindow();
           
            this.InstructionsLabel();
            this.PlayGameButton();
        }

        private void WinningGame()
        {
            this.YouWonLabel();
            this.DisplayScore();
            this.LeaderboardLabel();
        }
        
        private void LosingGame()
        {
            this.YouLostLabel();
        }
        /*
        |--------------------------------------------------------------------------
        | ANCHOR Create bricks
        |--------------------------------------------------------------------------
        */
        private void CreateBricks()
        {
            int xCoordinate = (ClientRectangle.Width/NumberOfBricksInRow);
            int yCoordinate = ((ClientRectangle.Height/2)/(this.gameColors.Count*this.NumberOfRowsPerColor));

            Color brickColor = this.backgroundColor;

            for (int i = 0; i < this.gameColors.Count; i++)
            {
                for (int j = 0; j < this.NumberOfRowsPerColor; j++)
                {
                    for (int k = 0; k < this.NumberOfBricksInRow; k++)
                    {
                        int x = k * xCoordinate;
                        int y = ((i * NumberOfRowsPerColor + j) * yCoordinate)+40;
                        Brick brick = new Brick(this, x,y,xCoordinate-2, yCoordinate-2, gameColors[i], this.gameColorsValues[i]);
                        this.gameBricks.Add(brick);
                    }
                }
            }
        }

        /*
        |--------------------------------------------------------------------------
        | ANCHOR Create Paddle
        |--------------------------------------------------------------------------
        */
        private void CreatePaddle()
        {
            this.paddle = new Paddle(this, 450, 750, 200, 10);
        }
        /*
        |--------------------------------------------------------------------------
        | ANCHOR Create Ball
        |--------------------------------------------------------------------------
        */
        private void CreateBall()
        {
            this.ball = new Ball(this, 550, 700, 25, 25);
        }

        /*
        |--------------------------------------------------------------------------
        | ANCHOR GAME
        |--------------------------------------------------------------------------
        */
        private bool HasWon()
        {
            return (this.gameBricks.Count == 0);
        }

        private void PlayGame(object sender, EventArgs e)
        {
            this.ClearWindow();

            this.CreateBricks();
            this.CreatePaddle();
            this.CreateBall();

            this.keyboardManager = new KeyboardManager(this);
            this.keyboardManager.Focus();

            this.restrictions = new BoxRestrictions(0, 0, this.ClientRectangle.Width, this.ClientRectangle.Height);
            
            this.timer = new Timer();
            this.timer.Interval = this.tickPeriod;
            this.timer.Tick += this.Tick;
            this.timer.Enabled = true;

            this.stopwatch = new Stopwatch();
            this.stopwatch.Start();
        }

        public void GameOver()
        {
            this.timer.Enabled = false;
            this.stopwatch.Stop();
            this.ClearWindow();
            this.QuitGameButton();
            this.AuthorLabel();

            if (this.hasWon)
            {
                this.WinningGame();
            }
            else
            {
                this.LosingGame();
            }
        }
        /*
        |--------------------------------------------------------------------------
        | ANCHOR Main methods
        |--------------------------------------------------------------------------
        */
        private void Tick(object sender, EventArgs e)
        {
            if (!this.HasWon())
            {
                this.ScoreLabel();
                this.NumberOfLivesLabel();
                this.TimeCounterLabel();
                this.BrickCounterLabel();
                
                if (this.keyboardManager.LeftKey)
                    this.paddle.MoveLeft();

                if (this.keyboardManager.RightKey)
                    this.paddle.MoveRight();

                this.ball.BallMovement();

                Brick brickToDestroy = this.ball.HasHitBricks(this.gameBricks);

                if(brickToDestroy != null)
                {
                    this.Score += brickToDestroy.ColorValue;
                    this.gameBricks.Remove(brickToDestroy);
                    brickToDestroy.Dispose();
                }

                this.ball.HasHitPaddle(this.paddle);
                this.ball.HasHitTopWall();
                this.ball.HasHitSideWalls();
                this.ball.HasHitBottomWall(this.paddle);
            }
            else
            {
                this.hasWon = true;
                this.GameOver();
            }
        }

        public Breakout()
        {
            ClientSize = new Size(1090, 800);
            this.BackColor = this.backgroundColor;
            this.Text = "Breakout";
            this.GreetingButton();
            this.GameLogo();
        }

        [STAThread]
        public static void Main()
        {
            Application.Run(new Breakout());
        }
    }
}
