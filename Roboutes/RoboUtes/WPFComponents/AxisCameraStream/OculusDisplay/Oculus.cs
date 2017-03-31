using OculusWrap;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OculusDisplay
{
    public class Oculus
    {
        /// <summary>
        /// This restricts the event of PositionChanged
        ///     Value is delta of degrees
        /// </summary>
        public double MovementPrecision;

        private Euler __eulerAngles = new Euler(0,0,0);
        private Euler _lastEventAngles = new Euler(180, 180,180);
        private Euler _eulerAngles
        {

            get { return __eulerAngles; }
            set
            {
                Euler oldAngles = __eulerAngles;
                __eulerAngles = value;

                if (PositionChanged == null) return;

                // If above threshold amount of change, raise event
                double deltaX = Math.Abs(value.X - _lastEventAngles.X);
                double deltaY = Math.Abs(value.Y - _lastEventAngles.Y);
                double deltaZ = Math.Abs(value.Z - _lastEventAngles.Z);

                if (deltaX > MovementPrecision || deltaY > MovementPrecision || deltaZ > MovementPrecision)
                {
                    _lastEventAngles = value;
                    PositionChanged(this, new OculusPositionChangedEventArgs(value));
                }
            }
        }
        public Euler EulerAngles { get { return _eulerAngles; } }

        public Bitmap Image;

        public delegate void PositionChangedHanlder(object sender, OculusPositionChangedEventArgs e);
        public event PositionChangedHanlder PositionChanged;

        /// <summary>
        /// This represents an Oculus display, tested on DK2.  Using a flat Bitmap image to display on a wall in front of the user.
        ///     This will report any changes in orientation via the PositionChanged event.
        ///     Update the Image property in order to change the displayed image.
        /// </summary>
        public Oculus()
        {
            RenderToOculusAsync();
        }

        public void RenderToOculusAsync()
        {
            BackgroundWorker renderWorker = new BackgroundWorker();
            renderWorker.DoWork += (s, ev) =>
            {
                RenderToOculus();
            };

            renderWorker.RunWorkerAsync();
        }

        public void RenderToOculus()
        {
            Wrap oculus = new Wrap();
            Hmd hmd = null;
            InputLayout inputLayout = null;
            SharpDX.Direct3D11.Buffer contantBuffer = null;
            SharpDX.Direct3D11.Buffer vertexBuffer = null;
            ShaderSignature shaderSignature = null;
            PixelShader pixelShader = null;
            ShaderBytecode pixelShaderByteCode = null;
            VertexShader vertexShader = null;
            ShaderBytecode vertexShaderByteCode = null;
            Texture2D mirrorTextureD3D11 = null;
            Layers layers = null;
            EyeTexture[] eyeTextures = null;
            DeviceContext immediateContext = null;
            DepthStencilState depthStencilState = null;
            DepthStencilView depthStencilView = null;
            Texture2D depthBuffer = null;
            RenderTargetView backBufferRenderTargetView = null;
            Texture2D backBuffer = null;
            SharpDX.DXGI.SwapChain swapChain = null;
            Factory factory = null;

            // Initialize the Oculus runtime.
            bool success = oculus.Initialize();
            if (!success)
            {
                throw new Exception("Failed to initialize the Oculus runtime library.");
            }

            // Use the head mounted display.
            OVR.GraphicsLuid graphicsLuid;
            hmd = oculus.Hmd_Create(out graphicsLuid);
            if (hmd == null)
            {
                throw new Exception("Oculus Rift not detected.");
            }

            if (hmd.ProductName == string.Empty)
            {
                throw new Exception("The HMD is not enabled.");
            }

            try
            {
                // Specify which head tracking capabilities to enable.
                hmd.SetEnabledCaps(OVR.HmdCaps.DebugDevice);

                // Start the sensor which informs of the Rift's pose and motion
                hmd.ConfigureTracking(OVR.TrackingCaps.Orientation | OVR.TrackingCaps.MagYawCorrection | OVR.TrackingCaps.Position, OVR.TrackingCaps.None);

                // Create a set of layers to submit.
                eyeTextures = new EyeTexture[2];
                OVR.ovrResult result;

                // Create DirectX drawing device.
                SharpDX.Direct3D11.Device device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.Debug);

                // Create DirectX Graphics Interface factory, used to create the swap chain.
                factory = new Factory();

                immediateContext = device.ImmediateContext;

                Viewport viewport = new Viewport(0, 0, hmd.Resolution.Width, hmd.Resolution.Height, 0.0f, 1.0f);

                immediateContext.OutputMerger.SetDepthStencilState(depthStencilState);
                immediateContext.OutputMerger.SetRenderTargets(depthStencilView, backBufferRenderTargetView);
                immediateContext.Rasterizer.SetViewport(viewport);

                // Retrieve the DXGI device, in order to set the maximum frame latency.
                using (SharpDX.DXGI.Device1 dxgiDevice = device.QueryInterface<SharpDX.DXGI.Device1>())
                {
                    dxgiDevice.MaximumFrameLatency = 1;
                }

                layers = new Layers();

                #region Eye FOV layer
                LayerEyeFov layerEyeFov = layers.AddLayerEyeFov();

                for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++)
                {
                    OVR.EyeType eye = (OVR.EyeType)eyeIndex;
                    EyeTexture eyeTexture = new EyeTexture();
                    eyeTextures[eyeIndex] = eyeTexture;

                    // Retrieve size and position of the texture for the current eye.
                    eyeTexture.FieldOfView = hmd.DefaultEyeFov[eyeIndex];
                    eyeTexture.TextureSize = hmd.GetFovTextureSize(eye, hmd.DefaultEyeFov[eyeIndex], 1.0f);
                    eyeTexture.RenderDescription = hmd.GetRenderDesc(eye, hmd.DefaultEyeFov[eyeIndex]);
                    eyeTexture.HmdToEyeViewOffset = eyeTexture.RenderDescription.HmdToEyeViewOffset;
                    eyeTexture.ViewportSize.Position = new OVR.Vector2i(0, 0);
                    eyeTexture.ViewportSize.Size = eyeTexture.TextureSize;
                    eyeTexture.Viewport = new Viewport(0, 0, eyeTexture.TextureSize.Width, eyeTexture.TextureSize.Height, 0.0f, 1.0f);

                    // Define a texture at the size recommended for the eye texture.
                    eyeTexture.Texture2DDescription = new Texture2DDescription();
                    eyeTexture.Texture2DDescription.Width = eyeTexture.TextureSize.Width;
                    eyeTexture.Texture2DDescription.Height = eyeTexture.TextureSize.Height;
                    eyeTexture.Texture2DDescription.ArraySize = 1;
                    eyeTexture.Texture2DDescription.MipLevels = 1;
                    eyeTexture.Texture2DDescription.Format = Format.R8G8B8A8_UNorm;
                    eyeTexture.Texture2DDescription.SampleDescription = new SampleDescription(1, 0);
                    eyeTexture.Texture2DDescription.Usage = ResourceUsage.Default;
                    eyeTexture.Texture2DDescription.CpuAccessFlags = CpuAccessFlags.None;
                    eyeTexture.Texture2DDescription.BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget;

                    // Convert the SharpDX texture description to the native Direct3D texture description.
                    OVR.D3D11.D3D11_TEXTURE2D_DESC swapTextureDescriptionD3D11 = SharpDXHelpers.CreateTexture2DDescription(eyeTexture.Texture2DDescription);

                    // Create a SwapTextureSet, which will contain the textures to render to, for the current eye.
                    result = hmd.CreateSwapTextureSetD3D11(device.NativePointer, ref swapTextureDescriptionD3D11, OVR.D3D11.SwapTextureSetD3D11Flags.None, out eyeTexture.SwapTextureSet);
                    WriteErrorDetails(oculus, result, "Failed to create swap texture set.");

                    // Create room for each DirectX texture in the SwapTextureSet.
                    eyeTexture.Textures = new Texture2D[eyeTexture.SwapTextureSet.TextureCount];
                    eyeTexture.RenderTargetViews = new RenderTargetView[eyeTexture.SwapTextureSet.TextureCount];

                    // Create a texture 2D and a render target view, for each unmanaged texture contained in the SwapTextureSet.
                    for (int textureIndex = 0; textureIndex < eyeTexture.SwapTextureSet.TextureCount; textureIndex++)
                    {
                        // Retrieve the current textureData object.
                        OVR.D3D11.D3D11TextureData textureData = eyeTexture.SwapTextureSet.Textures[textureIndex];

                        // Create a managed Texture2D, based on the unmanaged texture pointer.
                        eyeTexture.Textures[textureIndex] = new Texture2D(textureData.Texture);

                        // Create a render target view for the current Texture2D.
                        eyeTexture.RenderTargetViews[textureIndex] = new RenderTargetView(device, eyeTexture.Textures[textureIndex]);
                    }

                    // Define the depth buffer, at the size recommended for the eye texture.
                    eyeTexture.DepthBufferDescription = new Texture2DDescription();
                    eyeTexture.DepthBufferDescription.Format = Format.D32_Float;
                    eyeTexture.DepthBufferDescription.Width = eyeTexture.TextureSize.Width;
                    eyeTexture.DepthBufferDescription.Height = eyeTexture.TextureSize.Height;
                    eyeTexture.DepthBufferDescription.ArraySize = 1;
                    eyeTexture.DepthBufferDescription.MipLevels = 1;
                    eyeTexture.DepthBufferDescription.SampleDescription = new SampleDescription(1, 0);
                    eyeTexture.DepthBufferDescription.Usage = ResourceUsage.Default;
                    eyeTexture.DepthBufferDescription.BindFlags = BindFlags.DepthStencil;
                    eyeTexture.DepthBufferDescription.CpuAccessFlags = CpuAccessFlags.None;
                    eyeTexture.DepthBufferDescription.OptionFlags = ResourceOptionFlags.None;

                    // Create the depth buffer.
                    eyeTexture.DepthBuffer = new Texture2D(device, eyeTexture.DepthBufferDescription);
                    eyeTexture.DepthStencilView = new DepthStencilView(device, eyeTexture.DepthBuffer);

                    // Specify the texture to show on the HMD.
                    layerEyeFov.ColorTexture[eyeIndex] = eyeTexture.SwapTextureSet.SwapTextureSetPtr;
                    layerEyeFov.Viewport[eyeIndex].Position = new OVR.Vector2i(0, 0);
                    layerEyeFov.Viewport[eyeIndex].Size = eyeTexture.TextureSize;
                    layerEyeFov.Fov[eyeIndex] = eyeTexture.FieldOfView;
                    layerEyeFov.Header.Flags = OVR.LayerFlags.None;
                }
                #endregion

                #region Render loop
                while (true)
                {
                    OVR.Vector3f[] hmdToEyeViewOffsets = { eyeTextures[0].HmdToEyeViewOffset, eyeTextures[1].HmdToEyeViewOffset };
                    double displayMidpoint = hmd.GetPredictedDisplayTime(0);
                    OVR.TrackingState trackingState = hmd.GetTrackingState(displayMidpoint);
                    OVR.Posef[] eyePoses = new OVR.Posef[2];

                    // Calculate the position and orientation of each eye.
                    oculus.CalcEyePoses(trackingState.HeadPose.ThePose, hmdToEyeViewOffsets, ref eyePoses);

                    Quaternion rotationQuaternion = SharpDXHelpers.ToQuaternion(eyePoses[0].Orientation);
                    _eulerAngles = Euler.FromQuaternion(rotationQuaternion);

                    for (int eyeIndex = 0; eyeIndex < 2; eyeIndex++)
                    {
                        OVR.EyeType eye = (OVR.EyeType)eyeIndex;
                        EyeTexture eyeTexture = eyeTextures[eyeIndex];

                        layerEyeFov.RenderPose[eyeIndex] = eyePoses[eyeIndex];

                        // Retrieve the index of the active texture and select the next texture as being active next.
                        int textureIndex = eyeTexture.SwapTextureSet.CurrentIndex++;

                        immediateContext.OutputMerger.SetRenderTargets(eyeTexture.DepthStencilView, eyeTexture.RenderTargetViews[textureIndex]);
                        immediateContext.ClearRenderTargetView(eyeTexture.RenderTargetViews[textureIndex], SharpDX.Color.Black);
                        immediateContext.ClearDepthStencilView(eyeTexture.DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
                        immediateContext.Rasterizer.SetViewport(eyeTexture.Viewport);


                    }

                    if (Image != null)
                    {
                        int imageStreamSize = Image.Height * Image.Width * 4;
                        MemoryStream imageStream = new MemoryStream(imageStreamSize);
                        Image.Save(imageStream, System.Drawing.Imaging.ImageFormat.Png);

                        LayerQuad hudLayer = layers.AddLayerQuadInWorld();
                        hudLayer.Header.Type = OVR.LayerType.Quad;
                        hudLayer.Header.Flags = OVR.LayerFlags.HighQuality;
                        hudLayer.Viewport = new OVR.Recti(new OVR.Vector2i(0, 0), new OVR.Sizei(hmd.Resolution.Width, hmd.Resolution.Height));
                        hudLayer.QuadSize = new OVR.Vector2f()
                        {
                            X = hmd.Resolution.Width,
                            Y = hmd.Resolution.Height
                        };
                        hudLayer.QuadPoseCenter = new OVR.Posef()
                        {
                            Orientation = eyePoses[0].Orientation,
                            Position = eyePoses[0].Position
                        };
                    }

                    //OVR.D3D11.D3D11TextureData imageTextureData = hmd.create

                    //hudLayer.ColorTexture = imageTextureData.Texture;
                    //quad.ColorTexture = SharpDX.Direct3D11.Resource.FromStream(device, imageStream, imageStreamSize).NativePointer;

                    hmd.SubmitFrame(0, layers);
                }
                #endregion
            }
            finally
            {
                if (immediateContext != null)
                {
                    immediateContext.ClearState();
                    immediateContext.Flush();
                }

                // Release all resources
                Dispose(inputLayout);
                Dispose(contantBuffer);
                Dispose(vertexBuffer);
                Dispose(shaderSignature);
                Dispose(pixelShader);
                Dispose(pixelShaderByteCode);
                Dispose(vertexShader);
                Dispose(vertexShaderByteCode);
                Dispose(mirrorTextureD3D11);
                Dispose(layers);
                Dispose(eyeTextures[0]);
                Dispose(eyeTextures[1]);
                Dispose(immediateContext);
                Dispose(depthStencilState);
                Dispose(depthStencilView);
                Dispose(depthBuffer);
                Dispose(backBufferRenderTargetView);
                Dispose(backBuffer);
                Dispose(swapChain);
                Dispose(factory);

                // Disposing the device, before the hmd, will cause the hmd to fail when disposing.
                // Disposing the device, after the hmd, will cause the dispose of the device to fail.
                // It looks as if the hmd steals ownership of the device and destroys it, when it's shutting down.
                // device.Dispose();
                Dispose(hmd);
                Dispose(oculus);
            }
        }

        /// <summary>
		/// Write out any error details received from the Oculus SDK, into the debug output window.
		/// 
		/// Please note that writing text to the debug output window is a slow operation and will affect performance,
		/// if too many messages are written in a short timespan.
		/// </summary>
		/// <param name="oculus">OculusWrap object for which the error occurred.</param>
		/// <param name="result">Error code to write in the debug text.</param>
		/// <param name="message">Error message to include in the debug text.</param>
		private static void WriteErrorDetails(Wrap oculus, OVR.ovrResult result, string message)
        {
            if (result >= OVR.ovrResult.Success)
                return;

            // Retrieve the error message from the last occurring error.
            OVR.ovrErrorInfo errorInformation = oculus.GetLastError();

            string formattedMessage = string.Format("{0}. \nMessage: {1} (Error code={2})", message, errorInformation.ErrorString, errorInformation.Result);
            Trace.WriteLine(formattedMessage);
            Console.WriteLine(formattedMessage, message);

            throw new Exception(formattedMessage);
        }

        /// <summary>
        /// Dispose the specified object, unless it's a null object.
        /// </summary>
        /// <param name="disposable">Object to dispose.</param>
        private static void Dispose(IDisposable disposable)
        {
            if (disposable != null)
                disposable.Dispose();
        }

        #region Vertice Declarations
        static Vector4[] m_vertices = new Vector4[]
        {
			// Near
			new Vector4( 1,  1, -1, 1), new Vector4(1, 0, 0, 1),
            new Vector4( 1, -1, -1, 1), new Vector4(1, 0, 0, 1),
            new Vector4(-1, -1, -1, 1), new Vector4(1, 0, 0, 1),
            new Vector4(-1,  1, -1, 1), new Vector4(1, 0, 0, 1),
            new Vector4( 1,  1, -1, 1), new Vector4(1, 0, 0, 1),
            new Vector4(-1, -1, -1, 1), new Vector4(1, 0, 0, 1),	
			
			// Far
			new Vector4(-1, -1,  1, 1), new Vector4(0, 1, 0, 1),
            new Vector4( 1, -1,  1, 1), new Vector4(0, 1, 0, 1),
            new Vector4( 1,  1,  1, 1), new Vector4(0, 1, 0, 1),
            new Vector4( 1,  1,  1, 1), new Vector4(0, 1, 0, 1),
            new Vector4(-1,  1,  1, 1), new Vector4(0, 1, 0, 1),
            new Vector4(-1, -1,  1, 1), new Vector4(0, 1, 0, 1),	

			// Left
			new Vector4(-1,  1,  1, 1), new Vector4(0, 0, 1, 1),
            new Vector4(-1,  1, -1, 1), new Vector4(0, 0, 1, 1),
            new Vector4(-1, -1, -1, 1), new Vector4(0, 0, 1, 1),
            new Vector4(-1, -1, -1, 1), new Vector4(0, 0, 1, 1),
            new Vector4(-1, -1,  1, 1), new Vector4(0, 0, 1, 1),
            new Vector4(-1,  1,  1, 1), new Vector4(0, 0, 1, 1),	

			// Right
			new Vector4( 1, -1, -1, 1), new Vector4(1, 1, 0, 1),
            new Vector4( 1,  1, -1, 1), new Vector4(1, 1, 0, 1),
            new Vector4( 1,  1,  1, 1), new Vector4(1, 1, 0, 1),
            new Vector4( 1,  1,  1, 1), new Vector4(1, 1, 0, 1),
            new Vector4( 1, -1,  1, 1), new Vector4(1, 1, 0, 1),
            new Vector4( 1, -1, -1, 1), new Vector4(1, 1, 0, 1),	

			// Bottom
			new Vector4(-1, -1, -1, 1), new Vector4(1, 0, 1, 1),
            new Vector4( 1, -1, -1, 1), new Vector4(1, 0, 1, 1),
            new Vector4( 1, -1,  1, 1), new Vector4(1, 0, 1, 1),
            new Vector4( 1, -1,  1, 1), new Vector4(1, 0, 1, 1),
            new Vector4(-1, -1,  1, 1), new Vector4(1, 0, 1, 1),
            new Vector4(-1, -1, -1, 1), new Vector4(1, 0, 1, 1),	

			// Top
			new Vector4( 1,  1,  1, 1), new Vector4(0, 1, 1, 1),
            new Vector4( 1,  1, -1, 1), new Vector4(0, 1, 1, 1),
            new Vector4(-1,  1, -1, 1), new Vector4(0, 1, 1, 1),
            new Vector4(-1,  1, -1, 1), new Vector4(0, 1, 1, 1),
            new Vector4(-1,  1,  1, 1), new Vector4(0, 1, 1, 1),
            new Vector4( 1,  1,  1, 1), new Vector4(0, 1, 1, 1)
        };
        #endregion
    }


    public class OculusPositionChangedEventArgs : EventArgs
    {
        public Euler EulerAngles { get; private set; }

        public OculusPositionChangedEventArgs(Euler eulerAngles)
        {
            EulerAngles = eulerAngles;
        }
    }


    public static class SharpDXHelpers
    {
        /// <summary>
        /// Convert a Vector4 to a Vector3
        /// </summary>
        /// <param name="vector4">Vector4 to convert to a Vector3.</param>
        /// <returns>Vector3 based on the X, Y and Z coordinates of the Vector4.</returns>
        public static Vector3 ToVector3(this Vector4 vector4)
        {
            return new Vector3(vector4.X, vector4.Y, vector4.Z);
        }

        /// <summary>
        /// Convert an ovrVector3f to SharpDX Vector3.
        /// </summary>
        /// <param name="ovrVector3f">ovrVector3f to convert to a SharpDX Vector3.</param>
        /// <returns>SharpDX Vector3, based on the ovrVector3f.</returns>
        public static Vector3 ToVector3(this OVR.Vector3f ovrVector3f)
        {
            return new Vector3(ovrVector3f.X, ovrVector3f.Y, ovrVector3f.Z);
        }

        /// <summary>
        /// Convert an ovrMatrix4f to a SharpDX Matrix.
        /// </summary>
        /// <param name="ovrMatrix4f">ovrMatrix4f to convert to a SharpDX Matrix.</param>
        /// <returns>SharpDX Matrix, based on the ovrMatrix4f.</returns>
        public static Matrix ToMatrix(this OculusWrap.OVR.Matrix4f ovrMatrix4f)
        {
            return new Matrix(ovrMatrix4f.M11, ovrMatrix4f.M12, ovrMatrix4f.M13, ovrMatrix4f.M14, ovrMatrix4f.M21, ovrMatrix4f.M22, ovrMatrix4f.M23, ovrMatrix4f.M24, ovrMatrix4f.M31, ovrMatrix4f.M32, ovrMatrix4f.M33, ovrMatrix4f.M34, ovrMatrix4f.M41, ovrMatrix4f.M42, ovrMatrix4f.M43, ovrMatrix4f.M44);
        }

        /// <summary>
        /// Converts an ovrQuatf to a SharpDX Quaternion.
        /// </summary>
        public static Quaternion ToQuaternion(OVR.Quaternionf ovrQuatf)
        {
            return new Quaternion(ovrQuatf.X, ovrQuatf.Y, ovrQuatf.Z, ovrQuatf.W);
        }

        /// <summary>
        /// Creates a Direct3D texture description, based on the SharpDX texture description.
        /// </summary>
        /// <param name="texture2DDescription">SharpDX texture description.</param>
        /// <returns>Direct3D texture description, based on the SharpDX texture description.</returns>
        public static OVR.D3D11.D3D11_TEXTURE2D_DESC CreateTexture2DDescription(Texture2DDescription texture2DDescription)
        {
            OVR.D3D11.D3D11_TEXTURE2D_DESC d3d11DTexture = new OVR.D3D11.D3D11_TEXTURE2D_DESC();
            d3d11DTexture.Width = (uint)texture2DDescription.Width;
            d3d11DTexture.Height = (uint)texture2DDescription.Height;
            d3d11DTexture.MipLevels = (uint)texture2DDescription.MipLevels;
            d3d11DTexture.ArraySize = (uint)texture2DDescription.ArraySize;
            d3d11DTexture.Format = (OVR.D3D11.DXGI_FORMAT)texture2DDescription.Format;
            d3d11DTexture.SampleDesc.Count = (uint)texture2DDescription.SampleDescription.Count;
            d3d11DTexture.SampleDesc.Quality = (uint)texture2DDescription.SampleDescription.Quality;
            d3d11DTexture.Usage = (OVR.D3D11.D3D11_USAGE)texture2DDescription.Usage;
            d3d11DTexture.BindFlags = (uint)texture2DDescription.BindFlags;
            d3d11DTexture.CPUAccessFlags = (uint)texture2DDescription.CpuAccessFlags;
            d3d11DTexture.MiscFlags = (uint)texture2DDescription.OptionFlags;

            return d3d11DTexture;
        }
    }

    /// <summary>
    /// Contains all the fields used by each eye.
    /// </summary>
    public class EyeTexture : IDisposable
    {
        #region IDisposable Members
        /// <summary>
        /// Dispose contained fields.
        /// </summary>
        public void Dispose()
        {
            if (SwapTextureSet != null)
            {
                SwapTextureSet.Dispose();
                SwapTextureSet = null;
            }

            if (Textures != null)
            {
                foreach (Texture2D texture in Textures)
                    texture.Dispose();

                Textures = null;
            }

            if (RenderTargetViews != null)
            {
                foreach (RenderTargetView renderTargetView in RenderTargetViews)
                    renderTargetView.Dispose();

                RenderTargetViews = null;
            }

            if (DepthBuffer != null)
            {
                DepthBuffer.Dispose();
                DepthBuffer = null;
            }

            if (DepthStencilView != null)
            {
                DepthStencilView.Dispose();
                DepthStencilView = null;
            }
        }
        #endregion

        public Texture2DDescription Texture2DDescription;
        public OculusWrap.D3D11.SwapTextureSet SwapTextureSet;
        public Texture2D[] Textures;
        public RenderTargetView[] RenderTargetViews;
        public Texture2DDescription DepthBufferDescription;
        public Texture2D DepthBuffer;
        public Viewport Viewport;
        public DepthStencilView DepthStencilView;
        public OVR.FovPort FieldOfView;
        public OVR.Sizei TextureSize;
        public OVR.Recti ViewportSize;
        public OVR.EyeRenderDesc RenderDescription;
        public OVR.Vector3f HmdToEyeViewOffset;
    }
}


