using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Media;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace Cats
{
    public partial class Main : Form
    {
        //Original song: Cats (Sped Up) by The living Tombstone
        private static SoundPlayer player = new SoundPlayer(new WebClient().OpenRead("https://gato.ovh/CDN/CatsSpeedUp.wav"));

        private static Random random = new Random();

        private static Screen currentScreen = Screen.PrimaryScreen;

        private static int catSize = 0;

        public static bool RandomBool() => random.Next() >= int.MaxValue / 2;

        private static int CustomNext(int minMax, int minMin, int maxMin, int maxMax) => RandomBool() ? random.Next(minMin, minMax) : random.Next(maxMin, maxMax);

        public static bool boxCollisions = true;

        private static Stopwatch sw = new Stopwatch();

        private static int fps = 40;
        private static int mspf = 1000 / fps;

        public Main()
        {
            player.Load();

            InitializeComponent();

            this.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            this.TopMost = true;
            this.ShowInTaskbar = false;

            this.TransparencyKey = this.BackColor = Color.FromArgb(255, 0, 0, 0);

            currentScreen = Screen.FromHandle(this.Handle);

            catSize = (currentScreen.Bounds.Width / 100) * 7;

            Init(round_1);

            Timer movement = new Timer();
            movement.Interval = mspf / 2;
            movement.Start();
            movement.Tick += TickMovement;

            Timer mooore = new Timer();
            mooore.Interval = (15 * 1000) + 500;

            int roundNum = 2;

            mooore.Tick += (object sender, EventArgs e) =>
            {
                switch (roundNum)
                {
                    case 2:
                        Init(round_2);
                        break;

                    case 3:
                        Init(round_3);

                        break;
                }

                roundNum++;

                mooore.Interval = (30 * 1000) + 730;
            };

            mooore.Start();

            Task.Factory.StartNew(async () =>
            {
                player.PlaySync();

                boxCollisions = false;

                await Task.Delay(5000);

                Environment.Exit(0);
            });
        }

        private static Image[] round_1 = { (RandomBool() ? Properties.Resources.cat_1 : Properties.Resources.cat_2), Properties.Resources.cat_3, Properties.Resources.cat_4 };

        private static Image[] round_2 = { (RandomBool() ? Properties.Resources.cat_5 : Properties.Resources.cat_6), Properties.Resources.cat_7, Properties.Resources.cat_8, Properties.Resources.cat_9 };

        private static Image[] round_3 = { Properties.Resources.cat_10, Properties.Resources.cat_11, Properties.Resources.cat_12, Properties.Resources.cat_13, Properties.Resources.cat_14 };

        private void Init(Image[] images)
        {
            foreach (var cat in images)
            {
                PictureBox pictureBox = new PictureBox();
                pictureBox.Image = cat;

                pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox.Width = catSize;
                pictureBox.Height = catSize;

                pictureBox.Location = new Point(random.Next(currentScreen.Bounds.Width), random.Next(currentScreen.Bounds.Height));

                this.Controls.Add(pictureBox);
            }
        }

        private Dictionary<int, Point> velocities = new Dictionary<int, Point>();

        private const int Gravity = 4;
        private const int TerminalVelocity = 80;

        private const int minVelocity = 4;
        private const int maxVelocity = 15;

        private async void TickMovement(object sender, EventArgs e)
        {
            await Task.Delay(Math.Max(0, mspf - (int)sw.ElapsedMilliseconds));

            sw.Restart();

            foreach (Control control in this.Controls)
            {
                if (control is PictureBox pictureBox)
                {
                    int picId = pictureBox.GetHashCode();

                    if (!velocities.ContainsKey(picId)) velocities[picId] = new Point(CustomNext(-minVelocity, -maxVelocity, minVelocity, maxVelocity), CustomNext(-minVelocity, -maxVelocity, minVelocity, maxVelocity));

                    //Console.WriteLine(picId + ": " + velocities[picId]);

                    pictureBox.Left += velocities[picId].X;
                    pictureBox.Top += velocities[picId].Y;

                    if (boxCollisions)
                    {
                        if (pictureBox.Left < 0)
                        {
                            velocities[picId] = new Point(Math.Abs(velocities[picId].X), velocities[picId].Y);

                            pictureBox.Left = 0;
                        }
                        else if (pictureBox.Top < 0)
                        {
                            velocities[picId] = new Point(velocities[picId].X, Math.Abs(velocities[picId].Y));

                            pictureBox.Top = 0;
                        }
                        else if (pictureBox.Right > this.ClientSize.Width)
                        {
                            velocities[picId] = new Point(-Math.Abs(velocities[picId].X), velocities[picId].Y);

                            pictureBox.Left = this.ClientSize.Width - pictureBox.Width;
                        }
                        else if (pictureBox.Bottom > this.ClientSize.Height)
                        {
                            velocities[picId] = new Point(velocities[picId].X, -Math.Abs(velocities[picId].Y));

                            pictureBox.Top = this.ClientSize.Height - pictureBox.Height;
                        }
                    }

                    foreach (Control otherControl in this.Controls)
                    {
                        if (otherControl is PictureBox otherPictureBox && otherPictureBox != pictureBox && pictureBox.Bounds.IntersectsWith(otherPictureBox.Bounds))
                        {
                            int otherPicId = otherPictureBox.GetHashCode();

                            // Calculate the direction based on the relative positions of the picture boxes
                            int directionX = pictureBox.Location.X > otherPictureBox.Location.X ? 1 : -1;
                            int directionY = pictureBox.Location.Y > otherPictureBox.Location.Y ? 1 : -1;

                            try
                            {
                                velocities[picId] = new Point(velocities[picId].X * directionX, velocities[picId].Y * directionY);
                                velocities[otherPicId] = new Point(velocities[otherPicId].X * -directionX, velocities[otherPicId].Y * -directionY);

                                /*if (pictureBox.Bounds.IntersectsWith(otherPictureBox.Bounds))
                                {
                                    pictureBox.Location = new Point(pictureBox.Location.X + velocities[picId].X, pictureBox.Location.Y + velocities[picId].Y);
                                    otherPictureBox.Location = new Point(otherPictureBox.Location.X + velocities[otherPicId].X, otherPictureBox.Location.Y + velocities[otherPicId].Y);
                                }*/
                            }
                            catch { }
                        }
                    }

                    if (pictureBox.Bottom < this.ClientSize.Height)
                    {
                        // Apply gravity
                        var velocity = velocities[picId];
                        int newY = velocity.Y + Gravity;

                        if (newY > TerminalVelocity) newY = TerminalVelocity;
                        if (newY < -TerminalVelocity) newY = -TerminalVelocity * 2;

                        velocities[picId] = new Point(velocity.X, newY);
                    }
                }
            }
        }
    }
}