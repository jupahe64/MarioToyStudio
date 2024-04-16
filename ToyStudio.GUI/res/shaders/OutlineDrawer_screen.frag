#version 330

in vec2 TexCoords;

uniform sampler2D uColor;
uniform usampler2D uId;
uniform sampler2D uDepth;
uniform sampler2D uSceneDepth;

out vec4 FragColor;

void main()
{
    ivec2 pos0 = ivec2(gl_FragCoord.xy);
    ivec2 pos1 = pos0 + ivec2(-2, 0);
    ivec2 pos2 = pos0 + ivec2( 0,-2);
    ivec2 pos3 = pos0 + ivec2( 2, 0);
    ivec2 pos4 = pos0 + ivec2( 0, 2);

    uint i0 = texelFetch(uId, pos0, 0).r;
    uint i1 = texelFetch(uId, pos1, 0).r;
    uint i2 = texelFetch(uId, pos2, 0).r;
    uint i3 = texelFetch(uId, pos3, 0).r;
    uint i4 = texelFetch(uId, pos4, 0).r;

    float d0 = texelFetch(uDepth, pos0, 0).r;
    float d1 = texelFetch(uDepth, pos1, 0).r;
    float d2 = texelFetch(uDepth, pos2, 0).r;
    float d3 = texelFetch(uDepth, pos3, 0).r;
    float d4 = texelFetch(uDepth, pos4, 0).r;

    //closest position to the camera (x, y, depth)
    vec3 cPos = vec3(vec2(pos0), d0);
    cPos = d1 < cPos.z ? vec3(vec2(pos1), d1) : cPos;
    cPos = d2 < cPos.z ? vec3(vec2(pos2), d2) : cPos;
    cPos = d3 < cPos.z ? vec3(vec2(pos3), d3) : cPos;
    cPos = d4 < cPos.z ? vec3(vec2(pos4), d4) : cPos;

    ivec2 pos = i0==0u ? ivec2(cPos.xy) : pos0;

    vec4 color = texelFetch(uColor, pos, 0);
    float sceneDepth = texelFetch(uSceneDepth, pos, 0).r;

    bool isEdge = 
        i0!=i1 ||
        i0!=i2 ||
        i0!=i3 ||
        i0!=i4;

    float alpha = cPos.z <= sceneDepth ? 1.0 : 0.5;

    FragColor = isEdge ? vec4(color.rgb, alpha) : vec4(0.0);
}