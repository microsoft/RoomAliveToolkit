using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Device = SharpDX.Direct3D11.Device;

namespace RoomAliveToolkit
{
    public class BlendShader : IDisposable
    {
        public BlendShader(Device device)
        {
            var shaderByteCode = new ShaderBytecode(File.ReadAllBytes("Content/BlendVS.cso"));
            vertexShader = new VertexShader(device, shaderByteCode);
            pixelShader = new PixelShader(device, new ShaderBytecode(File.ReadAllBytes("Content/BlendPS.cso")));

            // depth stencil state
            var depthStencilStateDesc = new DepthStencilStateDescription()
            {
                IsDepthEnabled = true,
                DepthWriteMask = DepthWriteMask.All,
                DepthComparison = Comparison.LessEqual,
                IsStencilEnabled = false,
            };
            depthStencilState = new DepthStencilState(device, depthStencilStateDesc);

            // rasterizer states
            var rasterizerStateDesc = new RasterizerStateDescription()
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid,
                IsDepthClipEnabled = true,
                IsFrontCounterClockwise = true,
                IsMultisampleEnabled = false,
            };
            rasterizerState = new RasterizerState(device, rasterizerStateDesc);

            // color sampler state
            var colorSamplerStateDesc = new SamplerStateDescription()
            {
                Filter = Filter.Anisotropic,
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Border,
                BorderColor = new SharpDX.Color4(0, 0, 0, 0),
                MaximumAnisotropy = 4,
            };
            colorSamplerState = new SamplerState(device, colorSamplerStateDesc);

            // constant buffer
            var vertexShaderConstantBufferDesc = new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                SizeInBytes = 64,
                CpuAccessFlags = CpuAccessFlags.Write,
                StructureByteStride = 0,
                OptionFlags = 0,
            };
            vertexShaderConstantBuffer = new SharpDX.Direct3D11.Buffer(device, vertexShaderConstantBufferDesc);

            var pixelShaderConstantBufferDesc = new BufferDescription()
            {
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                SizeInBytes = 1568,
                CpuAccessFlags = CpuAccessFlags.Write,
                StructureByteStride = 0,
                OptionFlags = 0,
            };
            pixelShaderConstantBuffer = new SharpDX.Direct3D11.Buffer(device, pixelShaderConstantBufferDesc);

            // vertex layout
            vertexInputLayout = new InputLayout(device, shaderByteCode.Data, new[]
            {
                new InputElement("pos", 0, Format.R32G32B32_Float, 0, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            });
        }

        VertexShader vertexShader;
        PixelShader pixelShader;
        DepthStencilState depthStencilState;
        RasterizerState rasterizerState;
        SamplerState colorSamplerState;
        SharpDX.Direct3D11.Buffer vertexShaderConstantBuffer, pixelShaderConstantBuffer;
        InputLayout vertexInputLayout;

        public void Dispose()
        {
            vertexShader.Dispose();
            pixelShader.Dispose();
            depthStencilState.Dispose();
            rasterizerState.Dispose();
            colorSamplerState.Dispose();
            vertexShaderConstantBuffer.Dispose();
            vertexInputLayout.Dispose();
        }

        public void SetVertexShaderConstants(DeviceContext deviceContext, SharpDX.Matrix projection)
        {
            DataStream dataStream;
            deviceContext.MapSubresource(vertexShaderConstantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);
            dataStream.PackedWrite(projection);
            deviceContext.UnmapSubresource(vertexShaderConstantBuffer, 0);
        }

