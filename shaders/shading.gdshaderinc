const vec3 up = vec3(0.0, 1.0, 0.0);

float fresnel(float amount, vec3 normal, vec3 view)
{
	return pow((1.0 - clamp(dot(normalize(normal), normalize(view)), 0.0, 1.0 )), amount);
}

vec2 generate_matcap_uvs_basic(vec3 normal_view_space) {
	vec2 matcap_uv = (normal_view_space.xy * 0.5) + 0.5;
	matcap_uv.y = 1.0 - matcap_uv.y;
	return matcap_uv;
}

vec2 get_matcap_uv(vec3 v_world_normal, vec3 v_eye_pos, vec3 v_world_pos, float margin, bool is_spatial) {
	/*
		Fix the normal, and calculate the direction from the eye position.
		Then calculate the reflection based on both of these.
		All of this in world space, for convenience.
	*/
	vec3 normal = normalize(v_world_normal);
	vec3 view_dir = normalize(v_eye_pos - v_world_pos);
	vec3 reflection = reflect(view_dir, normal);

	/*
		Find the horizontal and vertical target direction.
		Differs if we're going spatial.
	*/
	float vert = dot(reflection, up);
	float hor;
	if (is_spatial) {
		hor = cross(reflection, up).z;
	} else {
		hor = cross(reflection, normal).y;
	}

	/*
		Calculate the UV based on the horizontal and vertical direction.
		Account for the margin.
	*/
	vec2 uv = vec2(hor, vert) * (1.0 - margin);
	uv = 0.5 + 0.5 * uv;
	
	return uv;
}

vec2 generate_matcap_uv_stable(
	mat4 modelview_matrix,
	vec3 vertex,
	vec3 normal,
) {
	vec2 matcap_uv = vec2(0.0);
	vec4 view_pos = modelview_matrix * vec4(vertex, 1.0);
    vec3 view_dir = normalize(-view_pos.xyz);
    vec3 view_normal = normalize((modelview_matrix * vec4(normal, 0.0)).xyz);
    
    vec3 up_vector = vec3(0.0, 1.0, 0.0);
    vec3 right_vector = vec3(1.0, 0.0, 0.0);
    
    vec3 ref_vector = (abs(view_dir.y) > 0.9) ? right_vector : up_vector;
    
    vec3 tangent = normalize(cross(ref_vector, view_dir));
    vec3 bitangent = cross(view_dir, tangent);
    
    vec2 coords = vec2(
        dot(view_normal, tangent),
        dot(view_normal, bitangent)
    );

    coords.x = coords.x;
    coords.y = -coords.y; // Flip Y coordinate
    
    matcap_uv = (coords * 0.496 + 0.5);
	
	return matcap_uv;
}

vec2 generate_matcap_uv_perspective_correct(
	vec3 pos,
	vec3 normal,
	mat4 model_matrix,
	mat4 view_matrix,
	mat4 inv_view_matrix,
	mat4 model_view_matrix
) {
	vec3 normal_normalized = normalize((model_view_matrix * vec4(normal, 0.0)).xyz);
	vec3 world_normal = (inv_view_matrix * vec4(normal_normalized, 0.0)).xyz;
	vec3 view_normal = (view_matrix * vec4(world_normal, 0.0)).xyz;

	// get view space position of vertex
    vec3 viewPos = (model_view_matrix * vec4(pos, 1.0)).xyz;
    vec3 viewDir = normalize(viewPos);

    // get vector perpendicular to both view direction and view normal
    vec3 viewCross = cross(viewDir, view_normal);
    
    // swizzle perpendicular vector components to create a new perspective corrected view normal
    view_normal = vec3(-viewCross.y, -viewCross.x, 0.0);

	return (view_normal.xy * 0.5 + 0.5);
}

vec4 sample_matcap_spatial(
	sampler2D matcap_texture,
	vec3 v_world_normal,
	vec3 v_world_pos,
	vec3 v_eye_pos,
	float margin,
	bool is_spatial
) {
	/*
		Fix the normal, and calculate the direction from the eye position.
		Then calculate the reflection based on both of these.
		All of this in world space, for convenience.
	*/
	vec3 normal = normalize(v_world_normal);
	vec3 view_dir = normalize(v_eye_pos - v_world_pos);
	vec3 reflection = reflect(view_dir, normal);
	
	/*
		Find the horizontal and vertical target direction.
		Differs if we're going spatial.
	*/
	float vert = dot(reflection, up);
	float hor;
	if (is_spatial) {
		hor = cross(reflection, up).z;
	} else {
		hor = cross(reflection, normal).y;
	}
	
	/*
		Calculate the UV based on the horizontal and vertical direction.
		Account for the margin.
	*/
	vec2 uv = vec2(hor, vert) * (1.0 - margin);
	uv = 0.5 + 0.5 * uv;
	
	return texture(matcap_texture, uv);
}

vec4 apply_matcap_basic(
	vec4 albedo_color,
	vec4 matcap_color,
	float matcap_intensity,
	float matcap_power,
	float smoothness,
	float metallic,
) {
    vec4 matcap_adjusted = pow(mix(vec4(0), matcap_color, matcap_intensity), vec4(matcap_power));
	float matcap_alpha = matcap_adjusted.a;
	float matcap_visibility = matcap_alpha * smoothness;
	vec4 matcap_add = mix(vec4(0), matcap_adjusted, matcap_visibility) * 0.05;
	vec4 matcap_mul = mix(vec4(1), matcap_adjusted, matcap_visibility);
	return mix(albedo_color + matcap_add, albedo_color * matcap_mul, metallic);
}

vec3 apply_matcap(
	vec3 albedo,
	sampler2D matcap_tex,
	vec2 matcap_uv,
	float matcap_intensity,
	float matcap_power,
	float smoothness,
	float metallic,
) {
	vec4 matcap_color = texture(matcap_tex, matcap_uv);
    vec4 matcap_adjusted = pow(mix(vec4(0), matcap_color, matcap_intensity), vec4(matcap_power));
	float matcap_alpha = matcap_adjusted.a;
	float matcap_visibility = matcap_alpha * smoothness;
	vec3 matcap_add = mix(vec3(0), matcap_adjusted.rgb, matcap_visibility) * 0.05;
	vec3 matcap_mul = mix(vec3(1), matcap_adjusted.rgb, matcap_visibility);
	return mix(albedo + matcap_add, albedo * matcap_mul, metallic);
}

vec3 rgb2hsv(vec3 c) {
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

vec3 hsv2rgb(vec3 c) {
    vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

const mat4 THRESHOLD_MATRIX = mat4(
		vec4(1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0),
		vec4(13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0),
		vec4(4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0),
		vec4(16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0));
		
float dither_transparent(float alpha, vec2 screen_pos) {
	return step(0.0, THRESHOLD_MATRIX[int(screen_pos.x) % 4][int(screen_pos.y) % 4] - (1.0 - alpha));
}