# Changelog

## [2.1.0] - 2026-11-28
### Changes
- Add blur.cginc. It samples between texels with a bilinear, clamped sampler so that the filtering averages them, and needs fewer samples for the same blur: `blur_downsample_box4` (4 samples, 4x4 box average at half resolution), `blur_downsample_center5` (center and 4 diagonals at half resolution; the same 4x4 texels as box4, weighted toward the center), `blur_upsample` (8 directions, the spread set by `offset`) and the separable gaussians `blur_gaussian5` (3 samples for 5 taps) and `blur_gaussian9` (5 samples for 9 taps). The weights of every function add up to 1. It needs no other include, so compute shaders can use it
- Add the Blur Sample (`Samples~/BlurSample`): a URP Renderer Feature that uses the RenderGraph API to halve the camera color a set number of times (1 to 6) with `blur_downsample_center5`, enlarge it back with `blur_upsample` and write it back to the camera color, for Game and SceneView cameras only. It needs Unity 6 and URP, and does not support single-pass instanced XR
- README: describe blur.cginc, how to pass the sampler and the texel size in Built-in and URP, why a wide blur needs passes outside the shader, notes on color space and alpha, and the Blur Sample

## [2.0.0] - 2026-11-27
### Breaking changes
- fractal.cginc: remove `fractal_glow_rings`, every `fractal_glow_rings_color` overload and the `FRACTAL_GLOW_RINGS_*` constants, because their origin could not be confirmed. The Shader Sample no longer has the Glow Rings toggle
- fractal.cginc: `fractal_apollonian` has a new implementation, because the origin of the previous one could not be confirmed. The name, the parameters and the meaning of .y (smallest squared distance to the origin) stay the same. .x is now an estimate of the distance to the unit circle and the axes, so it measures a different shape than before, and the picture changes. `fractal_apollonian_color` and its default style change with it
- package.json: `"unity"` is now `"2022.3"`
### Changes
- Move the package from the ShaderModules repository (`Packages/Mods`) to this repository (`Packages/com.amenone.shadermodules`). The package name and the .meta GUIDs stay the same, so `#include "Packages/com.amenone.shadermodules/Shaders/..."` and references to the package assets keep working
- Move the samples (`Shaders/sample`, `ComputeParticleSample`, `ComputeShaderSample`) to `Samples~` and list them in package.json (Shader Sample, Compute Particle Sample, Compute Shader Sample). They are imported from Package Manager instead of being part of the package. The .meta files in `Samples~` are kept, so an imported sample has the same GUIDs as before
- Remove the unused Dummy.cs and DummyShaderModule.asmdef. The DummyShaderModule assembly no longer exists
- noise.cginc and LICENSE.md: the webgl-noise notice also lists `mod289`, `permute` and `simplex_noise_deriv` (built from `simplex_noise`). noise.cginc only gets a one line comment on `simplex_noise_deriv`; its code does not change
- Add README and CHANGELOG

Versions up to 1.0.5 were released from the ShaderModules repository.
