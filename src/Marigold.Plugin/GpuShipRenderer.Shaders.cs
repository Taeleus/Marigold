namespace Marigold
{
    internal sealed partial class GpuShipRenderer
    {
        private const string ShaderSource = @"
cbuffer Preview : register(b0)
{
    float4 CameraRight;
    float4 CameraUp;
    float4 CameraNear;
    float4 CenterInverseRadius;
    float4 Settings; // Zoom, ship opacity, hull depth tolerance, depth scale.
};

Texture2D<float4> ColorMap : register(t0);
Texture2D<float> HullDepth : register(t1);
SamplerState MaterialSampler : register(s0);

float3 ViewPosition(float3 p)
{
    return float3(dot(p, CameraRight.xyz), dot(p, CameraUp.xyz), dot(p, CameraNear.xyz));
}

float4 ClipPosition(float3 p)
{
    return float4(p.xy * Settings.x, .5 - Settings.w * p.z, 1);
}

struct ModelInput
{
    float3 Position : POSITION;
    float2 Uv : TEXCOORD0;
    float4 TransformX : INSTANCE0;
    float4 TransformY : INSTANCE1;
    float4 TransformZ : INSTANCE2;
    float4 UvTransform : INSTANCE3;
    float4 Color : INSTANCE4;
};

struct ModelPixel
{
    float4 Position : SV_Position;
    float2 Uv : TEXCOORD0;
    float3 View : TEXCOORD1;
    nointerpolation float4 Color : COLOR0;
};

ModelPixel ModelVertex(ModelInput input)
{
    ModelPixel output;
    float4 p = float4(input.Position, 1);
    float3 world = float3(dot(p, input.TransformX), dot(p, input.TransformY), dot(p, input.TransformZ));
    output.View = ViewPosition((world - CenterInverseRadius.xyz) * CenterInverseRadius.w);
    output.Position = ClipPosition(output.View);
    output.Uv = input.Uv * input.UvTransform.z + input.UvTransform.xy;
    output.Color = input.Color;
    return output;
}

float Light(float3 p)
{
    float3 n = cross(ddx(p), ddy(p));
    return .60 + .40 * abs(n.z) * rsqrt(max(dot(n, n), 1.e-18));
}

float4 PaintPixel(ModelPixel input) : SV_Target
{
    float alpha = input.Color.a * Settings.y;
    return float4(input.Color.rgb * Light(input.View) * alpha, alpha);
}

float4 TexturedPixel(ModelPixel input) : SV_Target
{
    float alpha = input.Color.a * Settings.y;
    float3 tex = ColorMap.Sample(MaterialSampler, input.Uv).rgb;
    return float4(tex * (.25 + .75 * input.Color.rgb) * Light(input.View) * alpha, alpha);
}

struct OverlayInput
{
    float3 Position : POSITION;
    float4 Color : COLOR0;
    float DepthAware : TEXCOORD0;
};

struct OverlayPixel
{
    float4 Position : SV_Position;
    nointerpolation float4 Color : COLOR0;
    nointerpolation float DepthAware : TEXCOORD0;
};

float3 LinearColor(float3 srgb)
{
    return lerp(srgb / 12.92, pow((srgb + .055) / 1.055, 2.4), step(.04045, srgb));
}

OverlayPixel OverlayVertex(OverlayInput input)
{
    OverlayPixel output;
    output.Position = ClipPosition(ViewPosition(input.Position));
    output.Color = float4(LinearColor(input.Color.rgb), input.Color.a);
    output.DepthAware = input.DepthAware;
    return output;
}

float4 OverlayColor(OverlayPixel input, float behind)
{
    float alpha = input.Color.a;
    if (input.DepthAware > .5 && Settings.y > 0)
    {
        float hull = HullDepth.Load(int3(int2(input.Position.xy), 0));
        if (input.DepthAware > 1.5 && hull >= 1)
            discard;
        if (input.Position.z > hull + Settings.z)
            alpha *= behind;
    }
    return float4(input.Color.rgb * alpha, alpha);
}

float4 FillPixel(OverlayPixel input) : SV_Target { return OverlayColor(input, .5); }
float4 LinePixel(OverlayPixel input) : SV_Target { return OverlayColor(input, .4); }
";
    }
}
