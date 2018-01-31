# Strategy implementation

## Progression

The intention behind the strategy system is to provide a way to code various potentially beneficial strategies, such that the StrategyChooser can choose which is most applicable in a given situation.
On one hand, this allows for lots of experimentation, as we expect our ML system to filter out flawed strategies.
On the other hand, the more strategies we implement, the more difficult it becomes to train a StrategyChooser, as the amount of potentially useless mutations increases.

As it turns out, building many ineffective strategies did not prove to be very useful, as the time it took to evolve anything usable was simply too much.
[That is why I decided to only enable two strategies], and make them intelligent enough that either choice would prove useful in some way.

The two intelligent strategies are [Attack] and [Defend] - the rest can no longer be chosen by the strategychooser, but are still used inside Attack and Defend.

In addition, there is one odd strategy that is more event-based, as it cannot be chosen and is more permanent.
This is the [Desert strategy]. Since retreat is sometimes the best way to survive, Desert attempts to survive instead of fight by hiding in a corner.
Since this is a very specific, global strategy, I decided not to count on the ML system to choose when to perform it (and perform it on all ships), and hardcoding it instead.

Both the Attack and Defend strategy effectively form groups of ships and compare our shipcount to the enemy shipcount, attacking if our shipcount is higher, or abandoning if our shipcount is considerably lower.

## Pros and Cons

This system of implementing various strategies is a good way of introducing human strategic knowledge into the system.
By relying on the ML system to make high-level strategic choices, coding these strategies simply comes down to thinking of worthwhile goals and how to attain them in a one-ship context.

The major con is that implementing these strategies can be quite a bit of work, and requires insight into the problem.
In addition, working in this one-ship context can make it difficult to perform tasks as a group, especially if you rely on emergent behaviour.