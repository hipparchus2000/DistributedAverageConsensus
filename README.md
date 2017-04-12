# DistributedAverageConsensus

Averages (and other stats) on large distributed databases.
Jeff Davies

If you wanted to calculate an accurate average (also known as mean) across a vast data set of geographically distributed data using a traditional map reduce method, you’d need to lock the whole distributed database, then calculate the mean, then unlock the database. Clearly this would interfere with the process it’s intending to monitor, so is impractical in many applications.
The solution is Distributed Average Consensus, discussed in many papers, which are usually fairly mathematical, basically mathematically proving why this general set of algorithms give a good approximation for the average.
Here is a typical paper discussing Distributed Average Consensus: http://web.stanford.edu/~boyd/papers/pdf/lms_consensus.pdf
Red River have produced an example implementation of this, using WebSockets for each node to update itself from other nodes.
https://github.com/hipparchus2000/DistributedAverageConsensus

The general concept is like this: say on each node you have a socket listener. You evaluate your local sum and average, then publish it to the other nodes. Each node listens for the other nodes, and receives their sum and averages on a timescale that makes sense. You can then evaluate a good guess at the total average by (sumForAllNodes(storedAverage[node] * storedCount[node]) / (sumForAllNodes(storedCount[node])). If you have a truly large dataset, you could just listen for new values as they are stored in the node, and amend the local count and average, then publish them.
If even this is taking too long, you could average over a random subset of the data in each node.
The example uses fleck to run on more versions of windows than windows-10-only microsoft websockets implementation. Run this on two nodes, one with
<appSettings>
    <add key="thisNodeName" value="UK" />
</appSettings>
in the app.config, and use "EU-North" in the other. The two instances exchange means using websockets. You just need to add your back end enumeration of the database.
Don't forget to add static lock or separate activity by synchronising at given times. (not shown in the example for simplicity).


