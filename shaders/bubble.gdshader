shader_type spatial;
render_mode unshaded;
render_mode depth_draw_always;

#include "shading.gdshaderinc"

uniform sampler2D color_gradient : source_color, repeat_disable;
uniform sampler2D matcap_tex : hint_default_transparent, source_color, filter_linear;

varying vec3 v_world_normal;
varying vec3 v_world_pos;
varying vec3 v_eye_pos;
varying vec2 matcap_uv;

void vertex() {
	matcap_uv = generate_matcap_uv_perspective_correct(
		VERTEX, NORMAL, MODEL_MATRIX, VIEW_MATRIX, INV_VIEW_MATRIX, MODELVIEW_MATRIX
	);
}

void fragment() {
	float fresnel_value = fresnel(1.0, NORMAL, VIEW);
	vec3 color = texture(color_gradient, vec2(fresnel_value, 0.0)).rgb * 1.5;
	
	vec3 albedo_with_matcap = apply_matcap(
		color,
		matcap_tex,
		matcap_uv,
		2.0,
		1.0,
		1.0,
		0.0
	);
	
	ALPHA = mix(fresnel_value * fresnel_value, 1.0, 0.7);
	
	ALBEDO = albedo_with_matcap;
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
