using HelixToolkit.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace RoboUtes
{
    public partial class ThreeDimensionalArm : UserControl
    {
        private Model3DGroup arm;

        private BoxVisual3D mybox;

        private Model3D armBase;
        private Model3D turntable;
        private Model3D shoulder;
        private Model3D elbow;
        private Model3D hand;

        private Transform3DGroup transformGroup = new Transform3DGroup();

        public float TurntableAngle
        {
            get
            {
                return (float)GetValue(TurntableAngleProperty);
            }
            set
            {
                SetValue(TurntableAngleProperty, value);
            }
        }

        public float ShoulderAngle
        {
            get
            {
                return (float)GetValue(ShoulderAngleProperty);
            }
            set
            {
                SetValue(ShoulderAngleProperty, value);
            }
        }

        public float ElbowAngle
        {
            get
            {
                return (float)GetValue(ElbowAngleProperty);
            }
            set
            {
                SetValue(ElbowAngleProperty, value);
            }
        }

        private static readonly DependencyProperty TurntableAngleProperty =
          DependencyProperty.Register("TurntableAngle", typeof(float),
            typeof(ThreeDimensionalArm), new PropertyMetadata(0.0f, PropertyChangedCallback));

        private static readonly DependencyProperty ShoulderAngleProperty =
          DependencyProperty.Register("ShoulderAngle", typeof(float),
            typeof(ThreeDimensionalArm), new PropertyMetadata(0.0f, PropertyChangedCallback));

        private static readonly DependencyProperty ElbowAngleProperty =
          DependencyProperty.Register("ElbowAngle", typeof(float),
            typeof(ThreeDimensionalArm), new PropertyMetadata(0.0f, PropertyChangedCallback));

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            PropertyChanged(e);
        }

        private delegate void PropertyChangedHandler(DependencyPropertyChangedEventArgs e);
        private static event PropertyChangedHandler PropertyChanged;

        public ThreeDimensionalArm()
        {
            InitializeComponent();

            arm = new Model3DGroup();

            //load the model files
            ModelImporter importer = new ModelImporter() { DefaultMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.LightGray)) };
            armBase = importer.Load("Resources/base4.obj");
            turntable = importer.Load("Resources/shoulder5.obj");
            shoulder = importer.Load("Resources/shoulder3.obj");
            elbow = importer.Load("Resources/elbow3.obj");
            hand = importer.Load("Resources/newarmhand2.obj");

            //add the pieces to the 3d model
            arm.Children.Add(armBase);
            arm.Children.Add(turntable);
            arm.Children.Add(shoulder);
            arm.Children.Add(elbow);

            // display model
            modelVisual3d.Content = arm;

            //creates a small cube for determining 3d coordinates
            mybox = new BoxVisual3D();
            mybox.Height = 1;
            mybox.Width = 1;
            mybox.Length = 1;

            mybox.Center = new Point3D(0, 0, 20);

            arm.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90));

            PropertyChanged += ThreeDimensionalArm_PropertyChanged;
        }

        private void ThreeDimensionalArm_PropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            switch(e.Property.Name)
            {
                case "TurntableAngle":
                    drawTurntable();
                    break;
                case "ShoulderAngle":
                    drawShoulder();
                    break;
                case "ElbowAngle":
                    drawElbow();
                    break;
            }
        }

        private void drawTurntable()  //rotate turntable
        {
            //apply transformation
            turntable.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), TurntableAngle));

            drawShoulder();
        }


        private void drawShoulder()  //pivots shoulder
        {
            //new group of transformations, the group will add movements
            var group3d = new Transform3DGroup();

            group3d.Children.Add(turntable.Transform);

            Point3D origin = group3d.Transform(new Point3D(0, 9, 1));


            double turntableAngleInRadians = Math.PI / 180 * TurntableAngle;

            double x = Math.Cos(-turntableAngleInRadians);
            double y = 0;
            double z = Math.Sin(-turntableAngleInRadians);

            RotateTransform3D shoulderTransform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(x, y, z), ShoulderAngle));

            shoulderTransform.CenterX = origin.X;
            shoulderTransform.CenterY = origin.Y;
            shoulderTransform.CenterZ = origin.Z;

            //add it to the transformation group, turntable transform will be applied to shoulder as well
            group3d.Children.Add(shoulderTransform);

            shoulder.Transform = group3d;

            drawElbow();  //move elbow with shoulder
        }

        private void drawElbow()  //pivots elbow
        {
            var groupd3d = new Transform3DGroup();
            groupd3d.Children.Add(shoulder.Transform);

            Point3D origin = groupd3d.Transform(new Point3D(0, 19, 5));

            double turntableAngleInRadians = Math.PI / 180 * TurntableAngle;

            double x = Math.Cos(-turntableAngleInRadians);
            double y = 0;
            double z = Math.Sin(-turntableAngleInRadians);

            RotateTransform3D elbowTransform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(x, y, z), ElbowAngle));

            elbowTransform.CenterX = origin.X;
            elbowTransform.CenterY = origin.Y;
            elbowTransform.CenterZ = origin.Z;

            groupd3d.Children.Add(elbowTransform);

            elbow.Transform = groupd3d;
        }
    }
}
