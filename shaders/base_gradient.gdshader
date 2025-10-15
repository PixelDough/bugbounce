shader_type spatial;
render_mode cull_disabled;
render_mode depth_draw_opaque;

uniform sampler2D albedo : hint_default_white, source_color;
uniform sampler2D main_light_gradient : hint_default_white, repeat_disable;
uniform vec3 tint : source_color = vec3(1,1,1);
uniform float vertex_color_intensity : hint_range(0.0, 1.0) = 1.0;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec3 albedo_sample = texture(albedo, UV).rgb;
	vec3 vert_color = mix(vec3(1.0), COLOR.rgb, vertex_color_intensity);
	ALBEDO = albedo_sample * vert_color * tint;
	
	ROUGHNESS = 1.0;
	METALLIC = 0.0;
}

void light() {
	float light_dot = dot(NORMAL, LIGHT);
	float light_dot_normalized = (light_dot + 1.0) * 0.5;
	vec3 main_light_color = texture(
		main_light_gradient,
		vec2(light_dot_normalized, 0)
	).rgb;
	
	DIFFUSE_LIGHT = main_light_color * ATTENUATION * ALBEDO;
	SPECULAR_LIGHT = vec3(0);
}
