shader_type spatial;
render_mode cull_back, diffuse_burley, specular_schlick_ggx;

uniform float night_factor : hint_range(0.0, 1.0) = 0.0;
uniform float wetness : hint_range(0.0, 1.0) = 0.0;
uniform float glassiness : hint_range(0.0, 1.0) = 0.35;
uniform float window_columns : hint_range(3.0, 12.0) = 6.0;
uniform float window_rows : hint_range(4.0, 18.0) = 10.0;
uniform vec3 window_day = vec3(0.12, 0.28, 0.38);
uniform vec3 window_night = vec3(1.00, 0.72, 0.42);

varying vec4 instance_data;

void vertex() {
    instance_data = INSTANCE_CUSTOM;
}

float checker(vec2 cell) {
    return step(0.5, fract(dot(cell, vec2(0.75487766, 0.56984029))));
}

void fragment() {
    vec3 base_tint = max(instance_data.rgb, vec3(0.08));
    vec2 tiled = UV * vec2(window_columns, window_rows);
    vec2 cell_uv = fract(tiled);
    vec2 cell_id = floor(tiled);

    float inset_x = step(0.14, cell_uv.x) * step(cell_uv.x, 0.86);
    float inset_y = step(0.14, cell_uv.y) * step(cell_uv.y, 0.82);
    float window_mask = inset_x * inset_y;

    float horizontal_frame = 1.0 - step(0.08, min(abs(cell_uv.y - 0.14), abs(cell_uv.y - 0.82)));
    float vertical_frame = 1.0 - step(0.08, min(abs(cell_uv.x - 0.14), abs(cell_uv.x - 0.86)));
    float frame = clamp(horizontal_frame + vertical_frame, 0.0, 1.0);

    float upward = abs(NORMAL.y);
    float roof_mask = smoothstep(0.55, 0.88, upward);
    float lit_seed = checker(cell_id + floor(instance_data.a * 37.0));
    float lit = window_mask * lit_seed * night_factor * (1.0 - roof_mask);

    vec3 wall = base_tint * mix(0.56, 0.82, glassiness);
    vec3 glass = mix(window_day, base_tint * 0.65, 0.24);
    vec3 facade = mix(wall, glass, window_mask * (0.60 + glassiness * 0.34));
    facade = mix(facade, base_tint * 0.42, frame * 0.72);
    facade = mix(facade, base_tint * 0.34, roof_mask);

    ALBEDO = facade;
    METALLIC = mix(0.03, 0.52, window_mask * glassiness);
    ROUGHNESS = mix(0.62, 0.16, window_mask * glassiness);
    ROUGHNESS = mix(ROUGHNESS, 0.09, wetness * 0.65);
    EMISSION = window_night * lit * (1.20 + glassiness * 1.15);
}
