const vec3 up = vec3(0.0, 1.0, 0.0);

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

vec3 apply_matcap(
	vec3 albedo,
	sampler2D matcap_tex,
	float matcap_intensity,
	float matcap_power,
	float smoothness,
	float metallic,
	vec3 normal_map_sample,
	vec3 tangent,
	vec3 binormal,
	vec3 normal,
	mat4 inv_view_matrix,
	vec3 eye_pos,
	vec3 world_pos
) {
	vec3 normal_offset = normal_map_sample * vec3(2.0) - vec3(1.0);
	vec3 normal_world = normalize(tangent * normal_offset.y + binormal * normal_offset.x + normal * normal_offset.z);
	vec3 v_world_normal = (inv_view_matrix * vec4(normal_world, 0.0)).rgb;
	vec2 matcap_uv = get_matcap_uv(v_world_normal, eye_pos, world_pos, 0.15, false);
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