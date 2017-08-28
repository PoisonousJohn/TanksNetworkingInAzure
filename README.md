# TanksNetworkingInAzure

## Motivation

Building a multiplayer game may be a challenging task. Especially a realtime game like first-person shooter or even MMO.

Luckily we have a bunch of technologies that give us opportunity to make this task easier. For instance, with Unity 3D you can build both client and server. There are quite a few ways to build client-server solutions. But if you care about your players, you will strive to provide them the best gameplay experience.

Technically, in multiplayer games this means:
- Prevent people from cheating
- Provide the best network experience

Cheaters may ruin the entire ecosystem of your game, bypassing all constraints and game design you've carefuly built. Worst thing is that it's also causing troubles to your loyal players, and that can make them leave your game.

If your players experience network latency issues the game would feel "laggy", and likely nobody would play it in that case.

To prevent people from cheating, you need a **authorative dedicated game server**. Let's break this down to words:

- **Authorative** &mdash; this means that server has authority to decide whether action that player has commited is valid or not. In such case, if player tries to cheat, say, attempting to send sword damage higher than it should be, server will reject that, and apply only actual damage.
- **Dedicated** &mdash; this means that server will not be hosted by some of the clients. It will be dedicated, and running on some kind of "hosting". This will eliminate the "no latency" advantage of the player that "hosts" the server. Also dedicated server eases connectivity problems that you may encounter working on peer-to-peer games.

Another problem is scalability. It's cruitial for game to be able to handle all players that want to play it. If your game server can't catch up with growing traffic, you will likely lose all your users, because they just won't be able to play.

It's hard to provision server hardware and hard to maintain software scalability by your own. To address this issue we will use power of the cloud which gives you opportunity to scale up and down quickly, easily and freely.

Server development is too broad topic to cover. So I will focus on making a dedicated cloud server for your game on example of Unity Tanks Networking project.

## Prerequisites

Prior to working with this project, you need to have **Azure account** ([free trial, requires credit card](https://azure.microsoft.com/en-us/free/)) or account actived via **Azure Pass** (temporary test account, that is provided for you by Microsoft).

We'll be using docker containers, so f you're not familiar with **Docker**, you can take a brief introduction to technology [here](https://docs.docker.com/engine/docker-overview/).

## Project overview

Tanks Networking is a standard asset pack from Unity, which you can find in [Unity Store](https://www.assetstore.unity3d.com/en/#!/content/46213). It was slightly modified so it can be containerized and deployed right in the cloud.


## Working environment

To build a project, typically it's enough to have a latest Unity 2017 setup. But in order to deploy
our Unity server to cloud, we need to leverage container technologies. And we need several toolsc
installed for that:

- Azure CLI
- Docker Engine

Installation process differs between OSes, something may go wrong, so we will use pre-configured virtual machine in cloud, ready to start and play with this demo.

To create Virtual Machine (VM), use a script we've prepared for you:
1. Open Azure Cloud Shell (click `>_` sign on the top panel).
1. Shell may ask you to create a storage for its files. Just choose your subscription and click "Create storage"
1. Once Azure Cloud Shell finish its setup, you'll see command prompt: `~$`
1. Now you can launch a script that will deploy your new VM, typing following command into Azure Cloud Shell: `curl -s http://pathtoscript | bash /dev/stdin <resourceGroupName> <vmName>`
You can replace `<resourceGroupName>` with the name you want for your resource group (folder for your resources). And replace `<vmName>` with the name you want for your virtual machine. For example: `curl -s http://pathtoscript | bash /dev/stdin AzureTutorial MyWorkstationVM`

### Links

http://machinezone.github.io/research/networking-solutions-for-kubernetes/

