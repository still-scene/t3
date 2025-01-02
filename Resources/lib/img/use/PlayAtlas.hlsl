cbuffer ParamConstants : register(b0)
{
    float CountX;
    float CountY;
    float Truncate;
    float Phase;
}

cbuffer Resolution : register(b1)
{
    float TargetWidth;
    float TargetHeight;
}

struct vsOutput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD;
};

Texture2D<float4> Image : register(t0);
sampler texSampler : register(s0);

float4 psMain(vsOutput input) : SV_TARGET
{
    float width, height;
    Image.GetDimensions(width, height);

    // Calculate total frames, clamping to prevent issues
    float totalFrames = max(CountX * CountY - Truncate, 1);
    
    // Calculate the current frame based on Phase
    float frame = floor(Phase * totalFrames);
    
    // Calculate the row and column of the current frame
    float row = floor(frame / CountX);
    float col = frame % CountX;

    // Calculate the UV offset for the current frame
    float2 frameUV = float2(col / CountX, row / CountY);
    
    // Scale the UV coordinates to the size of each frame
    float2 frameSize = float2(1.0 / CountX, 1.0 / CountY);
    float2 uv = frameUV + input.texCoord * frameSize;

    // Sample the image at the adjusted UV coordinates
    float4 color = Image.Sample(texSampler, uv);

    return color;
}
