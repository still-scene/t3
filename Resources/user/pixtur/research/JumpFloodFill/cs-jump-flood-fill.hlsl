cbuffer Constants : register(b0)
{
    int frame;
    int numPasses;
    int2 resolution;
};

Texture2D<uint> InputBuffer : register(t0);
RWTexture2D<uint> OutputBuffer : register(u0);

// Functions to pack and unpack data
uint pack(int2 p, uint s)
{
    return (uint(p.x) & 0x7FFFu) | (s & 0x8000u) | (uint(p.y) << 16);
}

int4 unpack(uint d)
{
    uint x = d & 0x7FFFu;
    uint y = d >> 16;
    uint s = d & 0x8000u;
    uint r = (y == 0xFFFFu) ? 0u : 1u;
    return int4(x, y, s, r);
}

uint init(float d)
{
    return (d < 0.5f) ? 0xFFFFFFFFu : 0xFFFF7FFFu;
}

float dot2(int2 x)
{
    float2 xf = float2(x);
    return dot(xf, xf);
}

int computeNumPasses(int2 resolution)
{
    int dim = max(resolution.x, resolution.y);
    dim = max(dim - 1, 0);
    int r = 0;
    if (dim > 0xFFFF) { dim >>= 16; r |= 16; }
    if (dim > 0x00FF) { dim >>= 8; r |= 8; }
    if (dim > 0x000F) { dim >>= 4; r |= 4; }
    if (dim > 0x0003) { dim >>= 2; r |= 2; }
    if (dim > 0x0001) { dim >>= 1; r |= 1; }
    return r + 1;
}

float shape(float2 p)
{
    // Mandelbrot set
    float2 z = float2(0.0f, 0.0f);
    float2 c = p - float2(0.75f, 0.0f);
    for (int i = 0; i < 15; i++)
    {
        z = float2(z.x * z.x - z.y * z.y, 2.0f * z.x * z.y) + c;
        if (dot(z, z) > 4.0f)
            return 1.0f;
    }
    return 0.0f;
}

[numthreads(16, 16, 1)]
void main(uint3 DTid : SV_DispatchThreadID)
{
    int2 p_pixl = int2(DTid.xy);
    if (p_pixl.x >= resolution.x || p_pixl.y >= resolution.y)
        return;

    if (frame == 0)
    {
        float2 fragCoord = float2(p_pixl);
        float2 p = (2.0f * fragCoord - float2(resolution)) / resolution.y;
        float d = shape(p);

        uint data = init(d);
        OutputBuffer[uint2(p_pixl)] = data;
    }
    else if ((frame >= 1) && (frame <= numPasses))
    {
        uint p_data_packed = InputBuffer.Load(int3(p_pixl, 0));
        int4 p_data = unpack(p_data_packed);

        float currdis = (p_data.w == 1) ? dot2(p_pixl - p_data.xy) : 1e20f;

        int width = (1 << (numPasses - frame));

        int minx = (p_pixl.x - width < 0) ? 0 : -1;
        int miny = (p_pixl.y - width < 0) ? 0 : -1;
        int maxx = (p_pixl.x + width > resolution.x - 1) ? 0 : 1;
        int maxy = (p_pixl.y + width > resolution.y - 1) ? 0 : 1;

        for (int y = miny; y <= maxy; y++)
        {
            for (int x = minx; x <= maxx; x++)
            {
                int2 q_offs = int2(x, y) * width;
                int2 q_pixl = p_pixl + q_offs;

                // Boundary check
                if (q_pixl.x < 0 || q_pixl.y < 0 || q_pixl.x >= resolution.x || q_pixl.y >= resolution.y)
                    continue;

                uint q_data_packed = InputBuffer.Load(int3(q_pixl, 0));
                int4 q_data = unpack(q_data_packed);

                if (q_data.z != p_data.z)
                {
                    float dis = dot2(q_offs);
                    if (dis < currdis)
                    {
                        currdis = dis;
                        p_data_packed = pack(q_pixl, p_data.z);
                    }
                }
                else if (q_data.w == 1)
                {
                    float dis = dot2(q_data.xy - p_pixl);
                    if (dis < currdis)
                    {
                        currdis = dis;
                        p_data_packed = pack(q_data.xy, p_data.z);
                    }
                }
            }
        }

        OutputBuffer[uint2(p_pixl)] = p_data_packed;
    }
    else if (frame == numPasses + 1)
    {
        uint data = InputBuffer.Load(int3(p_pixl, 0));
        OutputBuffer[uint2(p_pixl)] = data;
    }
}