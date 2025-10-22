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
uniform float foam_distance = 2;
uniform sampler2D DEPTH_TEXTURE : hint_depth_texture;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture;

uniform float sparkle_amount : hint_range(0.0, 1.0) = 0.4;

// Returns 1.0 if we can see above the water, 0.0 otherwise
float snells_window(vec3 normal, vec3 view, float ior) {
	float cos_theta = dot(normal, view);
	return step(sqrt(1.0 - cos_theta * cos_theta) * ior, 1.0);
}

float fresnel(float amount, vec3 normal, vec3 view)
{
	return pow((1.0 - clamp(dot(normalize(normal), normalize(view)), 0.0, 1.0 )), amount);
}

float edge(float near, float far, float depth){
	depth = 1.0 - 2.0 * depth;
	return near * far / (far + depth * (near - far));
}

vec3 unpack_normal_map(vec4 rgba) {
     //https://youtu.be/-dMbacvrDrs?si=q68d93F2_R61e6Bs
    vec3 n = rgba.xzy * vec3(2.0) - vec3(1.0);
    n.z *= -1.0;
    return n;
}

//vec3 get_normal_from_map(sampler2D normal_map, vec2 uv) {
    //float uv_scale = 1.0 / 2048.0;
    //vec3 l = unpack_normal_map(texture(normal_map, uv + uv_scale * vec2(-1.0, 0.0)));
    //vec3 r = unpack_normal_map(texture(normal_map, uv + uv_scale * vec2(1.0, 0.0)));
    //vec3 u = unpack_normal_map(texture(normal_map, uv + uv_scale * vec2(0.0, -1.0)));
    //vec3 d = unpack_normal_map(texture(normal_map, uv + uv_scale * vec2(0.0, 1.0)));
    //
    //vec3 normalized = normalize(l + r + u + d);
    //return normalized;
//}

void fragment() {
	ROUGHNESS = 0.1;
	METALLIC = 0.1;

	vec3 world_vertex = (INV_VIEW_MATRIX * vec4(VERTEX,1.0)).xyz;
	
	float fresnel = fresnel(5.0, NORMAL, VIEW);

	// Basic waves
	float time = TIME * 0.015;
	vec2 normal_uv = vec2(world_vertex.x, world_vertex.z) * 0.005;
	vec3 normal_sample_1 = (texture(normal_map_texture, normal_uv + vec2(-1, 1) * time)).rgb;
	vec3 normal_sample_2 = (texture(normal_map_texture, -normal_uv + vec2(0, 1) * time)).rgb;
	vec3 normal_map = vec3((normal_sample_1.xy + normal_sample_2.xy) * 0.5, 0.0);
	
	// Set NORMAL directly since we need to use it later
	NORMAL_MAP_DEPTH = 0.5;
	normal_map.z = sqrt(max(0.0, 1.0 - dot(normal_map.xy, normal_map.xy)));
	NORMAL_MAP = normal_map;
	
	// Screen distortion
	vec2 distorted_uv = SCREEN_UV - (normal_map.xy * 2.0 - 1.0) * NORMAL_MAP_DEPTH * 0.2;
	
	// depth variables and calc
	float depth_sample = textureLod(DEPTH_TEXTURE, distorted_uv, 0.0).r;
	vec3 screen_sample = textureLod(SCREEN_TEXTURE, distorted_uv, 0.0).rgb;

	vec3 ndc = vec3(distorted_uv * 2.0 - 1.0, depth_sample);
	vec4 world = INV_VIEW_MATRIX * INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	float depth_texture_y = world.y / world.w;
	float vertex_y = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).y;
	float depth_blend = clamp((vertex_y - depth_texture_y) / depth_distance, 0.0, 1.0);
	depth_blend = exp(-depth_blend * beers_law);
	
	// Need a water depth value for both the regular UVs and the distorted UVs
	// regular waterdepth
	float z_depth = edge(0.05, 200, texture(DEPTH_TEXTURE, SCREEN_UV).x); // depth texture
	float z_pos = edge(0.05, 200, FRAGCOORD.z); // from position
	float waterDepth = z_depth - z_pos; // diff
	float behind_multiplier = max(sign(waterDepth), 0.0);
	// distorted waterdepth
	float z_depth_distorted = edge(0.05, 200, texture(DEPTH_TEXTURE, distorted_uv).x); // depth texture
	float z_pos_distorted = edge(0.05, 200, FRAGCOORD.z); // from position
	float waterDepth_distorted = z_depth_distorted - z_pos_distorted; // diff
	float behind_multiplier_distorted = max(sign(waterDepth_distorted), 0.0);
	
	float depth_blend_foam = clamp((vertex_y - depth_texture_y) / foam_distance, 0.0, 1.0);
	depth_blend_foam = exp(-depth_blend_foam * beers_law);
	depth_blend_foam *= 1.0 - waterDepth;
	
	depth_blend *= behind_multiplier_distorted;
	vec3 screen_color = textureLod(SCREEN_TEXTURE, distorted_uv, depth_blend * 2.5).rgb;
	vec3 depth_color = mix(color_deep.rgb, color_shallow.rgb, depth_blend);
	vec3 color = mix(depth_color * 0.25, depth_color, depth_blend);
	
	vec3 surface_color = mix(water_color, color, 0.75);
	ALBEDO = mix(surface_color, mix(surface_color * 10.0, vec3(1.0), 0.5), fresnel);
	
	float bump_amount = normal_sample_1.x * normal_sample_2.x;
	float sparkle_value = sign(sparkle_amount * bump_amount - 0.1);
	float shallow_foam_value = step(0.9, (bump_amount + depth_blend_foam) * 2.0);
	ALBEDO = mix(ALBEDO, vec3(1.0), max(0.0, sparkle_value));
	ALBEDO = mix(ALBEDO, vec3(1.0), max(0.0, shallow_foam_value));

	// Apply Snell's window mask to water surface when viewed from below
	if(!FRONT_FACING) {
		float mask = snells_window(NORMAL, VIEW, index_of_refraction);
		ALBEDO = mix(water_color, ALBEDO, mask);
		EMISSION = ALBEDO;
		RADIANCE = vec4(vec3(0.0), mask);
		IRRADIANCE = vec4(vec3(1.0), mask);
	}
	
	ROUGHNESS = 0.15;
	METALLIC = 0.0;
}