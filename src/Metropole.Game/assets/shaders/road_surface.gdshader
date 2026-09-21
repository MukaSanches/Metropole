shader_type spatial;
render_mode diffuse_burley, specular_schlick_ggx, cull_back;

uniform vec3 dry_color : source_color = vec3(0.050, 0.060, 0.070);
uniform vec3 wet_color : source_color = vec3(0.018, 0.028, 0.038);
uniform float wetness : hint_range(0.0, 1.0) = 0.0;
uniform float night_factor : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    float w = smoothstep(0.0, 1.0, wetness);
    ALBEDO = mix(dry_color, wet_color, w);
    ROUGHNESS = mix(0.90, 0.22, w);
    METALLIC = mix(0.015, 0.10, w);
    SPECULAR = mix(0.32, 0.68, w);
    ALBEDO *= mix(1.0, 0.82, night_factor);
}
