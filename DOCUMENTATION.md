# Assignment 2 - k-d Tree Collision Detection

## Project Overview

This Unity project demonstrates collision detection using a three-dimensional k-d tree. Scene triangles are stored in the tree, a ray is cast in the current camera direction, and the first intersected triangle is highlighted.

The project contains the following main features:

- A recursively constructed 3D k-d tree
- A wireframe visualization of the tree cells
- Free camera movement
- Custom ray traversal through the k-d tree
- Custom ray-triangle intersection
- First-hit triangle highlighting

## Controls

| Input | Action |
|---|---|
| `W`, `A`, `S`, `D` | Move the player |
| Mouse movement | Rotate the camera |
| Left mouse button | Cast a ray |
| `Space` | Cast a ray |
| `K` | Show or hide the k-d tree wireframe |

## Division of Work

### Ray Casting and Hit Detection

Implemented by: Andrea Windisch

My part of the assignment was the raycasting and hit-detection system. This includes:

- Creating a ray from the camera position and viewing direction
- Traversing the k-d tree using its splitting planes
- Visiting only the relevant near and far tree cells
- Testing rays against triangles in leaf nodes
- Keeping track of the closest intersection
- Highlighting the first intersected triangle
- Creating the runtime mesh and material used for highlighting

### k-d Tree Construction and Visualization

Implemented by: **[COLLEAGUE NAME]**

## Project Structure

### Raycasting Files

#### `Assets/Scripts/rayCasting/RayData.cs`

`RayData` is a small custom data structure containing:

- `origin`: where the ray starts
- `direction`: the normalized direction of the ray

The project uses this type instead of Unity's built-in `Ray` type so that the raycast algorithm only depends on an origin and direction.

#### `Assets/Scripts/rayCasting/RaycastController.cs`

`RaycastController` is the Unity-facing part of the raycast system. It:

1. Reads the configured input action, left mouse button, or space bar.
2. Creates a `RayData` value from the camera position and forward direction.
3. Sends the ray to the k-d tree.
4. Receives the closest intersected triangle and its distance.
5. Draws a debug ray.
6. Creates or updates the red triangle highlight.

The controller does not use `Physics.Raycast`.

### k-d Tree Files

#### `Assets/Scripts/kdTree/KdTree.cs`

The `KdTree` class contains the tree construction and ray query. The ray query recursively traverses nodes using their split axis and split value.

#### `Assets/Scripts/kdTree/KdNode.cs`

Each node stores:

- Its bounding box
- Left and right child nodes
- Triangles when the node is a leaf
- Tree depth
- Split axis
- Split value

#### `Assets/Scripts/kdTree/KdTreeCreator.cs`

This component collects scene triangles, builds the tree, creates the wireframe visualization, and exposes the completed tree to the raycast controller.


## Ray Creation

The ray starts at the camera and points in the current viewing direction:

```csharp
RayData ray = new RayData(
    rayCamera.transform.position,
    rayCamera.transform.forward);
```

The ray can be described mathematically as:

```text
S(t) = origin + t * direction
```

`t` represents the distance from the ray origin.

## k-d Tree Ray Traversal

The ray traversal follows the splitting-plane algorithm from the lecture.

For each internal node:

1. Read the node's split axis and split value.
2. Determine which child contains the current ray-segment origin.
3. Treat that child as the near child and the other as the far child.
4. If the ray is parallel to the splitting plane, visit only the near child.
5. Otherwise, calculate where the ray crosses the splitting plane:

```text
t = (splitValue - origin[axis]) / direction[axis]
```

6. If the crossing is inside the current segment, visit the near segment first.
7. Visit the far segment only when it can still contain a closer hit.
8. If the crossing is outside the segment, visit only the current side.

The near and far segments are handled recursively until a leaf node is reached.

## Leaf-Node Triangle Testing

Triangles are stored in leaf nodes. When traversal reaches a leaf, every triangle in that leaf is tested against the current ray segment.

A hit is accepted only when:

- The ray intersects the triangle
- The intersection lies inside the current ray segment
- The intersection is closer than the previous closest hit

The closest triangle and distance are passed through the recursive calls using `ref` parameters.

## Ray-Triangle Intersection

The implementation uses the Moller-Trumbore algorithm.

First, two triangle edges are calculated:

```csharp
Vector3 edge1 = tri.b - tri.a;
Vector3 edge2 = tri.c - tri.a;
```

The determinant is then calculated. A determinant close to zero means the ray is parallel to the triangle, so there is no unique intersection.

The algorithm calculates the barycentric coordinates `u` and `v`. The intersection lies inside the triangle when:

```text
u >= 0
v >= 0
u + v <= 1
```

Finally, the distance along the ray is calculated. A negative distance is rejected because it lies behind the ray origin.

## First-Hit Selection

The closest distance initially starts at positive infinity. Whenever a valid triangle hit is found, it replaces the current result only if it is closer:

```csharp
distanceFromStart + distance < closest
```

This ensures that a triangle behind another triangle cannot replace the first visible hit.

## Triangles Crossing Split Planes

A triangle can extend across a splitting plane even when its center lies on only one side. Such a triangle is stored in both children.

Without this behavior, the traversal could skip the visible part of a triangle and incorrectly select another triangle behind it.

Tree construction stops splitting a node when both children would receive the complete unchanged triangle list. This prevents ineffective recursion.

## Hit Visualization

When a hit is found, a separate triangle mesh is created from the three hit vertices. It uses a red unlit material so that it is clearly visible.

The highlight is moved slightly along the triangle normal:

```csharp
Vector3 offset = normal * surfaceOffset;
```

This prevents z-fighting between the original triangle and the highlight. Both triangle winding orders are added so the highlight is visible from either side.

If the ray misses, the previous highlight is hidden.

## k-d Tree Visualization

The tree wireframe can be toggled with `K`. The initial state is configured through `showCells` in the Inspector.


## Performance Observations

Raycasting does not test every triangle in the scene. It recursively visits only the tree cells crossed by the ray and skips branches that cannot contain a closer hit.

The most important pruning conditions are:

- Skip null nodes
- Skip invalid or exhausted ray segments
- Skip branches starting behind an existing closer hit
- Skip the far child when the near child already contains a closer hit



## Known Limitations and Future Improvements

- The scene geometry is treated as static after the tree is built.
- Moving meshes would require rebuilding or updating the tree.
- Triangles crossing split planes are duplicated, which improves correctness but increases memory usage.
- The hit visualization is created at runtime and represents one triangle at a time.

