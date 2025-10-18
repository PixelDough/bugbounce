shader_type spatial;
render_mode cull_disabled;
render_mode depth_draw_opaque;
render_mode shadows_disabled;

#include "shading.gdshaderinc"

varying vec3 v_world_normal;
varying vec3 v_world_pos;
varying vec3 v_eye_pos;

group_uniforms Color;
uniform sampler2D albedo : hint_default_white, source_color;
uniform vec3 tint : source_color = vec3(1,1,1);
uniform float vertex_color_intensity : hint_range(0.0, 1.0) = 1.0;

group_uniforms NormalMap;
uniform sampler2D normal_tex : hint_normal, filter_linear;

group_uniforms Matcap;
uniform sampler2D matcap_tex : hint_default_transparent, source_color, filter_linear;
uniform float matcap_intensity = 1.0;
uniform float matcap_power = 1.0;
uniform float smoothness : hint_range(0, 1) = 0.0;
uniform float metallic : hint_range(0, 1) = 0.0;

group_uniforms Lighting;
uniform sampler2D main_light_gradient : hint_default_white, source_color, repeat_disable;

varying vec2 matcap_uv;

void vertex() {
	v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_eye_pos = (INV_VIEW_MATRIX * vec4(EYE_OFFSET, 1.0)).xyz;
	vec4 worldPos = (MODEL_MATRIX * vec4(VERTEX, 1.0));
	v_world_pos = worldPos.xyz;
	
	matcap_uv = generate_matcap_uv_perspective_correct(
		VERTEX, NORMAL, MODEL_MATRIX, VIEW_MATRIX, INV_VIEW_MATRIX, MODELVIEW_MATRIX
	);
}

void fragment() {
	vec3 albedo_sample = texture(albedo, UV).rgb;
	vec3 vert_color = mix(vec3(1.0), COLOR.rgb, vertex_color_intensity);
	vec3 final_color = albedo_sample * vert_color * tint;

	vec3 normal_sample = texture(normal_tex, UV).rgb;
	vec3 normal_adjusted = mix(normal_sample, vec3(0.5, 0.5, 1.0), 0.25);
	NORMAL_MAP = normal_adjusted;
	
	vec3 albedo_with_matcap = apply_matcap(
		final_color,
		matcap_tex,
		matcap_uv,
		matcap_intensity,
		matcap_power,
		smoothness,
		metallic
	);
	
	ALBEDO = albedo_with_matcap;
	
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
	
	DIFFUSE_LIGHT = main_light_color * ATTENUATION;
	SPECULAR_LIGHT = vec3(0);
}
