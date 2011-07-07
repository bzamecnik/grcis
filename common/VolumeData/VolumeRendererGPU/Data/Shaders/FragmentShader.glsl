#extension GL_EXT_gpu_shader4 : enable

uniform sampler3D volumeTexture;
uniform float selectedDepth; // [0;1]
uniform float depth; // size of the volume (in pixels)
//uniform vec2 valueRange;
uniform mat4 cameraToWorld;

uniform vec2 senzorSize;

vec4 getOrthogonalSlice() {
	float z = clamp(selectedDepth, 0.0, 1.0);
	// top view:
	vec4 color = texture3D(volumeTexture, vec3(gl_TexCoord[0].st, z));
	// front view:
	//vec4 color = texture3D(volumeTexture, vec3(gl_TexCoord[0].s, 1-z, 1-gl_TexCoord[0].t));
	// side view:
	//vec4 color = texture3D(volumeTexture, vec3(z, gl_TexCoord[0].s, 1-gl_TexCoord[0].t));
	//color = color / (valueRange.t - valueRange.s) + valueRange.s;
	return color;
}

vec4 averageAlongZ() {
	vec4 colorAccum = vec4(0, 0, 0, 0);
	vec3 coords = vec3(gl_TexCoord[0].st, 0);
	float zStep = 1 / float(depth - 1);
	for (int i = 0; i < depth; i += 1) {
		colorAccum += texture3D(volumeTexture, coords);
		coords.z += zStep;
	}
	return colorAccum / float(depth);
}

vec4 maxIntensityProjAlongZ() {
	float maxIntensity = 0;
	vec3 coords = vec3(gl_TexCoord[0].st, 0);
	float zStep = 1 / float(depth - 1);
	for (int i = 0; i < depth; i += 1) {
		float intensity = texture3D(volumeTexture, coords).r;
		maxIntensity = max(intensity, maxIntensity);
		coords.z += zStep;
	}
	return maxIntensity;
}

vec4 averageAlongRay(vec3 origin, vec3 directionStep, int count) {
	vec4 colorAccum = vec4(0, 0, 0, 0);
	vec3 coords = origin;
	for (int i = 0; i < count; i += 1) {
		colorAccum += texture3D(volumeTexture, coords);
		coords += directionStep;
	}
	return colorAccum / float(count);
}


	//vec3 rayOrigin = lensPos;
    //vec3 rayDir = outputDir - rayOrigin;
    //vec3 planeOrigin = vec3(0, 0, imageLayerDepth);
	//vec3 planeNormal = normalize(vec3(0.25, 0, 1));
    //
    //float t = dot((planeOrigin - rayOrigin), planeNormal) / dot(rayDir, planeNormal);
    //// when plane normal is (0,0,1) only z component is used:
    ////float t = (imageLayerDepth - rayOrigin.z) / rayDir.z;
    //if (t < 0.0)
    //{
		//return vec4(1, 0, 1, 0); // no intersection
	//}    
    //vec3 intersectionPos = rayOrigin + t * rayDir;

// pixelPos - pixel position in normalized device space [0; 1]^2
// rayStepLength - step for ray marching (in world space units)
vec4 rayCastVolumeMIP(vec2 pixelPos, float rayStepLength) {
	// senzor position in camera space
	// map from [0; 1]^2 to [-w/2;w/2]x[-h/2;h/2]
	// TODO: use a matrix (ndcToCamera)
	vec2 senzorPos = senzorSize * (pixelPos - vec2(0.5, 0.5));
	// ray origin in world space
	vec3 rayOrigin = (cameraToWorld * vec4(senzorPos, 0.0, 1.0)).xyz;
	// case of orthogonal projection
	vec3 rayDirection = (cameraToWorld * vec4(0.0, 0.0, 1.0, 0.0)).xyz;
	
	// clip the ray by the volume cube [0;1]^3 to a line segment
	// TODO
	
	vec3 rayStart = rayOrigin;
	vec3 rayEnd = rayOrigin + 1 * rayDirection;
	
	vec3 position = rayStart;
	vec3 rayStep = rayStepLength * normalize(rayDirection);
	int stepCount = int(length(rayEnd - rayStart) / rayStepLength);
	vec4 colorAccum = vec4(0, 0, 0, 0);
	float maxIntensity = 0;
	for (int i = 0; i < stepCount; i += 1) {
		float intensity = texture3D(volumeTexture, position + vec3(0.5, 0.5, 0.5)).r;
		
		maxIntensity = max(intensity, maxIntensity);
		//colorAccum.rgb += intensity;
		
		position += rayStep;
	}
	colorAccum.rgb = maxIntensity;
	//colorAccum.rgb /= (float)stepCount;
	colorAccum.a = 1;
	return colorAccum;
	
	// TODO:
	// - input:
	//    - pixel position on senzor [0;1]^2
	//    - constant:
	//      - volume data (3D texture)
	//      - senzor size in camera space
	//      - uniform spacing between points for ray marching
	// - output: color
	// - algorithm:
	//   - transform pixel position to senzor position in camera space
	//   - transform senzor position to world space -> ray origin
	//     - world to camera matrix
	//   - compute ray direction
	//     - using orthographic or perspective transform
	//       - orthographic - normal to senzor plane
	//       - perspective - (center of projection) - (ray origin)
	//   - clip the ray to the volume cube
	//     - compute six ray to square intersections
	//     - find out min/max ray parameters
	//     - compute start/end points (in world space)
	//       - no intersection -> zero ray marching steps
	//   - find out the number of steps of ray marching
	//   - march the ray
	//     - get volume density at the current position
	//     - evaluate and accumulate color
	//   - return the accumulated color
}


