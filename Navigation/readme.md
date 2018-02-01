# Navigation

As noted before, fully understanding this navigation algorithm will take a lot of work.  
I will summarize the workings on a more in-depth level in this document, but if you truly want to understand the complexity, you will have to dive into the code yourself.

## Structure

The goal is to form a valid path for every ship, from its current position to its final destination.  
That means, while we are only submitting the move for this frame to the game, we essentially decide our moves until the ship's goal is reached.  
Note that this path will almost never be traversed precisely as first calculated - with changing context, unexpected enemy behaviour or changing goals, the path is almost sure to change completely next frame.

We can divide the process of building these paths into three steps:

* First, we build a model of the world that will save all our ships' paths, as well as the predicted paths of enemy ships, and the stationary 'paths' of planets.
* Next, we iteratively rebuild the paths for all our ships, such that they will change to avoid colliding with other ships.
* Lastly, we postprocess any paths that did not manage to avoid collision, by cutting their paths short right before the moment of collision (rather than pathing around the collision)

Each of these steps has their own intricacies, so let's dive in.

### NavigationWorld

To make sure collision checking in the next step will be successful, we want to make our model of the world as accurate as possible.  
This is difficult, because there is information we know, and information we don't know.  
In particular, we have no idea how the enemy undocked ships will move.  
To make a wild guess, however, we can naively extrapolate the movements of the enemy ships over the last turn to approximate their movements instead.

Next, while we don't know how our ships will move after all three steps are complete, we are in control of moving them in the end, so setting their paths here is the first step to figuring them out.  
This is what I call the 'offline' step:
* First, we calculate the same naive extrapolated paths for our own moving ships - any ships that are docking will be excepted as they cannot move
* Second, each ship will create a 'smart' path in this world of extrapolated paths, where they attempt to reach their goal while avoiding the other extrapolated paths. Note that these new paths are not saved to the NavigationWorld yet, so they do not yet respond to other 'smart' paths
* Lastly, after each of these new paths is created, they are saved to the NavigationWorld. Note that many of these would be intersecting and causing collisions if followed.

In the next step, we will continue creating smart paths for our ships, but now we constantly update the NavigationWorld for every path created.  
Hence, that will be the 'online' step.

### Avoiding Collisions

Let's start with a simple example.

