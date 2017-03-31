using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace BarrelShaderLibrary
{
    public class BarrelDistortion : ShaderEffect
    {
        #region Constructors

        static BarrelDistortion()
        {
            _pixelShader.UriSource = Global.MakePackUri("Barrel.ps");
        }

        public BarrelDistortion()
        {
            this.PixelShader = _pixelShader;

            // Update each DependencyProperty that's registered with a shader register.  This
            // is needed to ensure the shader gets sent the proper default value.
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(factorProperty);
            UpdateShaderValue(xCenterProperty);
            UpdateShaderValue(yCenterProperty);
            UpdateShaderValue(blueOffsetProperty);
            UpdateShaderValue(redOffsetProperty);
        }

        #endregion

        #region Dependency Properties

        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        // Brush-valued properties turn into sampler-property in the shader.
        // This helper sets "ImplicitInput" as the default, meaning the default
        // sampler is whatever the rendering of the element it's being applied to is.
        public static readonly DependencyProperty InputProperty =
            ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(BarrelDistortion), 0);



        public float factor
        {
            get { return (float)GetValue(factorProperty); }
            set { SetValue(factorProperty, value); }
        }

        public float xCenter
        {
            get { return (float)GetValue(xCenterProperty); }
            set { SetValue(xCenterProperty, value); }
        }

        public float yCenter
        {
            get { return (float)GetValue(yCenterProperty); }
            set { SetValue(yCenterProperty, value); }
        }

        public float blueOffset
        {
            get { return (float)GetValue(blueOffsetProperty); }
            set { SetValue(blueOffsetProperty, value); }
        }
        public float redOffset
        {
            get { return (float)GetValue(redOffsetProperty); }
            set { SetValue(redOffsetProperty, value); }
        }


        // Scalar-valued properties turn into shader constants with the register
        // number sent into PixelShaderConstantCallback().
        public static readonly DependencyProperty factorProperty =
            DependencyProperty.Register("factor", typeof(float), typeof(BarrelDistortion),
                    new UIPropertyMetadata(1.0f, PixelShaderConstantCallback(0)));

        public static readonly DependencyProperty xCenterProperty =
            DependencyProperty.Register("xCenter", typeof(float), typeof(BarrelDistortion),
                    new UIPropertyMetadata(0.5f, PixelShaderConstantCallback(1)));

        public static readonly DependencyProperty yCenterProperty =
            DependencyProperty.Register("yCenter", typeof(float), typeof(BarrelDistortion),
                    new UIPropertyMetadata(0.5f, PixelShaderConstantCallback(2)));

        public static readonly DependencyProperty blueOffsetProperty =
            DependencyProperty.Register("blueOffset", typeof(float), typeof(BarrelDistortion),
                    new UIPropertyMetadata(0.024f, PixelShaderConstantCallback(3)));

        public static readonly DependencyProperty redOffsetProperty =
            DependencyProperty.Register("redOffset", typeof(float), typeof(BarrelDistortion),
                    new UIPropertyMetadata(-0.012f, PixelShaderConstantCallback(4)));

        #endregion

        #region Member Data

        private static PixelShader _pixelShader = new PixelShader();

        #endregion

    }
}
