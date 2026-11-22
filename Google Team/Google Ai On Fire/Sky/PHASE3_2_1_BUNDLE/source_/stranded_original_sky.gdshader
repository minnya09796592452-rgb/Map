shader_type sky;
uniform int debug_mode = 0;
uniform sampler2D stars_tex : source_color;
uniform sampler2D cloud_tex : source_color;
uniform sampler2D inscatter_lut : filter_linear, repeat_disable;
uniform sampler2D transmittance_lut : filter_linear, repeat_disable;
uniform sampler2D irradiance_lut : filter_linear, repeat_disable;
uniform vec3 beta_rayleigh = vec3(0.000021896, 0.000051163, 0.00011414);
uniform vec3 beta_mie = vec3(0.000000068, 0.000000103, 0.000000150);
uniform float mie_g = 0.6975;
uniform vec3 sky_color : source_color = vec3(0.4579, 0.9211, 0.9631);
uniform vec3 zenith_color : source_color = vec3(0.14, 0.48, 0.78);
uniform vec3 horizon_color : source_color = vec3(0.68, 0.88, 0.96);
uniform vec3 night_haze : source_color = vec3(0.0172, 0.0229, 0.0300);
uniform vec3 sun_tint : source_color = vec3(0.9631, 0.9521, 0.9211);
uniform float cloud_coverage = 0.52;
uniform float cloud_speed = 0.008;
void sky() {
    float h = clamp(EYEDIR.y, 0.0, 1.0);
    float cos_theta = dot(EYEDIR, LIGHT0_DIRECTION);
    float sun_amt = max(cos_theta, 0.0);
    float sun_elev = clamp(LIGHT0_DIRECTION.y * 0.5 + 0.5, 0.02, 0.98);
    vec2 uv_trans = vec2(clamp(h, 0.02, 0.98), 0.05);
    vec2 uv_irrad = vec2(sun_elev, 0.05);
    float tile = floor(clamp(sun_elev, 0.0, 0.999) * 16.0);
    vec2 uv_inscat = vec2((tile + clamp(cos_theta * 0.5 + 0.5, 0.05, 0.95)) / 16.0, clamp(1.0 - h, 0.05, 0.95));
    vec3 trans = texture(transmittance_lut, uv_trans).rgb;
    vec3 irrad = texture(irradiance_lut, uv_irrad).rgb;
    vec3 inscat = texture(inscatter_lut, uv_inscat).rgb;
    if (debug_mode == 1) { COLOR = trans; return; }
    if (debug_mode == 2) { COLOR = irrad * 6.0; return; }
    if (debug_mode == 3) { COLOR = inscat; return; }
    float phase_r = 0.75 * (1.0 + cos_theta * cos_theta);
    float g2 = mie_g * mie_g;
    float phase_m = 1.5 * ((1.0 - g2) / (2.0 + g2)) * ((1.0 + cos_theta * cos_theta) / pow(max(1.0 + g2 - 2.0 * mie_g * cos_theta, 0.001), 1.5));
    vec3 scatter_col = (beta_rayleigh * phase_r + beta_mie * phase_m * 180.0) * 4200.0 * trans;
    vec3 base_sky = mix(horizon_color, sky_color, smoothstep(0.0, 0.35, h));
    base_sky = mix(base_sky, zenith_color, pow(h, 0.75));
    float day_factor = smoothstep(-0.15, 0.25, LIGHT0_DIRECTION.y);
    vec3 sky_grad = mix(night_haze, base_sky + scatter_col + inscat * 0.14 + irrad * 0.18, day_factor);
    if (EYEDIR.y < 0.0) {
        sky_grad = mix(horizon_color * day_factor + night_haze, vec3(0.047, 0.08, 0.14), clamp(-EYEDIR.y * 5.0, 0.0, 1.0));
    }
    vec2 uv1 = EYEDIR.xz / (abs(EYEDIR.y) + 0.30) * 0.22 + vec2(TIME * cloud_speed, TIME * cloud_speed * 0.4);
    vec2 uv2 = EYEDIR.xz / (abs(EYEDIR.y) + 0.30) * 0.45 - vec2(TIME * cloud_speed * 0.5, 0.0);
    float c1 = texture(cloud_tex, uv1).r;
    float c2 = texture(cloud_tex, uv2).g;
    float cld_mask = smoothstep(1.0 - cloud_coverage - 0.18, 1.0 - cloud_coverage + 0.32, c1 * 0.65 + c2 * 0.35) * smoothstep(0.02, 0.25, h);
    vec3 cld_col = mix(vec3(0.92, 0.95, 0.98), sun_tint, pow(sun_amt, 4.0)) * max(day_factor, 0.12);
    vec3 col = mix(sky_grad, cld_col, cld_mask * 0.68);
    vec3 stars = texture(stars_tex, uv1 * 1.5).rgb * smoothstep(0.35, 0.90, h) * mix(0.85, 0.10, day_factor);
    col += stars * (1.0 - cld_mask);
    col += sun_tint * pow(sun_amt, 360.0) * 3.8 * day_factor;
    col += sun_tint * pow(sun_amt, 10.0) * 0.24 * day_factor;
    COLOR = col;
}
