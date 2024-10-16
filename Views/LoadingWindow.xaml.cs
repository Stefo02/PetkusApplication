using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace PetkusApplication.Views
{
    public partial class LoadingWindow : Window
    {
        private const int numberOfCircles = 8; // Broj elipsi
        private const double circleRadius = 15; // Poluprečnik elipse
        private const double animationRadius = 50; // Poluprečnik kruga
        private const double rotationSpeed = 3; // Brzina rotacije
        private double angle = 0; // Početni ugao

        public LoadingWindow()
        {
            InitializeComponent();
            CreateCircles();
            StartAnimation();
        }

        private void CreateCircles()
        {
            for (int i = 0; i < numberOfCircles; i++)
            {
                // Kreiraj elipsu
                Ellipse circle = new Ellipse
                {
                    Width = circleRadius,
                    Height = circleRadius,
                    Fill = new SolidColorBrush(Colors.White),
                    Margin = new Thickness(2) // Razmak od 2 piksela
                };

                // Izračunaj poziciju
                double angleInRadians = i * (360.0 / numberOfCircles) * (Math.PI / 180); // Ugao u radijanima
                double x = (CirclesContainer.Width / 2) + animationRadius * Math.Cos(angleInRadians) - (circleRadius / 2);
                double y = (CirclesContainer.Height / 2) + animationRadius * Math.Sin(angleInRadians) - (circleRadius / 2);

                // Postavi elipsu na izračunatu poziciju
                Canvas.SetLeft(circle, x);
                Canvas.SetTop(circle, y);

                // Dodaj elipsu u Canvas
                CirclesContainer.Children.Add(circle);

                // Pokreni animaciju za elipsu
                AnimateCircle(circle);
            }
        }

        private void StartAnimation()
        {
            // Postavi tajmer za animaciju
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50); // U intervalima od 50ms
            timer.Tick += (s, e) =>
            {
                // Pomeraj elipse oko centra
                MoveCircles();
            };
            timer.Start();
        }

        private void MoveCircles()
        {
            angle += rotationSpeed; // Povećaj ugao
            if (angle >= 360) angle = 0; // Resetuj na 0 ako pređe 360

            for (int i = 0; i < CirclesContainer.Children.Count; i++)
            {
                Ellipse circle = CirclesContainer.Children[i] as Ellipse;

                // Izračunaj novu poziciju
                double angleInRadians = (angle + (360.0 / numberOfCircles * i)) * (Math.PI / 180); // Ugao u radijanima
                double x = (CirclesContainer.Width / 2) + animationRadius * Math.Cos(angleInRadians) - (circleRadius / 2);
                double y = (CirclesContainer.Height / 2) + animationRadius * Math.Sin(angleInRadians) - (circleRadius / 2);

                // Postavi novu poziciju elipse
                Canvas.SetLeft(circle, x);
                Canvas.SetTop(circle, y);
            }
        }

        private void AnimateCircle(Ellipse circle)
        {
            // Povećavanje i smanjivanje elipse
            DoubleAnimation scaleAnimation = new DoubleAnimation
            {
                From = 4.5,
                To = 10.5,
                Duration = new Duration(TimeSpan.FromSeconds(1)),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            circle.BeginAnimation(WidthProperty, scaleAnimation);
            circle.BeginAnimation(HeightProperty, scaleAnimation);
        }
    }
}