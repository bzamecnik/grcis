#extension GL_EXT_gpu_shader4 : enable

uniform sampler3D volumeTexture;
uniform float selectedDepth; // [0;1]
uniform float depth; // size of the volume (in pixels)
//uniform vec2 valueRange;
uniform mat4 cameraToWorld;

//uniform vec2 senzorSize;

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


//vec4 rayCastVolume(vec2 pixelPos) {
	//vec3 senzorPos = vec3(senzorSize * (pixelPos - vec2(0.5, 0.5)), 0.0);
	
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
	//   - find out the number of step of ray marching
	//   - march the ray
	//     - get volume density at the current position
	//     - evaluate and accumulate color
	//   - return the accumulated color
//}

void main() {
	vec3 color = getOrthogonalSlice();
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
	
	gl_FragColor = vec4(color.rgb, 1);
}
