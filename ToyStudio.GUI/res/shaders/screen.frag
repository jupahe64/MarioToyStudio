#version 330

in vec2 TexCoords;

uniform sampler2D uSceneColor;
uniform sampler2D uOutline;
uniform sampler2D uHighlight;
uniform sampler2D uSceneDepth;
uniform sampler2D uHighlightDepth;

out vec4 FragColor;

const float GAMMA = 2.2;

void main()
{
    FragColor.rgb = texture(uSceneColor, vec2(TexCoords.x, 1.0 - TexCoords.y)).rgb;
    FragColor.a = 1.0;

    FragColor.rgb = pow(FragColor.rgb, vec3(1.0/GAMMA));


    vec4 outline = texture(uOutline, vec2(TexCoords.x, 1.0 - TexCoords.y));
    vec4 highlight = texture(uHighlight, vec2(TexCoords.x, 1.0 - TexCoords.y));
    float sceneDepth = texture(uSceneDepth, vec2(TexCoords.x, 1.0 - TexCoords.y)).r;
    float highlightDepth = texture(uHighlightDepth, vec2(TexCoords.x, 1.0 - TexCoords.y)).r;

    FragColor.rgb = mix(FragColor.rgb, highlight.rgb, highlight.a * float(highlightDepth == sceneDepth));

    FragColor.rgb = mix(FragColor.rgb, outline.rgb, outline.a);
}