        public void SetPixelShaderConstants(DeviceContext deviceContext, List<ProjectorCameraEnsemble.Projector> projectors, List<ProjectorCameraEnsemble.Camera> cameras)
        {
            DataStream dataStream;
            deviceContext.MapSubresource(pixelShaderConstantBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out dataStream);

            // numProjectors
            dataStream.PackedWrite((uint)projectors.Count);

            foreach (var projector in projectors)
            {
                var view = projector.pose.ToSharp4x4();
                view.Invert();
                view.Transpose();

                var cameraMatrix = projector.cameraMatrix;
                float fx = (float)cameraMatrix[0, 0];
                float fy = (float)cameraMatrix[1, 1];
                float cx = (float)cameraMatrix[0, 2];
                float cy = (float)cameraMatrix[1, 2];

                float near = 0.1f;
                float far = 10.0f;

                float w = projector.width;
                float h = projector.height;

                var projection = GraphicsTransforms.ProjectionMatrixFromCameraMatrix(fx, fy, cx, cy, w, h, near, far);
                projection.Transpose();

                var viewProjection = view * projection;

                // every element of an array starts on a new 4-component vector
                dataStream.NextVector();

                // invertedFocalLength
                dataStream.PackedWrite(1.0f / fx);

                // viewProjection
                dataStream.PackedWrite(viewProjection);

                // projectorColor
                var projectorColor = HSV2RGB(new float[] { (float)projectors.IndexOf(projector) / (float)projectors.Count, 0.3f, 1});
                var color = new Vector3(projectorColor);
                dataStream.PackedWrite(color);
            }

            // skip past the empty entries in the fixed length array
            dataStream.Seek(784, SeekOrigin.Begin);

            dataStream.PackedWrite((uint)cameras.Count);

            foreach (var camera in cameras)
            {
                var depthToWorld = camera.pose.ToSharp4x4();
                var depthToColor = camera.calibration.depthToColorTransform.ToSharp4x4();
                depthToWorld.Transpose();
                depthToColor.Transpose();
                var worldToDepth = depthToWorld;
                worldToDepth.Invert();
                var worldToColor = worldToDepth * depthToColor;

                // every element of an array starts on a new 4-component vector
                dataStream.NextVector();

                // worldToColor
                dataStream.PackedWrite(worldToColor);

                // f
                dataStream.PackedWrite(new Vector2((float)camera.calibration.colorCameraMatrix[0, 0] / (float)Kinect2Calibration.colorImageWidth,
    -(float)camera.calibration.colorCameraMatrix[1, 1] / (float)Kinect2Calibration.colorImageHeight));
                // c
                dataStream.PackedWrite(new Vector2((float)camera.calibration.colorCameraMatrix[0, 2] / (float)Kinect2Calibration.colorImageWidth,
                    1f - (float)camera.calibration.colorCameraMatrix[1, 2] / (float)Kinect2Calibration.colorImageHeight));
                // k1
                dataStream.PackedWrite((float)camera.calibration.colorLensDistortion[0]);
                // k2
                dataStream.PackedWrite((float)camera.calibration.colorLensDistortion[1]);
            }

            deviceContext.UnmapSubresource(pixelShaderConstantBuffer, 0);
        }


        public void Render(DeviceContext deviceContext,
            List<ProjectorCameraEnsemble.Projector> projectors,
            List<ProjectorCameraEnsemble.Camera> cameras,
            EnsembleDeviceResources ensembleDeviceResources,
            VertexBufferBinding vertexBufferBinding,
            SharpDX.Direct3D11.Buffer indexBuffer,
            SharpDX.Matrix worldViewProjection,
            ShaderResourceView depthMapsSRV,
            ShaderResourceView colorDepthMapsSRV,
            ShaderResourceView zBufferSRV)
        {
            deviceContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            deviceContext.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);

            deviceContext.InputAssembler.InputLayout = vertexInputLayout;
            deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            deviceContext.VertexShader.Set(vertexShader);
            deviceContext.VertexShader.SetConstantBuffer(0, vertexShaderConstantBuffer);
            deviceContext.PixelShader.Set(pixelShader);
            deviceContext.PixelShader.SetSampler(0, colorSamplerState);
            deviceContext.PixelShader.SetConstantBuffer(0, pixelShaderConstantBuffer);
            deviceContext.OutputMerger.DepthStencilState = depthStencilState;
            deviceContext.Rasterizer.State = rasterizerState;

            SetVertexShaderConstants(deviceContext, worldViewProjection);

            // TODO: these do not need to be set every frame:
            SetPixelShaderConstants(deviceContext, projectors, cameras);

            deviceContext.PixelShader.SetShaderResource(0, depthMapsSRV);
            deviceContext.PixelShader.SetShaderResource(1, ensembleDeviceResources.colorImageTextureRV);
            deviceContext.PixelShader.SetShaderResource(2, colorDepthMapsSRV);
            deviceContext.PixelShader.SetShaderResource(3, zBufferSRV);


            int numVertices = cameras.Count * Kinect2Calibration.depthImageWidth * Kinect2Calibration.depthImageHeight * 6;
            deviceContext.DrawIndexed(numVertices, 0, 0);

            deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding());
            deviceContext.InputAssembler.SetIndexBuffer(null, Format.R32_UInt, 0);
            deviceContext.PixelShader.SetShaderResource(0, null);
            deviceContext.PixelShader.SetShaderResource(1, null);
            deviceContext.PixelShader.SetShaderResource(2, null);
            deviceContext.PixelShader.SetShaderResource(3, null);


        }

        public float[] HSV2RGB(float[] hsv)
        {
            float[] color = new float[3];
            for (int ii = 0; ii < 3; ii++)
                color[ii] = 0;
            float f, p, q, t;
            float h, s, v;
            float r = 0, g = 0, b = 0;
            float i;
            if (hsv[1] == 0)
            {
                if (hsv[2] != 0)
                {
                    color[0] = color[1] = color[2] = hsv[2];
                }
            }
            else
            {
                h = hsv[0] * 360.0f;
                s = hsv[1];
                v = hsv[2];
                if (h == 360.0)
                {
                    h = 0;
                }
                h /= 60;
                i = (float) Math.Floor(h);
                f = h - i;
                p = v * (1.0f - s);
                q = v * (1.0f - (s * f));
                t = v * (1.0f - (s * (1.0f - f)));
                if (i == 0)
                {
                    r = v;
                    g = t;
                    b = p;
                }
                else if (i == 1)
                {
                    r = q;
                    g = v;
                    b = p;
                }
                else if (i == 2)
                {
                    r = p;
                    g = v;
                    b = t;
                }
                else if (i == 3)
                {
                    r = p;
                    g = q;
                    b = v;
                }
                else if (i == 4)
                {
                    r = t;
                    g = p;
                    b = v;
                }
                else if (i == 5)
                {
                    r = v;
                    g = p;
                    b = q;
                }
                color[0] = r;
                color[1] = g;
                color[2] = b;
            }
            return color;
        }


    }

    public static class SharpDXConversions
    {
        public static SharpDX.Matrix ToSharp4x4(this Matrix m)
        {
            var output = new SharpDX.Matrix();
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    output[i, j] = (float)m[i, j];
            return output;
        }
    }


    // HLSL variables are on 4-byte boundaries; additionally, a variable may not cross a 4-component vector (16 bytes);
    // arrays are not packed; every element is stored on a 4-component vector (applies to array of struct, too).
    public static class HLSLPackedDataStreamExtensions
    {
        // unconditionally advance to the next 4-component value;
        // this can be useful if writing out an array manually
        public static void NextVector(this DataStream dataStream)
        {
            int bytesRemaining = 16 - (int)dataStream.Position % 16;
            for (int i = 0; i < bytesRemaining; i++)
                dataStream.WriteByte(0);
        }

        // advance to the next 4-component value if a value of a given size would cross a 16-byte boundary;
        static void NextVector(this DataStream dataStream, int sizeBytes)
        {
            sizeBytes = Math.Min(sizeBytes, 16);
            int bytesRemaining = 16 - (int)dataStream.Position % 16;
            if (sizeBytes > bytesRemaining)
                for (int i = 0; i < bytesRemaining; i++)
                    dataStream.WriteByte(0);
        }

        public static void PackedWrite(this DataStream dataStream, float value)
        {
            dataStream.Write(value);
        }

        public static void PackedWrite(this DataStream dataStream, int value)
        {
            dataStream.Write(value);
        }

        public static void PackedWrite(this DataStream dataStream, uint value)
        {
            dataStream.Write(value);
        }

        public static void PackedWrite(this DataStream dataStream, SharpDX.Matrix value)
        {
            dataStream.NextVector(4 * 4);
            // hlsl matrices are stored column major by default
            for (int col = 0; col < 4; col++)
                for (int row = 0; row < 4; row++)
                    dataStream.Write(value[row, col]);
        }

        public static void PackedWrite(this DataStream dataStream, SharpDX.Vector2 value)
        {
            dataStream.NextVector(2 * 4);
            dataStream.Write(value);
        }

        public static void PackedWrite(this DataStream dataStream, SharpDX.Vector3 value)
        {
            dataStream.NextVector(3 * 4);
            dataStream.Write(value);
        }

        public static void PackedWrite(this DataStream dataStream, SharpDX.Vector4 value)
        {
            dataStream.NextVector(4 * 4);
            dataStream.Write(value);
        }

        public static void PackedWrite(this DataStream dataStream, float[] value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                dataStream.NextVector();
                dataStream.Write(value[i]);
            }
        }

    }


}
