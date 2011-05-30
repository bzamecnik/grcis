//uniform sampler2D functionTex;
//varying vec2 texCoord; // rather gl_TexCoord[0]
uniform vec2 offset;
uniform float scale;
uniform float valueDrift;
uniform int functionIndex;

// TODO: pass thresholds as a 1D texture

float f_waves0(in float x, in float y) {
	return sin(0.1 * x) + cos(0.1 * y);
}

float f_drop0(in float x, in float y) {
	float r = 0.1 * sqrt(x * x + y * y);
	return ((r <= 10e-16) ? 10.0 : (10.0 * sin(r) / r));
}

float f(in float x, in float y) {
	if (functionIndex == 0) {
		return f_drop0(x * scale, y * scale) + valueDrift;
	} else {
		return f_waves0(x * scale, y * scale) + valueDrift;	
	}
}

// Algorithm:
// Josef Pelikan: Rastrove algoritmy pro vypocet izocar, KSVI MFF UK, 1992
bool isIsoContour(vec2 coord) {
	float x = coord.x;
	float y = coord.y;
	
	float pixelSize = scale;
	vec4 sideCenterValues = vec4(
		f(x + 0.5, y),
		f(x, y + 0.5),
		f(x + 1.0, y + 0.5),
		f(x + 0.5, y + 1.0));
	float minValue = 10e20;
	float maxValue = -10e20;
	for (int i = 0; i < 4; i++) {
		minValue = min(minValue, sideCenterValues[i]);
		maxValue = max(maxValue, sideCenterValues[i]);
	}
	for (float threshold = -10.0; threshold <= 10.0; threshold += 0.5) {
		if ((minValue < threshold) && (maxValue >= threshold)) {
			return true;
		}
	}
	return false;
}

void main() {
	//gl_FragColor = texture2D(functionTex, texCoord);
	vec2 coord = (gl_FragCoord.xy - offset);
	
	if (isIsoContour(coord)) {
		// show isoline
		gl_FragColor.rgb = vec3(0, 0.9, 0);
	} else {
		// plot the color-coded function value
		gl_FragColor.r = 0.25 * f(coord.x, coord.y) + 0.5;
	}
}