vec3 hsvToRgbColor(float hue, float saturation, float value) {
	float chroma = value * saturation;
    float h = 6 * hue;
    float  x = chroma * (1 - abs(mod(h, 2.0) - 1));
    float  r1 = 0;
    float  g1 = 0;
    float  b1 = 0;
    switch (int(floor(h)))
    {
        case 0: r1 = chroma; g1 = x; break;
        case 1: r1 = x; g1 = chroma; break;
        case 2: g1 = chroma; b1 = x; break;
        case 3: g1 = x; b1 = chroma; break;
        case 4: r1 = chroma; b1 = x; break;
        case 5: r1 = x; b1 = chroma; break;
        default: break;
    }
    float m = value - chroma;
    float r = r1 + m;
    float g = g1 + m;
    float b = b1 + m;
    return vec3(r, g, b);
}

vec4 rayCastVolumeGlowingFog(vec2 pixelPos, float rayStepLength) {
	// senzor position in camera space
	// map from [0; 1]^2 to [-w/2;w/2]x[-h/2;h/2]
	// TODO: use a matrix (ndcToCamera)
	vec2 senzorPos = senzorSize * (pixelPos - vec2(0.5, 0.5));
	// ray origin in world space
	vec3 rayOrigin = (cameraToWorld * vec4(senzorPos, 0.0, 1.0)).xyz;
	// case of orthogonal projection
	vec3 rayDirection = (cameraToWorld * vec4(0.0, 0.0, 1.0, 0.0)).xyz;
	
	// clip the ray by the volume cube [0;1]^3 to a line segment
	// TODO
	
	vec3 rayStart = rayOrigin;
	vec3 rayEnd = rayOrigin + 1 * rayDirection;
	
	vec3 position = rayStart;
	vec3 rayStep = rayStepLength * normalize(rayDirection);
	int stepCount = int(length(rayEnd - rayStart) / rayStepLength);
	
	// attenuation exponent
    float tau = 3.0;

    float maxDensity = 0;
    int maxPosition = 0; // position of max density
    float attenuation = 1; // accumulator
    float prevIntensity = 0;
    float intensity = 0;
    float thickness = 1;// rayStepLength;
    float attenuationThreshold = 0.01;
	
	for (int i = 0; i < stepCount; i += 1) {
		float density = texture3D(volumeTexture, position + vec3(0.5, 0.5, 0.5)).r;
		
        if (density >= maxDensity)
        {
            maxDensity = density;
            maxPosition = i;
        }
        float sliceIntensity = density * thickness;
        float sliceAttenuation = exp(-tau * prevIntensity);
        attenuation *= sliceAttenuation;
        intensity += sliceIntensity * attenuation;
        prevIntensity = sliceIntensity;
        if (attenuation < attenuationThreshold)
        {
            break;
        }

		position += rayStep;
	}
	float maxPositionNormalized = maxPosition / float(stepCount - 1);
    intensity = min(intensity, 1.0);

    return vec4(hsvToRgbColor(1 - maxDensity, 1 - maxPositionNormalized, intensity), 1);
}

void main() {
	//vec3 color = getOrthogonalSlice();
	//vec3 color = maxIntensityProjAlongZ();
	
	////vec3 from = vec3(0, 0, 0);
	////vec3 to = vec3(0, 0, 1);
	////vec3 count = depth;
	//vec3 count = 50;
	//////vec3 directionStep = (to - from) / float(count - 1);
	//vec3 origin = vec3(gl_TexCoord[0].st - vec2(0.5, 0.5), 0);
	////vec3 directionStep = vec3(0, 0, 1) / float(count - 1);
	//vec3 directionStep = vec3(0, 0, 1) / float(count - 1);
	//origin = (cameraToWorld * vec4(origin, 1)).xyz;
	//directionStep = (cameraToWorld * vec4(directionStep, 1)).xyz;
	//vec3 color = averageAlongRay(origin, directionStep, count);
	
	//vec3 color = rayCastVolumeMIP(gl_TexCoord[0].st, 1.0/305.0).rgb;
	//vec3 color = rayCastVolume(gl_TexCoord[0].st, 0.01).rgb;
	vec3 color = rayCastVolumeGlowingFog(gl_TexCoord[0].st, 1.0/305.0).rgb;
	
	gl_FragColor = vec4(color.rgb, 1);
}
