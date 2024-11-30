using System;
using System.Drawing;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;

public class CubeApp : Form
{
    private Device device;
    private SwapChain swapChain;
    private RenderTargetView renderView;
    private DeviceContext context;
    private Buffer vertexBuffer;
    private Buffer indexBuffer;
    private Buffer constantBuffer;
    private VertexShader vertexShader;
    private PixelShader pixelShader;
    private InputLayout inputLayout;
    private Matrix view;
    private Matrix projection;
    private float angle;

    public CubeApp()
    {
        this.Text = "Rotating 3D Cube";
        this.ClientSize = new Size(800, 600);

        InitializeDevice();
        InitializeCube();
        InitializeMatrices();
        InitializeShaders();

        Application.Idle += (sender, args) => Render();
    }

    private void InitializeDevice()
    {
        var swapChainDesc = new SwapChainDescription
        {
            BufferCount = 1,
            ModeDescription = new ModeDescription(800, 600, new Rational(60, 1), Format.R8G8B8A8_UNorm),
            IsWindowed = true,
            OutputHandle = this.Handle,
            SampleDescription = new SampleDescription(1, 0),
            SwapEffect = SwapEffect.Discard,
            Usage = Usage.RenderTargetOutput
        };

        Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware, DeviceCreationFlags.None, swapChainDesc, out device, out swapChain);
        context = device.ImmediateContext;

        using (var backBuffer = swapChain.GetBackBuffer<SharpDX.Direct3D11.Texture2D>(0))
        {
            renderView = new RenderTargetView(device, backBuffer);
        }

        context.OutputMerger.SetRenderTargets(renderView);
    }

    private void InitializeCube()
    {
        var vertices = new[]
        {
            new Vector3(-1, -1, -1),
            new Vector3(-1,  1, -1),
            new Vector3( 1,  1, -1),
            new Vector3( 1, -1, -1),
            new Vector3(-1, -1,  1),
            new Vector3(-1,  1,  1),
            new Vector3( 1,  1,  1),
            new Vector3( 1, -1,  1)
        };

        vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices);

        var indices = new ushort[]
        {
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,
            2, 3, 7, 2, 7, 6,
            1, 2, 6, 1, 6, 5,
            3, 0, 4, 3, 4, 7
        };

        indexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, indices);
    }

    private void InitializeMatrices()
    {
        view = Matrix.LookAtLH(new Vector3(0, 0, -5), Vector3.Zero, Vector3.UnitY);
        projection = Matrix.PerspectiveFovLH((float)Math.PI / 4, this.ClientSize.Width / (float)this.ClientSize.Height, 0.1f, 100.0f);
    }

    private void InitializeShaders()
    {
        var vertexShaderByteCode = ShaderBytecode.CompileFromFile("VertexShader.hlsl", "main", "vs_5_0");
        vertexShader = new VertexShader(device, vertexShaderByteCode);

        var pixelShaderByteCode = ShaderBytecode.CompileFromFile("PixelShader.hlsl", "main", "ps_5_0");
        pixelShader = new PixelShader(device, pixelShaderByteCode);

        var inputElements = new[]
        {
            new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0)
        };

        inputLayout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), inputElements);
        context.InputAssembler.InputLayout = inputLayout;

        constantBuffer = new Buffer(device, Utilities.SizeOf<Matrix>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
        context.VertexShader.SetConstantBuffer(0, constantBuffer);

        context.VertexShader.Set(vertexShader);
        context.PixelShader.Set(pixelShader);
    }

    private void Render()
    {
        angle += 0.01f;
        var world = Matrix.RotationY(angle);
        var wvp = world * view * projection;

        context.UpdateSubresource(ref wvp, constantBuffer);

        context.ClearRenderTargetView(renderView, new RawColor4(0, 0, 0, 1));

        context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
        context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vector3>(), 0));
        context.InputAssembler.SetIndexBuffer(indexBuffer, Format.R16_UInt, 0);

        context.DrawIndexed(36, 0, 0);

        swapChain.Present(1, PresentFlags.None);
    }

    protected override void OnClosed(EventArgs e)
    {
        renderView.Dispose();
        vertexBuffer.Dispose();
        indexBuffer.Dispose();
        constantBuffer.Dispose();
        inputLayout.Dispose();
        vertexShader.Dispose();
        pixelShader.Dispose();
        swapChain.Dispose();
        device.Dispose();

        base.OnClosed(e);
    }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new CubeApp());
    }
}
