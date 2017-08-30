# TanksNetworkingInAzure

- [Motivation](#motivation)
- [Prerequisites](#prerequisites)
- [Project overview](#project-overview)
- [Working environment](#working-environment)
- [Downloading a project](#downloading-a-project)
- [Building a dedicated game server](#building-a-dedicated-game-server)
	- [Building unity server](#building-unity-server)
	- [Bulding Docker Image](#bulding-docker-image)
- [Testing our Docker Image locally](#testing-our-docker-image-locally)
- [Testing our image in the cloud](#testing-our-image-in-the-cloud)
- [Warning](#warning)
- [What happened?](#what-happened)
- [Additional Links](#additional-links)

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
our Unity server to cloud, we need to leverage container technologies. And we need several tools installed for that:

- Azure CLI ([installation instructions](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli))
- Docker Engine ([installation instructions](https://docs.docker.com/engine/installation/#desktop))

## Downloading a project

If you're familiar with git, you may just clone repository. If not, you may spend some time to get acquainted with git, or just download this repository as a [zip](https://github.com/PoisonousJohn/TanksNetworkingInAzure/archive/master.zip) archive.

## Building a dedicated game server

### Building unity server

To host a dedicated game server, first we need to build it. Since the server doesn't require any graphical interface, we should ship ip in a "headless" mode. It's supported only in Linux builds. This means that you need to install a Linux Build Support for Unity (you can open Unity Download Assistant and select checkbox for Linux Build Support only).

To build project:
1. Open it in Unity
1. Open build settings (File -> Build Settings)
1. Ensure that PC, Mac & Linux Standalone is selected as a platform.
1. Set `TargetPlatform` to Linux
1. Set `Architecture` to `x86 + x86_64 (Universal)`
1. Set checkbox `Headless mode`
1. Click `Player settings`
1. Find `Scripting Define Symbols` field. This project uses Scripting Define Symbols to conditionaly compile some code. Supported defines are:
**DEDICATED_SERVER_MODE** -- makes application automatically start in a dedicated server mode. **DEDICATED_LOCALHOST** -- stubs some code to allow launch a dedicated game server locally on your machine.
1. To build a dedicated game server you need to add a `DEDICATED_SERVER_MODE` define symbol into `Scripting Define Symbols` player setting. So you will have something like `CROSS_PLATFORM_INPUT;DEDICATED_SERVER_MODE`.
1. Hit `Build` button and save your build in a `builds` directory inside a project folder. Name the build as `linuxserver`. This step is important. If you name folder & build wrong, next steps may not work for you. Start building. Here's what you should have after build succeeded:
```
TanksNetworking/
├── builds
│   ├── linuxserver_Data
│   ├── linuxserver.x86
│   └── linuxserver.x86_64
```

### Bulding Docker Image

What is Docker? To put it simply, Docker is a technology which allows us to deploy applications as a "single artifact". This artifact contains application and all its dependencies and instructions of how to launch that app. The application is run in a "sandbox" provided by the "host" OS.

Docker may virtualize resources, i.e. you may limit CPU and memory resources, you may put restrictions on a network etc.

Working with Docker images (that single deployment artifact I mentioned earlier), has many benefits over bare Virtual Machine approach. You may read more on Docker's website if you want.

So we will containerize our Unity Server. This means that we'll put Unity Server build into Docker Image. Docker needs instructions on how to pack your application into the image and how to launch your app. You can find them in `Dockerfile`.

Let's examine it a little.

`FROM ubuntu:16.04` this line tells what is the base image. It's like what is a "Base class" in programming. So we tell that our image is based on ubuntu linux. 16.04 is a tag of image. Typically it's used to bind to a specific version of the image. So in our case we're using 16.04 version of the Ubuntu Linux.

`RUN useradd -ms /bin/bash unity` this line runs a command inside an image. `useradd` is a linux command for adding a new user. This is reqiured to launch unity server under user, created specifically for this application.

`WORKDIR /home/unity` this tells to change a "Working Directory" when building your app. It's like a `cd` command in command line.

```
COPY builds/linuxserver.x86_64 /home/unity/
COPY builds/linuxserver_Data /home/unity/linuxserver_Data/
```

These lines peform a copy of files from `build context` to container image. So basically we're copying our server's files inside container.

`RUN chown -R unity:unity /home/unity/linuxserver*` this line calls a `chown` linux command to change owner of the files. So only `unity` user we've created earlier, may access them.

`USER unity` this line tells under which user following commands will be launched.

`EXPOSE 7777-7787` this line tells which ports should be `exposed` by container. Server uses specific ports to talk with clients. And this line tells which ones.

```
ENV SERVERS_REGISTRY_URL http://jpgjsr.azurewebsites.net/api/servers
ENV HEARTBEAT_PERIOD 3
```

These lines set environment variables (you can typically access them via System.Environment in C#). They are required to configure how server will talk to game servers registry (we will find out what it is later).

` CMD ["./linuxserver.x86_64", "-logFile", "/dev/stdout", "-batchmode", "-nographics"]`

And finally, this line tells how to launch a server. Notice that we're passing parameters to our server specifying that log should go to console and that we don't need to initialize graphics devices.

Now we know what is a Dockerfile and what it does and can start building a Docker Image.

1. Open `PowerShell`
1. Change working directory to the Project's directory (`cd` command).
1. Execute command `docker build -t unityserver:latest .`

This commands tells Docker engine to start a build. Parameter -t tells what is a name and tag of the image. Remember we were talking about tags? :latest stands for... yep, latest version of the image, obviously.

And one more tiny detail. Dot at the end. This tells that build context is a current directory. This is why we changed our working directory to the project's directory.

You should see the output similar to following:

```
PS C:\Users\ivfateev\Unity\TanksNetworking> docker build -t unityserver:latest .
Sending build context to Docker daemon    246MB
Step 1/11 : FROM ubuntu:16.04
 ---> ccc7a11d65b1
Step 2/11 : RUN useradd -ms /bin/bash unity
 ---> Using cache
 ---> 135a664b0ae4
Step 3/11 : WORKDIR /home/unity
 ---> Using cache
 ---> 274d8f72c829
Step 4/11 : COPY builds/linuxserver.x86_64 /home/unity/
 ---> Using cache
 ---> 2de07cdf2584
Step 5/11 : COPY builds/linuxserver_Data /home/unity/linuxserver_Data/
 ---> Using cache
 ---> f71bc957c7b7
Step 6/11 : RUN chown -R unity:unity /home/unity/linuxserver*
 ---> Using cache
 ---> 4e4a60c9bffe
Step 7/11 : USER unity
 ---> Using cache
 ---> 9d72af116d60
Step 8/11 : EXPOSE 7777-7787
 ---> Using cache
 ---> 280e90f3c316
Step 9/11 : ENV SERVERS_REGISTRY_URL http://jpgjsr.azurewebsites.net/api/servers
 ---> Using cache
 ---> a43c8dbf448b
Step 10/11 : ENV HEARTBEAT_PERIOD 3
 ---> Using cache
 ---> 68f5de09f706
Step 11/11 : CMD ./linuxserver.x86_64 -logFile /dev/stdout -batchmode -nographics
 ---> Using cache
 ---> d5ea0d29bc48
Successfully built d5ea0d29bc48
Successfully tagged unityserver:latest
SECURITY WARNING: You are building a Docker image from Windows against a non-Windows Docker host. All files and director
ies added to build context will have '-rwxr-xr-x' permissions. It is recommended to double check and reset permissions f
or sensitive files and directories.
```

As you can see, each command in Dockerfile is performed as a separate step. If you get an error, try to follow previous steps more precisely. Likely you did something wrong.

Now we've successfully built server's Docker Image. But where is it? Docker Images are stored in a Docker Registry. It's something like package repository (say the one used by npm, https://www.npmjs.com/) but for images. Default Docker repository is http://hub.docker.com. Actually we've used ubuntu:16.04 image. And you can find it there.

Yet, we didn't see our image. It's stored in our local repository. You can list all images that are available on your PC with command `docker image ls`. For instance, this is mine output:

```

PS C:\Users\ivfateev\Unity\TanksNetworking> docker image ls
REPOSITORY                   TAG                  IMAGE ID            CREATED             SIZE
unityserver                  latest               d5ea0d29bc48        4 days ago          283MB
ubuntu                       16.04                ccc7a11d65b1        2 weeks ago         120MB
ubuntu                       latest               7b9b13f7b9c0        2 months ago        118MB
```

You can find the image we've created in listing.

## Testing our Docker Image locally

So let's try and test our containerized server!

1. Open project in Unity
1. Open `Build settings`, then `Player settings`
1. Remove `DEDICATED_SERVER_MODE` scripting define symbol. This is required to prevent server starting automatically, and allow us to choose in which mode we want to launch an application
1. Launch application from `LobbyScene`

Ok, now we have application that can connect to the server. Next we should launch our server.

1. Return to the PowerShell
1. Execute command `docker run -p 7777:7777/udp unityserver:latest`

You should see output of the server in your console. If you press `Ctrl+C`, you'll detach from output, but server will still be running. Congratulations, you've just created a local container running your server image. You may image a `Container` as a tiny VM. Containers, also as VMs, may be started and stopped. You can list running containers with `docker ps` command. If you want include stopped containers in output, you use `docker ps -a` command. For example:
```
PS C:\Users\ivfateev\Unity\TanksNetworking> docker ps
CONTAINER ID        IMAGE                COMMAND                  CREATED             STATUS              PORTS                                   NAMES
35cc24ce6098        unityserver:latest   "./linuxserver.x86..."   6 minutes ago       Up 6 minutes        7777-7787/tcp, 0.0.0.0:7777->7777/udp   quizzical_wozniak
```

In the output, you may see container id, image that is running inside a container, and what is interesting &mdash; mapped ports. Remember we used `EXPOSE` command inside Dockerfile? Expose do not expose ports directly when you run a container. You should specify which ports of the "host" machine you want to map to the ports inside container. This is required because by default docker virtualize network.

For instance, imagine you want to launch serveral game servers. Server listens 7777 port. If you try to launch the second container with the same image, server will fail to start, because 7777 port is already occupied by the first instance.

Docker allows us to expose a single 7777 port inside a container, but we may map 7778 port on host machine to the 7777 port inside container. So we won't have port collision. So server is always listening for 7777 port, but it's virtual inside container.

Let's get back to Unity. Click on `Join` button in lobby UI. If everything is ok, you'll see that you've joined server's lobby. There you will wait until other players will join. This means that our container image works fine.

Now let's keep thing clear and delete our container with command `docker rm <containerId>`. Replace `<containerId>` with container id from listing of your `docker ps` command. Container will be stopped and removed.

## Testing our image in the cloud

Well, being able to play in the LAN is pretty cool, but not so exciting. Let's launch our server in the cloud, so anyone could join us! We need to use Azure CLI for that.

> It's assumed that at this point you have Azure account ready to use.

First, we should create a private Docker Registry. Remember we talked about hub.docker.com? Private docker registry is the same thing, but you can keep it private. Often you don't want to share your images to the public resources. Azure provides an easy way to create a private docker registry. There are [several ways](https://docs.microsoft.com/en-us/azure/container-registry/) to create it.

I recommend use Azure Portal as the simplest one. It's pretty straightforward, so I believe you can handle it without my guidance. After you finish, save `Username` and `Password` from Container Registry **Access keys** tab.

First, we'll tag our server image so we can push it to our private registry.

1. Open PowerShell
1. Execute command `docker image ls`. It will show you list of available images. Find the one we've created earlier and copy its image id.
1. Execute command `docker tag <imageId> -t <yourPrivateRegistryUrl>/unityserver:latest`. Replace `<imageId>` with image id we've copied eariler. Replace `<yourPrivateRegistryUrl>` with the url of Azure Container Registry you've created earlier. It should end with "azurecr.io". You can also find it on "Access keys" tab as "Login server".
1. Execute `docker image ls` command again, and notice that our image has new tag.
1. Now we need to get docker know about our private registry and authorize it. `docker login -u "<username>" -p "<password>" <yourPrivateRegistryUrl>`. You should see "Login Succeeded" as the response to this command.
1. Now, push image to the private registry: `docker push <yourPrivateRegistryUrl>/unityserver:latest`

Phew, that was tough. But we're almost done. Hold on. Now we need share our awesome server to the whole world! We will use Azure Container Service with [kubernetes](http://kubernetes.io) orchestrator. What is orchestrator? It's just a tool that automates pretty much tasks of managing cluster of your virtual machines manually. For now you can imagine that this tool automatically can launch your unity servers in the cloud.

1. Login your Azure CLI. I'd recommend to follow instructions for interactive login [here](https://docs.microsoft.com/en-us/cli/azure/authenticate-azure-cli#interactive-log-in). This will enable you operate your azure resources right from your PowerShell!
1. Create resource group for our server: `az group create --name "<groupName>" --location "westeurope"`. Location `westeurope` is just one of the available locations. You can choose different one, if you wish.
1. Open PowerShell as administrator (right click on PowerShell shortcut -> "Run as Administrator"). We need that to install required program.
1. Execute command `azure acs kubernetes install-cli --location C:\Windows\kubectl.exe`. This will install kubernetes cluster manager that we'll need to run our server. Close Administrator's PowerShell. Run `kubectl` command in a regular PowerShell. If you see help output of kubectl command, then you can procced to the next step. Else you should correctly install kubectl, carefully following instructions.
1. Now we can automatically deploy virtual machines, that will run our server. `az acs create --orchestrator-type Kubernetes -g testgroup -n MyContainerService --agent-count 1 --master-count 1`. This process will take quite a while. But be patient. Believe me, it's far quicker than setting it up manually.
1. Previous command deployed two Virtual Machines and bunch of other resources such as network, required to run a cluster. Typically servers are not directly accessible from the internet. But game servers are the special case, because we care about latency. We need to make few changes to the cluster's network:
	* Allow UDP traffic inside cluster's network. It's like setting up your PC's firewall.
		* Go to the Azure portal
		* Find resource group you've created earlier
		* Find resource of type "Network security group" and open it
		* Open "Inbound security rules" tab
		* Click "Add" button
		* Set "Port range" to "7777-7787". These are the ports used by our server.
		* Set "Priority" to "110"
		* Click "Ok"
		* Wait until rule is applied
	* Expose our server's VM to the rest of the world
		* In the same resource group find resource of type "Network interface" that has "agent" in its name. This is a network interface (it's like virtual network adapter) that is attached to Virtual Machine. Open it.
		* Navigate to "IP Configurations" and select "ipconfig1" configuration.
		* Enable `Public IP address`
		* Click on `Configure required settings`
		* Click `Create new`
		* Click `Ok`
		* Click `Save`. Wait until changes are applied. This will create new `Public IP` &mdash; IP address that is available from the Internet. So our game clients could use it to connect to our game server. We've selected that IP address is "dynamic". This means that it can change time to time. You can find it out in "Public IP" field when you open a virtual machine overview. Cool, now we're ready to launch our server.
1. Get back to the PowerShell and run `az acs kubernetes get-credentials --resource-group testgroup --name MyContainerService`. This will authorize kubectl to manage our cluster.
1. Did you remember how we've created Azure Container registry to host our images? kubectl need to know how to login to that registry. So let's create "secret": `kubectl create secret docker-registry acrsecret --docker-username <username> --docker-server <yourRegistryAddress> --docker-email "<youremail>" --docker-password "<registryPassword>"`
1. Now you need to open `deploy.yaml` file and replaces `image:` parameter value with the image on your private docker registry.
1. Finally! Let's run our server! Change your working directory to project's folder. Run command `kubectl apply -f deploy.yaml`. This will start proccess of launching game servers.
1. Now let's see whether it's successful or not: `kubectl get pods -w` &mdash; this will show luanched instances (pods) of our server in the cluster. `-w` flag makes command write any updates of pods' status in the console. Wait until you'll see "Running" status inside containers:
```
PS C:\Users\ivfateev\Unity\TanksNetworking> kubectl get pods -w
NAME                                     READY     STATUS              RESTARTS   AGE
gameserver-deployment-4170775043-2zkh9   0/1       ContainerCreating   0          22s
gameserver-deployment-4170775043-shqnr   0/1       ContainerCreating   0          22s
gameserver-deployment-4170775043-2zkh9   1/1       Running   0         27s
gameserver-deployment-4170775043-shqnr   1/1       Running   0         27s
```
11. Then you can press `Ctrl+C` to return back to the command prompt.
1. Now we can login to the dedicated game server in a cloud, following instructions that we did in "Testing our Docker Image locally", but we should replace 127.0.0.1 in text field with `Public IP` of our Virtual Machine. Again, you can find it on Azure portal, when you open "Agent VM".
1. To stop running servers you may execute `kubectl delete -f deploy.yaml` which will delete created pods. This may be required if you want to redeploy pods with different docker image. In that case you first delete pods, then apply them again.

## Warning
Virtual machines **consumes money** from your subscriptions when they're running. At this point you have two Virtual Machines running. One is "master" and another one is "agent". You can distinguish them by name.

So to **save your money** you may want **stop virtual machines** when you don't need them. This can be done manually or automatically at specific time.

To do it manually, open virtual machine overview and click `Stop` button. If you want to set up auto shutdown, open Virtual Machine and go to the "Auto-shutdown" tab.

Do not forget to start Virtual Machines again if you want to play more with servers and kubectl command.

## What happened?

You may be interested in details behind `kubectl apply` command. Basically it takes instructions from `deploy.yaml` file. Let's try to understand its contents.

This following part tells that we would use [Deployment](https://kubernetes.io/docs/concepts/workloads/controllers/deployment/) controller. It will automatically launch as many servers as we specify. Metadata name is required to distinguish deployments between each other. As you may noticed names for "pods" were generated with metadata.name as prefix.

```
apiVersion: apps/v1beta1
kind: Deployment
metadata:
  name: gameserver-deployment
```

The most insteresting part starts with `spec` section.
```
 replicas: 2
```

It tells how many "replicas" of the server we want to launch. For instance, I've specified 2 replicas, so players could play two different matches simultaneously.

`template` section speicifies template configuration that will be used to launch a replica. Template has its own `spec` section wich describes `pod` configuration.

`hostNetwork: true` tells that we don't want to use network virtualisation and allow server to choose port itself.

`imagePullSecrets` specifies which "secret" should be used to pull the image for container. Remember we've created a "secret" with kubectl command? Here's why we did it.

`containers` each pod may run multiple containers. And this sections describes them.

`image` this is the most important part of the file. It tells which image to use for container. We specified that we want to use latest version of our server.

`imagePullPolicy: Always` tells that we want to pull the image even if it's already present on server.

## Additional Links

- [Azure Container Service](https://docs.microsoft.com/en-us/azure/container-service/kubernetes/) tutorials
- [Docker](http://docker.com)
- [Kubernetes](http://kubernetes.io)

