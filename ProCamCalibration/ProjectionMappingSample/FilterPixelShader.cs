using System.IO;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using System.Runtime.InteropServices;

namespace RoomAliveToolkit
{
    public class FilterPixelShader
    {
        public FilterPixelShader(Device device, int imageWidth, int imageHeight, int constantBufferSize, string pixelShaderBytecodeFilename)
        {
            vertexShader = new VertexShader(device, new ShaderBytecode(File.ReadAllBytes("Content/FullScreenQuadVS.cso")));
            pixelShader = new PixelShader(device, new ShaderBytecode(File.ReadAllBytes(pixelShaderBytecodeFilename)));

            var rasterizerStateDesc = new RasterizerStateDescription()
            {
                CullMode = CullMode.None, 
                FillMode = FillMode.Solid,
                IsDepthClipEnabled = false,
                IsFrontCounterClockwise = true,
                IsMultisampleEnabled = false,
            };
            rasterizerState = new RasterizerState(device, rasterizerStateDesc);

            if (constantBufferSize > 0)
            {
                var constantBufferDesc = new BufferDescription()
                {
                    Usage = ResourceUsage.Dynamic,
                    BindFlags = BindFlags.ConstantBuffer,
                    SizeInBytes = constantBufferSize,
                    CpuAccessFlags = CpuAccessFlags.Write,
                    StructureByteStride = 0,
                    OptionFlags = 0,
                };
                constantBuffer = new Buffer(device, constantBufferDesc);
            }

            viewport = new Viewport(0, 0, imageWidth, imageHeight); // TODO: get these dimensions
            vertexBufferBinding = new VertexBufferBinding(null, 0, 0);
        }

        public virtual void Render(DeviceContext deviceContext, ShaderResourceView inputRV, RenderTargetView renderTargetView)
        {
            deviceContext.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            deviceContext.InputAssembler.InputLayout = null;
            deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleStrip;
            deviceContext.OutputMerger.SetTargets(renderTargetView);
            deviceContext.Rasterizer.State = rasterizerState;
            deviceContext.Rasterizer.SetViewport(viewport);
            deviceContext.VertexShader.SetShaderResource(0, null); // TODO: this should be done by the depthAndColorVS
            deviceContext.VertexShader.Set(vertexShader);
            deviceContext.GeometryShader.Set(null);
            deviceContext.PixelShader.Set(pixelShader);
            deviceContext.PixelShader.SetShaderResource(0, inputRV);
            if (constantBuffer != null)
                deviceContext.PixelShader.SetConstantBuffer(0, constantBuffer);
            deviceContext.Draw(4, 0);
            RenderTargetView nullRTV = null;
            deviceContext.OutputMerger.SetTargets(nullRTV);
            deviceContext.PixelShader.SetShaderResource(0, null);
        }

        VertexShader vertexShader;
        PixelShader pixelShader; 
        RasterizerState rasterizerState;
        public Viewport viewport;
        VertexBufferBinding vertexBufferBinding;
        protected Buffer constantBuffer;
    }

    public class FromUIntPS : FilterPixelShader
    {
        public FromUIntPS(Device device, int imageWidth, int imageHeight)
            : base(device, imageWidth, imageHeight, 0, "Content/FromUIntPS.cso")
        {
        }
    }


    public class PassThrough : FilterPixelShader
    {
        //TODO: maybe just put sampler in base class
        public PassThrough(Device device, int imageWidth, int imageHeight)
            : base(device, imageWidth, imageHeight, 0, "Content/PassThroughPS.cso")
        {
            var samplerStateDesc = new SamplerStateDescription()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Border,
                //BorderColor = new SharpDX.Color4(0.5f, 0.5f, 0.5f, 1.0f),
                BorderColor = new SharpDX.Color4(0, 0, 0, 1.0f),
            };
            samplerState = new SamplerState(device, samplerStateDesc);
        }
        public override void Render(DeviceContext deviceContext, ShaderResourceView inputRV, RenderTargetView renderTargetView)
        {
            deviceContext.PixelShader.SetSampler(0, samplerState);

            base.Render(deviceContext, inputRV, renderTargetView);
        }
 
        SamplerState samplerState;
    }

    public class RadialWobble : FilterPixelShader
    {
        public RadialWobble(Device device, int imageWidth, int imageHeight)
            : base(device, imageWidth, imageHeight, constantBufferSize, "Content/RadialWobblePS.cso")
        {
            var samplerStateDesc = new SamplerStateDescription()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Border,
                AddressV = TextureAddressMode.Border,
                AddressW = TextureAddressMode.Border,
                //BorderColor = new SharpDX.Color4(0.5f, 0.5f, 0.5f, 1.0f),
                BorderColor = new SharpDX.Color4(0, 0, 0, 1.0f),
            };
            samplerState = new SamplerState(device, samplerStateDesc);

            SetConstants(device.ImmediateContext, 0);
        }
        public override void Render(DeviceContext deviceContext, ShaderResourceView inputRV, RenderTargetView renderTargetView)
        {
            deviceContext.PixelShader.SetSampler(0, samplerState);

            base.Render(deviceContext, inputRV, renderTargetView);
        }
 
        SamplerState samplerState;

        const int constantBufferSize = 16; // must be multiple of 16

        public void SetConstants(DeviceContext deviceContext, float newAlpha)
        {
            Constants constants = new Constants()
            {
                alpha = newAlpha,
            };

            DataStream dataStream;
            deviceContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, MapFlags.None, out dataStream);
            dataStream.Write<Constants>(constants);
            deviceContext.UnmapSubresource(constantBuffer, 0);
        }


        // protip: compile shader with /Fc; output gives exact layout
        // hlsl matrices are stored column major
        // variables are stored on 4-component boundaries; inc. matrix columns
        // size is a multiple of 16
        [StructLayout(LayoutKind.Explicit, Size = constantBufferSize)]
        public unsafe struct Constants
        {
            [FieldOffset(0)]
            public float alpha;
        };
    }



    public class BilateralFilter : FilterPixelShader
    {
        public BilateralFilter(Device device, int imageWidth, int imageHeight)
            : base(device, imageWidth, imageHeight, constantBufferSize, "Content/BilateralFilterPS.cso")
        {
            SetConstants(device.ImmediateContext, 4f, 100f);
        }

        public void SetConstants(DeviceContext deviceContext, float newSpatialSigma, float newIntensitySigma)
        {
            Constants constants = new Constants()
            {
                spatialSigma = 1f/newSpatialSigma,
                intensitySigma = 1f/newIntensitySigma,
            };

            DataStream dataStream;
            deviceContext.MapSubresource(constantBuffer, MapMode.WriteDiscard, MapFlags.None, out dataStream);
            dataStream.Write<Constants>(constants);
            deviceContext.UnmapSubresource(constantBuffer, 0);
        }

        const int constantBufferSize = 16; // must be multiple of 16

        // protip: compile shader with /Fc; output gives exact layout
        // hlsl matrices are stored column major
        // variables are stored on 4-component boundaries; inc. matrix columns
        // size is a multiple of 16
        [StructLayout(LayoutKind.Explicit, Size = constantBufferSize)]
        public unsafe struct Constants
        {
            [FieldOffset(0)]
            public float spatialSigma;
            [FieldOffset(4)]
            public float intensitySigma;
        };
    }

}
