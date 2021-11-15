WU Fall 2021 | CSMA 302 | Lab #6
---
# Fluid Simulation

We will implement navier-stokes equations for incompressible fluid flow, using compute shaders.

The basic sim concepts will be developed in the studio and you will implement these parts individually :

*Mouse input*

 - take the mouse input pixel position `Input.mousePosition` and add velocity / concentration to the fluid field
 
*Boundary conditions*

 - Velocity Condition : before Divergence kernel, set border velocities to be the negative of the inner neighbor (see slide 29)
 - Pressure Condition : before ProjectField kernel, set border pressures to be the same as inner neighbors (see slide 30)

*Code Organization*

 - We'll go over how to make helper functions to dispatch the kernels and set up textures, to avoid duplicated code.

## Due Date

Due midnight November 21.

## Resources

[slides](https://docs.google.com/presentation/d/1xJB5mM8XYn44ucQRyh1mzvH4k4dz85qVFZ5OYxjK-JM/edit?usp=sharing)

[GPU Gems]( https://developer.download.nvidia.com/books/HTML/gpugems/gpugems_ch38.html)

## Grading

40 points for working fluid sim
15 points for mouse input
15 points for boundary conditions 
20 points for numerical stability - does the sim not explode for all parameter values?
10 points for code organizaiton and comments (make helper functions to dispatch kernels)


## Submitting 
(this is also in the syllabus, but consider this an updated version)

1. Disregard what the Syllabus said about Moodle, just submit your work to a branch on github on this repo (branch should be your firstname-lastname)
When you are finished, "Tag" the commit in git as "Complete". You can still work on it after that if you want, I will just grade the latest commit.

2. The project has to run and all the shaders you are using should compile. If it doesn't I'm not going to try to fix it to grade it, I will just let you know that your project is busted and you have to resubmit.  Every time this happens I'll take off 5%. You have 24 hours from when I return it to get it back in, working. 

3. Late projects will lose 10% every 24 hours they are late, after 72 hours the work gets an F. 

4. Obviously plagarism will not be tolerated, there are a small number of students so I can read all your code. Because it is on git it's obvious if you copied some else's. If you copy code without citing the source in a comment, this will be considered plagarism. 
