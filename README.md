# Marching Cube algorithm implemented in Unity DOTS

This is my implementation of the marching cubes algorithm using Unity DOTS. My main goal was to create a fast mesh generation. It is using the Unity Job System. It is an initial version, and I plan to improve it in the future. Right now, it can only generate a mesh from a noise function. It does not have any vertex sharing. Triangles for each cube are independent of each other.

## How to use it?

Open TerrainTestScene. In the TerrainTester game object, there is a TerrainTester component. It allows changing the generation parameters. Terrain size defines how big the terrain will be. Max chunk size defines the dimensions of a single terrain chunk. It allows for splitting terrain into multiple meshes. Cubes per unity defines the amount of marching cubes generated per Unity unit. It allows to generate more detailed, smoother terrain.

You can adjust the parameters of the noise using Noise Scale and Noise Offset. Offset can also be moved during runtime using WSAD keys.

<img width="445" height="480" alt="image" src="https://github.com/user-attachments/assets/9c6a6e75-5a7d-42a2-896d-0e73dab419b5" />

## Some performance characteristics
My machine specs:
- CPU: AMD Ryzen 9 7950X (16 cores)
- GPU: RTX 4070 Super
- RAM: 64 GB DDR5 6400 MHz
Performance:

### Test 1 

**Terrain Size:** 256x24x256, 
**Max Chunk Size:** 64x8x64, 
**Cubes Per Unit:** 2.

Total meshes: 48 
Total vertices: 13 875 534 
Total Triangles: 4 625 178 
Average Vertices per chunk: 289 073 
Average Triangles per chunk: 96 357 
Time: 128.30750 ms 
Average time: 2.67307

### Test 2 

**Terrain Size** 256x24x256,
**Max Chunk Size:** 64x8x64,
**Cubes Per Unit:** 3. 

Total meshes: 48 
Total vertices: 31 228 776 
Total Triangles: 10 409 592 
Average Vertices per chunk: 650 599 
Average Triangles per chunk: 216 866 
Time: 338.58020 ms 
Average time: 7.05375

### Test 3 
**Terrain Size** 128x24x128,
**Max Chunk Size:** 32x8x32,
**Cubes Per Unit:**: 2. 

Total meshes: 48 
Total vertices: 3 465 474 Total 
Triangles: 1 155 158 
Average Vertices per chunk: 72 197 
Average Triangles per chunk: 24 065 
Time: 23.37370 ms 
Average time: 0.48695

It can maintain a steady 30 fps, generating almost 3.5 million vertices each frame inside the Unity editor in release mode.

https://github.com/user-attachments/assets/f79b2f2c-8fcf-4efc-9d9c-434feb2583d4

## Plans for the future

I would like to improve this in the future. 

First thing on my TODO list is to use vertex sharing in adjacent triangles. It will improve the smoothness of the mesh and reduce the number of vertices. 
Depending on how performant it will be it may even improve performance, if the cost of calculating it will be smaller than the gains from a reduced amount of vertices that need to be assigned to the mesh and passed to the GPU.

The second thing is to add a more generic way of providing data for mesh generation. Not only simple noise. For example, some textures with SDFs.





