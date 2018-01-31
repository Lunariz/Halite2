# Halite 2 - An Evolutionary Approach

## Introduction

Halite 2 was an AI programming competition hosted by Two Sigma. The goal is to build a bot that controls ships to strategically beat the opponent.  
Here's an example replay to illustrate the idea:

[Replay](https://halite.io/play/?game_id=9767013&replay_class=1&replay_name=replay-20180129-132551%2B0000--749741380-312-208-1517232306)

## My Approach

I decided to use this competition to learn something new, and came upon the idea of using Machine Learning through the usage of Evolutionary Algorithms.  
While it is perfectly possible to 'hardcode' a bot to do exactly what you want it to do, I simply figured ML would be much more interesting.

I knew, however, that only using ML would make getting worthwhile results very difficult, especially because one of the largest success factors is navigation, which ML is notoriously bad at.  
Instead, I took the best parts of ML and hardcoding: I used ML to make the difficult, high-level strategic decisions, and then wrote the implementation for strategies and navigation myself.

I strongly believe that the best result is at the intersection between machine intelligence and human intelligence. This may change in the future, but it seems evident from the results of this bot as well.

Out of 5832 contestants, my bot was ranked #55 (Top 0.94%)  
Out of 153 contestants who used ML, my bot was ranked #2 (Top 1.3%)

## Structure

I have kept a clear Object-Oriented structure in my code. The structure will be reflected in this writeup as well.  
The intention of this structure is that you can choose how deep you want to go. If you find any particular part of my bot interesting, you can look at it in detail, and only understand the rest of my bot conceptually.  
My bot is divided in three parts, each of which has a separate writeup that goes into more detail. The three parts of my bot are as follows:

### High-level strategy choosing through NEAT

ML is great at taking in lots of data and processing it quickly to come at a conclusion or classification.  
In order to abuse this, I used a NEAT library called SharpNEAT to build neural networks that choose which strategy a ship performs.  
This StrategyChooser has nothing to say about how the strategy should be performed - it simply gives a high-level order to be executed.

In order to find a neural network that can interpret data in a smart way and give the correct order, NEAT builds a population of neural networks and evolves them based on their fitness.  
This fitness value is decided through a tournament - the bots battle each other, and the victors get to procreate. The result is a population of networks that improves in quality every generation.

[Read more]

### Strategy implementation

After the StrategyChooser chooses which strategy to perform, the question remaining is how to perform it.  
Each strategy has its own (hardcoded) implementation of how to perform it. This is where human intelligence comes in.  
Looking at strategies at this level is much easier than looking at the goal of the whole swarm. As a result, it is not difficult to implement and finetune a given strategy so that a single ship can reach a single goal.  
Each strategy has a different goal, but they all have the same result: each strategy returns a Command to indicate what they want to do to reach their goal.  
This Command is usually a MoveCommand, but can be a DockCommand or UndockCommand as well. Note that a MoveCommand does not describe -how- to get somewhere, simply where it wants to go.

[Read more]

### Navigation

Finally, when all ships have computed their commands, we can let Navigation loose to figured out the immediate choices for where ships will be going this turn.  
It is also at this point that we need to take care not to crash ships into each other, as Commands have no notion of other ships.  
My approach to Navigation ended up somewhat extravagant. Most users computed a nearsighted path, effectively only preventing collisions that were to happen in the next turn.  
I took this concept and extended it to prevent collisions at any point in time. This means that each ship computes a full, collisionless path to its destination, every turn.

This part of the code is probably the hardest to understand deeply, as it involves a lot of math complexity.

[Read more]

## Acknowledgements

First of all, thank you for reading my postmortem :)

A huge thanks to everyone at Two Sigma for setting this competition up, it was an absolute blast!  
I'd like to thank Fortunos for helping me conceptualize the structure from the start.  
I'd also like to thank reCurse, Psyho, Shummie, fohristiwhirl, and janzert in the Discord - I wouldn't have been able to think about strategies without your daily discussions.  
Lastly, I'd like to thank everyone else participating in the competition, you were all worthy opponents!