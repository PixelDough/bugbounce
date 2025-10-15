shader_type spatial;
render_mode cull_disabled;
render_mode depth_draw_always;

uniform sampler2D normal_map_texture : hint_normal;
uniform vec3 water_color : source_color = vec3(0.0625, 0.1992, 0.3594);
uniform float index_of_refraction = 1.333;

uniform vec4 color_deep : source_color;
uniform vec4 color_shallow : source_color;
uniform float beers_law = 2.0;
uniform float depth_distance = 2;
uniform sampler2D DEPTH_TEXTURE : hint_depth_texture;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture;

// Returns 1.0 if we can see above the water, 0.0 otherwise
float snells_window(vec3 normal, vec3 view, float ior) {
	float cos_theta = dot(normal, view);
	return step(sqrt(1.0 - cos_theta * cos_theta) * ior, 1.0);
}

float fresnel(float amount, vec3 normal, vec3 view)
{
	return pow((1.0 - clamp(dot(normalize(normal), normalize(view)), 0.0, 1.0 )), amount);
}

void fragment() {
	ROUGHNESS = 0.1;
	METALLIC = 0.1;

	vec3 world_vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.)).xyz;
	
	float fresnel = fresnel(5.0, NORMAL, VIEW);

	// Basic waves
	float time = TIME * 0.05;
	vec2 normal_uv = vec2(world_vertex.x, world_vertex.z) * 0.025;
	vec3 normal_sample_1 = texture(normal_map_texture, normal_uv + vec2(-1, 1) * time).xyz * 2.0 - 1.0;
	vec3 normal_sample_2 = texture(normal_map_texture, -normal_uv + vec2(0, 1) * time).xyz * 2.0 - 1.0;
	vec3 normal_map = vec3(normal_sample_1.xy + normal_sample_2.xy, 0.0);
	
	// Set NORMAL directly since we need to use it later
	NORMAL_MAP_DEPTH = 0.5;
	normal_map.z = sqrt(max(0.0, 1.0 - dot(normal_map.xy, normal_map.xy)));
	NORMAL = normalize(mix(NORMAL, TANGENT * normal_map.x + BINORMAL * normal_map.y + NORMAL * normal_map.z, NORMAL_MAP_DEPTH));
	
	// Screen distortion
	vec2 distorted_uv = SCREEN_UV - normal_map.xy * NORMAL_MAP_DEPTH * 0.1;
	
	// depth variables and calc
	float depth_sample = textureLod(DEPTH_TEXTURE, distorted_uv, 0.0).r;
	vec3 screen_sample = textureLod(SCREEN_TEXTURE, distorted_uv, 0.0).rgb;

	vec3 ndc = vec3(distorted_uv * 2.0 - 1.0, depth_sample);
	vec4 world = INV_VIEW_MATRIX * INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	float depth_texture_y = world.y / world.w;
	float vertex_y = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).y;
	float depth_blend = clamp((vertex_y - depth_texture_y) / depth_distance, 0.0, 1.0);
	depth_blend = exp(-depth_blend * beers_law);
	
	vec3 screen_color = textureLod(SCREEN_TEXTURE, distorted_uv, depth_blend * 2.5).rgb;
	vec3 depth_color = mix(color_deep.rgb, color_shallow.rgb, depth_blend);
	vec3 color = mix(depth_color * 0.25, screen_color * depth_color, depth_blend);
	
	vec3 surface_color = mix(water_color, color, 0.75);
	ALBEDO = mix(surface_color, mix(surface_color * 10.0, vec3(1.0), 0.5), fresnel);
	
	// Apply Snell's window mask to water surface when viewed from below
	if(!FRONT_FACING) {
		float mask = snells_window(NORMAL, VIEW, index_of_refraction);
		ALBEDO = mix(water_color, ALBEDO, mask);
		EMISSION = ALBEDO;
		RADIANCE = vec4(vec3(0.0), mask);
		IRRADIANCE = vec4(vec3(1.0), mask);
	}
}