//uniform sampler2D functionTex;
//varying vec2 texCoord; // rather gl_TexCoord[0]
uniform vec2 offset;
uniform float scale;
uniform float valueDrift;
uniform int functionIndex;
uniform float thresholds[256];
uniform int thresholdCount;

// TODO:
// - enable compiling the shader with user-supplied function
// - the computation could be performed in two phases
//   - first, the function could be sampled and stored into a texture
//   - second, the isocontours could be computed
//     - function evaluation could be done as texture lookup

float implicit_function(in float x, in float y) {
// ### FUNCTION ###
}

float f(in float x, in float y) {
	return implicit_function(x * scale, y * scale) + valueDrift;
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
	bool isContour = false;
	for (int i = 0; i < thresholdCount; i++) {
		float threshold = thresholds[i];
		// It is better not to break early as we know there is an isocontour.
		// Conditional execution slows the shader more the a few unnecessary
		// comparisons.
		isContour = isContour || ((minValue < threshold) && (maxValue >= threshold));
	}
	return isContour;
}

void main() {
	vec2 coord = (gl_FragCoord.xy - offset);
	
	if (isIsoContour(coord)) {
		// show isoline
		gl_FragColor.rgb = vec3(0.0, 0.0, 0.0);
	} else {
		//plot the color-coded function value
		float intensity = 0.25 * f(coord.x, coord.y);
		gl_FragColor.r = 0.75 + intensity;
		gl_FragColor.b = 0.75 - intensity;
		gl_FragColor.g = 0.75;
	}
}
