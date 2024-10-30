using System;
using System.Drawing;

namespace octonev2.Forms
{
    public class Snow
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Size { get; set; }
        public float Opacity { get; set; }

        private static readonly Random random = new();
        private const float FALL_SPEED = 2.0f;

        public Snow(int formWidth)
        {
            Reset(formWidth);
            Y = random.Next(-600, 600);
        }

        public void Reset(int formWidth)
        {
            X = random.Next(formWidth);
            Y = -5;
            Size = random.Next(2, 4);
            Opacity = random.Next(40, 100) / 100f;
        }

        public void Update()
        {
            Y += FALL_SPEED;
        }

        public void Draw(Graphics g)
        {
            using var brush = new SolidBrush(Color.FromArgb((int)(255 * Opacity), 255, 255, 255));
            g.FillEllipse(brush, X, Y, Size, Size);
        }
    }
}
