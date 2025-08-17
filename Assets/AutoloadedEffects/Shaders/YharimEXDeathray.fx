﻿sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);

float globalTime;
float3 mainColor;
matrix uWorldViewProjection;

// These 3 are required if using primitives. They are the same for any shader being applied to them
// so you can copy paste them to any other prim shaders and use the VertexShaderOutput input in the
// PixelShaderFunction.
// -->
struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}
// <--

// The X coordinate is the trail completion, the Y coordinate is the same as any other.
// This is simply how the primitive TextCoord is layed out in the C# code.
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    // This can also be copy pasted along with the above.
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
	coords.y = (coords.y - 0.5) / input.TextureCoordinates.z + 0.5;
    
    if (coords.x < 0.05)
        coords.y /= pow(coords.x / 0.05, 0.4);
    
    // Get the pixel of the fade map. What coords.x is being multiplied by determines
    // how many times the uImage1 is copied to cover the entirety of the prim. 4, 4.6
	float4 fadeMapColor = tex2D(uImage1, float2(frac(coords.x * 2 - globalTime * 4.6), coords.y));
    
    // Use the red value for the opacity, as the provided image *should* be grayscale.
    float opacity = fadeMapColor.r;
    // Lerp between the base color, and the provided color based on the opacity of the fademap.
    float4 colorCorrected = lerp(color, float4(mainColor, 1), fadeMapColor.r);
    
    // Fade out at the top and bottom of the streak.
    if (coords.y < 0.35)
        opacity *= pow(coords.y / 0.35, 6);
    if (coords.y > 0.65)
        opacity *= pow(1 - (coords.y - 0.65) / 0.8, 6);
    
    //// Fade out at the top and bottom of the streak.
    if (coords.x < 0.1)
        opacity *= pow(coords.x / 0.1, 6);
    if (coords.x > 0.95)
        opacity *= pow(1 - (coords.x - 0.95) / 0.05, 6);
    
    return colorCorrected * pow(opacity, 0.5);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}