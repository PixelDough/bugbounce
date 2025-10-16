shader_type spatial;
render_mode unshaded;

#include "shading.gdshaderinc"

uniform sampler2D color_gradient : source_color, repeat_disable;
uniform sampler2D matcap_tex : hint_default_transparent, source_color, filter_linear;

varying vec3 v_world_normal;
varying vec3 v_world_pos;
varying vec3 v_eye_pos;

float fresnel(float amount, vec3 normal, vec3 view)
{
	return pow((1.0 - clamp(dot(normalize(normal), normalize(view)), 0.0, 1.0 )), amount);
}

void vertex() {
	v_world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	v_eye_pos = (INV_VIEW_MATRIX * vec4(EYE_OFFSET, 1.0)).xyz;
	vec4 worldPos = (MODEL_MATRIX * vec4(VERTEX, 1.0));
	v_world_pos = worldPos.xyz;
}

void fragment() {
	float fresnel_value = fresnel(1.0, NORMAL, VIEW);
	vec3 color = texture(color_gradient, vec2(fresnel_value, 0.0)).rgb;
	vec3 albedo_with_matcap = apply_matcap_classic(
		color,
		matcap_tex,
		2.0,
		2.0,
		1.0,
		0.0,
		vec3(0.5, 0.5, 1.0),
		TANGENT,
		BINORMAL,
		NORMAL,
		NODE_POSITION_VIEW
	);
	ALBEDO = albedo_with_matcap * 2.0;
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
