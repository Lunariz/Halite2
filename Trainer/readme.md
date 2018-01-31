# High-level strategy choosing through NEAT

## Prototype

While the main objective of my approach was to use NEAT to choose strategies, you will see that there are various other Trainers and StrategyChoosers I used as well.  
In order to get a prototype of this approach running, I first made a more naive StrategyChooser first, called the StringStrategyChooser.  
Similarly, I made two naive Trainers as well to get some basic evolution going, which I called RandomTrainer and EvolutionaryTrainer.

The StringStrategyChooser is quite simple: only a single factor determines the strategy a ship will perform, and that is its ID. For example, the following string:
```
0,0,1
```
Will cause the first and second ship to dock, and the third to attack the enemy. After that, the pattern repeats: the fourth and fifth ship dock, while the sixth attacks.

This string is easy to randomly generate - which is what the RandomTrainer did. It would simply generate random StringStrategyChoosers and let them battle, saving only the champion.

Since StrategyChoosers are not transitive (A beating B and B beating C, does not mean A beats C), this often meant that the champion was easily defeated by one who happened to counter it during that specific match, not because the new champion was globally better.  
To combat this, I developed the EvolutionaryTrainer. This Trainer introduces two new ideas: a pool or population of StrategyChoosers, and mutation.

The EvolutionaryTrainer evolves a population of StrategyChoosers by:
* Identifying the best StrategyChoosers in the population
* Removing the worst bots and duplicating the best to rebuild the population
* Mutating the StrategyChoosers and allowing them to cross over (combine their contents to reproduce)

Over time, the quality of the population increases, as worthwhile mutations are rewarded and detrimental mutations are killed off.

But one question remains: how do we decide which StrategyChoosers are 'the best'?

## Simulating battles through tournaments

Two components form the backbone of deciding which StrategyChoosers are allowed to procreate: simulating battles and simulating tournaments

### Simulating battles

In order to find out which StrategyChooser performs best, we need to actually make them perform.  
This is why I built a framework for simulating games between different StrategyChoosers.

The first relevant script is [Trainer.PlayMatch()](https://github.com/Lunariz/Halite2/blob/master/Trainer/Trainer.cs#L42), which takes a list of StrategyChoosers and sets up a match for them.  
Because matches are played in a completely separate program (halite.exe) we need to make various preparations to get this working.  
One of these preparations is saving StrategyChoosers as files - this way we can pass the data to the bots who will be playing without a direct connection.

Next, we run the actual simulation by calling halite.exe with the relevant parameters (file locations). This is done in [RunUtility.RunMatch()](https://github.com/Lunariz/Halite2/blob/master/Trainer/RunUtility.cs#L62)

We parse the outputs of this simulation via [RunUtility.ProcessOutput()](https://github.com/Lunariz/Halite2/blob/master/Trainer/RunUtility.cs#L132), from which we can build [GameResults](https://github.com/Lunariz/Halite2/blob/master/Trainer/RunUtility.cs#L195). Using these results, we can decide how to treat the winners or losers in our tournament

### Simulating tournaments

Now that we can run single matches, it's time to use this fact to run an entire tournament on a list of StrategyChoosers.

This is all done through [TournamentUtility.RunTournament()](https://github.com/Lunariz/Halite2/blob/master/Trainer/TournamentUtility.cs#L36).  
RunTournament divides the games to be played into two groups: duels (1v1) and groups (1v1v1v1).  
While playing these games, it keeps track of StrategyChoosers through a point system, assigning points for every game played.

Both parts of the tournament (duel games and group games) are further divided into a number of rounds.  
In each round, the list of StrategyChoosers is randomized, and all StrategyChoosers play one match.

Since simulating these games is the most computationally expensive part of our whole Trainer system, it was highly effective to parallelize the games played.  
[TournamentUtility.PlayGames()](https://github.com/Lunariz/Halite2/blob/master/Trainer/TournamentUtility.cs#L57) allows for this by first batching the games to be played, and then playing them concurrently.

When all games are played, we can sort the StrategyChoosers by points (or points per game, if not all bots play an equal amount of games).  
The top N in this sorted list are then 'the best'

## NEATStrategyChooser

With a system for choosing winning StrategyChoosers in place, there is only one thing left to do: create and mutate interesting StrategyChoosers.  
For this I chose to use NEAT, or NeuroEvolution of Augmenting Topologies. You can read the paper about NEAT [here](https://pdfs.semanticscholar.org/10fb/6715f0cdbf1f0c3c5574d022b132e1e99cca.pdf).

Each NEATStrategyChooser is a neural network, using an arbitrary amount of inputs to generate an arbitrary amount of outputs.  
The whole framework that makes this possible is [SharpNEAT](http://sharpneat.sourceforge.net/). It is quite extensive, so I won't go into details about how it works.

You can find out which features I decided to use in [NEATInput](https://github.com/Lunariz/Halite2/blob/master/Trainer/StrategyChoosers/NEATInput.cs)

NEAT also supplies its own way of mutating and combining neural networks.  
As a result, I [integrated the tournament style of describing fitness](https://github.com/Lunariz/Halite2/blob/master/Trainer/NEATTrainer.cs#L211) into the NEAT framework, rather than the other way around.

The result is the [NEATTrainer](https://github.com/Lunariz/Halite2/blob/master/Trainer/NEATTrainer.cs), which additionally shows you the current best neural network while training!

## Pros & Cons

One huge benefit of using this system - evolving neural networks to choose high-level strategies - is that it is applicable to more than one problem.  
The Trainer/StrategyChoosers are just one module in the whole, and by swapping out the implementations of strategies, you can evolve any neural network to solve any problem, especially if it requires direct battling.

The largest drawback is the time it takes to simulate. In a variable environment (such as Halite's randomly generated maps), you need to run multiple rounds in the tournament (on a large pool of StrategyChoosers) to get an accurate ranking.  
As a result, it took many hours of simulation to evolve a NEATStrategyChooser which performed well.