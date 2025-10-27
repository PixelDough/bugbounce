shader_type spatial;
render_mode unshaded;
render_mode blend_add;

uniform sampler2D baseMap : source_color;
uniform float fadeScale = 1.0;
uniform float yOffset = 0.0;

varying vec3 vertex_pos;

void vertex() {
	COLOR = COLOR;
	vertex_pos = VERTEX;
}

void fragment() {
	ALBEDO = texture(baseMap, UV).rgb * COLOR.rgb;
	
	float distance_to_origin = (vertex_pos.y - NODE_POSITION_WORLD.y) - yOffset;
	ALPHA = clamp(1.0 - (distance_to_origin / fadeScale), 0.0, 1.0);
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
