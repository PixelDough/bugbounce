shader_type spatial;
render_mode cull_disabled;
render_mode depth_draw_opaque;
render_mode shadows_disabled;
render_mode diffuse_toon;

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

float get_unpacked_normal_z(vec3 val) {
	return sqrt((1.0 - (val.x * val.x)) - (val.y * val.y));
}

vec3 unpack_normal_map(vec4 rgba) {
     //https://youtu.be/-dMbacvrDrs?si=q68d93F2_R61e6Bs
    vec3 n = rgba.xzy * vec3(2.0) - vec3(1.0);
    n.z *= -1.0;
    return n;
}

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
	//vec3 normal_adjusted = mix(normal_sample, vec3(0.5, 0.5, 1.0), 0.25);
	NORMAL_MAP = normal_sample;
	
	vec3 normal_unpacked = normal_sample * 2.0 - 1.0;
	normal_unpacked.z = get_unpacked_normal_z(normal_unpacked);
	
	mat3 view_to_tangent = mat3(TANGENT, BINORMAL, NORMAL);
	mat3 tangent_to_view = inverse(view_to_tangent);
	
	vec4 albedo_with_matcap = apply_matcap_basic(
		vec4(final_color, 1.0),
		texture(matcap_tex, generate_matcap_uvs_basic(view_to_tangent * normal_unpacked)),
		matcap_intensity,
		matcap_power,
		smoothness,
		metallic
	);
	
	ALBEDO = albedo_with_matcap.rgb;
	
	ROUGHNESS = 0.0;
	METALLIC = 0.0;
}

void light() {
	float light_dot = dot(NORMAL, LIGHT);
	float light_dot_normalized = (light_dot + 1.0) * 0.5;
	vec3 main_light_color = texture(
		main_light_gradient,
		vec2(light_dot_normalized, 0)
	).rgb;
	
	DIFFUSE_LIGHT += main_light_color * ATTENUATION * LIGHT_COLOR / PI;
	
	float rim_glow_dot = dot(NORMAL, LIGHT);
	float rim_amount = 1.0 - ((rim_glow_dot + 1.0) * 0.5);
	rim_amount *= pow(smoothness, 0.5);
	vec3 rim_color = texture(
		main_light_gradient,
		vec2(pow(rim_amount, 1.0) * fresnel(0.6, NORMAL, VIEW), 0.0)
	).rgb;
	DIFFUSE_LIGHT += rim_color * ATTENUATION * LIGHT_COLOR / PI;
	SPECULAR_LIGHT = vec3(0);
}
