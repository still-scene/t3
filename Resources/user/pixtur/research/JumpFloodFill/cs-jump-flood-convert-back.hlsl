Texture2D<uint> DistanceField : register(t0);
RWTexture2D<float4> OutputBuffer : register(u0);

cbuffer ConstantsPS : register(b0)
{
    float2 resolution;
};

struct PSInput
{
    float4 position : SV_POSITION;
    float2 texcoord : TEXCOORD0;
};

float3 map(uint2 iXY, float2 p)
{
    uint b = DistanceField.Load(int3(iXY, 0));
    uint x = b & 0x7FFFu;
    uint y = b >> 16;
    uint s = b & 0x8000u;

    //float d = length(p - float2(x, y));
    //return float3((s == 0u) ? d : -d, float(x), float(y));
    return float3(x,y,0);
}

[numthreads(16, 16, 1)]
void main(uint3 DTid : SV_DispatchThreadID)
{
    int2 p_pixl = int2(DTid.xy);
    if (p_pixl.x >= resolution.x || p_pixl.y >= resolution.y)
        return;


    float2 p = p_pixl/resolution;
    float3 dce = map(p_pixl, p);

    OutputBuffer[uint2(p_pixl)] = float4(dce, 1.0f);
}
