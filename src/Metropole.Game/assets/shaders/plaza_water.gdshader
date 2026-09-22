shader_type spatial;
// Original 1.8 plaza water. Opaque, no screen texture, no extra render pass.
uniform vec4 water_color : source_color = vec4(0.06, 0.36, 0.43, 1.0);
void fragment() {
    vec2 p = UV * 2.0 - 1.0;
    float ripple = sin(length(p) * 48.0 - TIME * 3.5);
    ALBEDO = water_color.rgb + vec3(0.025, 0.045, 0.05) * ripple;
    METALLIC = 0.15;
    ROUGHNESS = 0.24 + ripple * 0.035;
}
