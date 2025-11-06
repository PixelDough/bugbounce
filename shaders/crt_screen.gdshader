shader_type spatial;
render_mode unshaded;

uniform sampler2DArray background_texture : source_color;
uniform sampler2D screen_texture : source_color;

instance uniform int thumbnail_index = 0;

void fragment() {
	vec3 bg_color = texture(background_texture, vec3(UV, float(thumbnail_index))).rgb;
	vec4 viewport_color = texture(screen_texture, UV);
	
	ALBEDO = mix(bg_color, viewport_color.rgb, viewport_color.a);
}
