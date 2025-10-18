shader_type spatial;
render_mode unshaded;
render_mode cull_disabled;
render_mode depth_test_default;
render_mode depth_prepass_alpha;
render_mode fog_disabled;

uniform sampler2D depth_texture : hint_depth_texture;
uniform float power = 1.0;

void vertex() {
	// billboard
	//VERTEX = mat3(inverse(MODELVIEW_MATRIX)) * VERTEX;
	
	VERTEX += mat3(inverse(MODEL_MATRIX)) * CAMERA_POSITION_WORLD;
}

void fragment() {
	float depth_sample = texture(depth_texture, SCREEN_UV).x; //DEPTH TEXTURE THAT STORES SCREEN DEPTH
	float fragment_depth = FRAGCOORD.z; //FRAGMENT DEPTH OF THE CURRENT MESH

	//if (fragment_depth < depth_sample){
		//discard;
	//}
	
	ALBEDO = pow(COLOR.rgb, vec3(power));
}