![Collision Example](https://i.imgur.com/edrOD9o.png)

* Ship A wants to find a path to goal D.  
* In its way are two (unmoving) planets, P1 and P2.  
* The ship first tries to move to D in a straight line. Clearly, this generates a collision with P1.  
* In order to avoid P1, we need to generate a new point to avoid it, we call this point C.  
* Having generated this point, we dive into the first recursion - we cannot assume we can safely reach point C, so now we must first find a path from A to C  
* Evidently, this also generates a collision, now with P2. Again, we generate a new 'safe' point, point B, and recurse to find a path from A to B  
* This time, going from A to B in a straight line is possible without colliding, meaning we have found the first part of our path.  
* We step out of the second recursion and now check the second part of this partial path - can we traverse from B to C in a straight line?  
* Since the answer is yes, we can safely combine these straight paths into a more complex path: A-B-C  
* We step out of the first recursion and now check the second part of this partial path - can we traverse from C to D in a straight line?  
* Again the answer is yes, so we combine the two safe paths (A-B-C and C-D) to form A-B-C-D, which is a collisionless path from A to D.

This is the basic approach to pathing, but three questions remain unanswered:

* How do we calculate the new points (B and C)?
* How do we recognize collisions?
* How does this translate to moving colliders?

#### Calculating Avoidance Points

Luckily, we have the liberty of assuming each collider is circular in this game. As a result, each collider has a center and radius we can work with.  
In the case of calculating avoidance points, we use the collider data to form tangents from our position to the collider.  
Tangents are the lines you can draw from any point outside a circle, that only touch the circle (and do not miss or intersect it)  
The two points on the circle where the tangents touch the circle are called tangent points.  
This gives us two points that we can reach in a straight line, while avoiding the collider.  
We choose the point closest to our final target to minimize distance traveled.

The relevant code can be found in [NavigationPath.CreateCollisionAvoidance()](https://github.com/Lunariz/Halite2/blob/master/Navigation/NavigationPath.cs#L180)

#### Recognizing collisions & Moving colliders

In order to find out if a path of a given ship will collide with a path of any other ship, we first need to iterate over every other path in our NavigationWorld.  
Each path consists of segments. In the example above, A-B would be a segment, as would B-C and C-D.  
Each segment can further be subdivided into frames. Since Halite enforces a maximum movement distance of 7 units per frame, a segment of length 10 can be divided into segments of length 7 and 3, each performable within one movementframe.

We do this subdivision for both paths we are looking at, and then attempt to find a collision between each matching segment:  
Segment 1 of Path A is compared to Segment 1 of Path B  
Segment 2 of Path A is compared to Segment 2 of Path B  
...  
Segment 1 of Path A is compared to Segment 1 of Path C  
Segment 2 of Path A is compared to Segment 2 of Path C  
...  
Etc.

However, there is one problem - in our attempt to find the earliest collision, checking paths one by one does not guarantee we find the earliest collision first.  
Meaning, it is possible to find a collision at the end of our path earlier than another collision at the start of our path.  
So instead of iterating by path, we need to iterate by segment # first:  
Segment 1 of Path A is compared to Segment 1 of Path B  
Segment 1 of Path A is compared to Segment 1 of Path C  
...  
Segment 2 of Path A is compared to Segment 2 of Path B  
Segment 2 of Path A is compared to Segment 2 of Path C  
...  
Etc.

This guarantees that the first collision we find, is the first collision that would occur in-game.  
In addition, this serves as a good optimization, as we can skip a lot of collision checking if we do find one early.

Another optimization is the usage of AABBs, or axis-aligned boundingboxes.  
Two paths whose bounding boxes do not overlap, cannot possibly collide in any given segment.  
As such, we can remove any paths from the list of paths to check if their boundingboxes do not overlap.

This system, while intuitive to understand, took the longest to develop by far. It is also the most spread out over various functions:  
[NavigationWorld.FindCollision()](https://github.com/Lunariz/Halite2/blob/master/Navigation/NavigationWorld.cs#L51) is the most high-level implementation that describes comparing path segments  
[NavigationCollisionUtility.CalculateCollisionTimeBetweenMovements()](https://github.com/Lunariz/Halite2/blob/master/Navigation/NavigationCollisionUtility.cs#L192) and its [counterpart CalculateCollisionTime](https://github.com/Lunariz/Halite2/blob/master/Navigation/NavigationCollisionUtility.cs#L202) describe the actual math between finding finding a collision between two objects flying in a straight line  
[BoundingBox.NavPathIntersect()](https://github.com/Lunariz/Halite2/blob/master/Navigation/NavigationCollisionUtility.cs#L375) is the entrypoint for finding collision between a path and a bounding box

Note that this approach of comparing paths is valid for both moving and unmoving objects.  
In fact, CalculateCollisionTimeBetweenMovements simplifies the situation by subtracting vectors to create an equivalent situation in which one object is unmoving.

### Postprocessing Paths

Because the previous step of avoiding collisions is iteration-bound, it is possible that a ship cannot find a valid path in its allotted iterations.  
As a result, we need to forcefully cut their paths short to prevent any collisions.  
This process is quite similar to avoiding collisions - we first find the collision, but instead of generating an avoidance point, we generate a [stop point](https://github.com/Lunariz/Halite2/blob/master/Navigation/NavigationPath.cs#L221) instead.

But this is not all. It is possible other paths relied on that ship moving during their iterations. This ship now stopping can actually cause a new collision with another of our ships.  
As such, we once again iterate over all paths and attempt to find new collisions, and forcefully stop them, until there are no collisions remaining.

This process takes place [here](https://github.com/Lunariz/Halite2/blob/master/Navigation/BatchNavigation.cs#L129)

## Pros and Cons

While this system was incredibly interesting to write, it is important to recognize that its scope was completely unnecessary for the problem at hand.  
Because the situation of the game changes constantly, these planned paths are almost useless beyond their first step.  
As a result, I spent a lot of time creating and debugging an incredibly interesting system to solve a problem that had a perfectly valid, naive approach: only calculating one frame at a time and making sure that frame does not cause collisions.  
Even worse, this system, even with the incredibly helpful optimizations, is very computationally heavy.  
For being a module in a ML system that relies on quick simulation, this was actually pretty damning.

Of course, the major pro is that I learned a lot from it, which was my goal for this project ;)