# AsyncNet
Asynchronous network library for .NET
## Documentation
[Wiki](https://github.com/bartlomiej-stys/AsyncNet/wiki)
## Purpose
The primary purpose of this library is to provide easy to use interface for TCP and UDP networking in C#
## Getting started
This repository contains multiple projects that fall into different category. See below.
## AsyncNet.Tcp
### Installation
[NuGet](https://www.nuget.org/packages/AsyncNet.Tcp/)
### Features:
* Easy to use TCP server
* Easy to use TCP client
* SSL support
* Custom protocol deframing / defragmentation support
### Basic Usage
#### TCP Server
```csharp
var server = new AsyncNetTcpServer(7788);
server.ServerStarted += (s, e) => Console.WriteLine($"Server started on port: " +
    $"{e.ServerPort}");
server.ConnectionEstablished += (s, e) =>
{
    var peer = e.RemoteTcpPeer;
    Console.WriteLine($"New connection from [{peer.IPEndPoint}]");

    var hello = "Hello from server!";
    var bytes = System.Text.Encoding.UTF8.GetBytes(hello);
    peer.Post(bytes);
};
server.FrameArrived += (s, e) => Console.WriteLine($"Server received: " +
    $"{System.Text.Encoding.UTF8.GetString(e.FrameData)}");
await server.StartAsync(CancellationToken.None);
```
#### TCP Client
```csharp
var client = new AsyncNetTcpClient("127.0.0.1", 7788);
client.ConnectionEstablished += (s, e) =>
{
    var peer = e.RemoteTcpPeer;
    Console.WriteLine($"Connected to [{peer.IPEndPoint}]");

    var hello = "Hello from client!";
    var bytes = System.Text.Encoding.UTF8.GetBytes(hello);
    peer.Post(bytes);
};
client.FrameArrived += (s, e) => Console.WriteLine($"Client received: " +
    $"{System.Text.Encoding.UTF8.GetString(e.FrameData)}");
await client.StartAsync(CancellationToken.None);
```
#### Awaitaible TCP Client
```csharp
var client = new AsyncNetTcpClient("127.0.0.1", 7788);
var awaitaibleClient = new AwaitaibleAsyncNetTcpClient(client);

try
{
    var awaitaiblePeer = await awaitaibleClient.ConnectAsync();

    var hello = "Hello from client!";
    var bytes = System.Text.Encoding.UTF8.GetBytes(hello);

    await awaitaiblePeer.RemoteTcpPeer.SendAsync(bytes);
    var response = await awaitaiblePeer.ReadFrameAsync();

    Console.WriteLine($"Client received: " +
        $"{System.Text.Encoding.UTF8.GetString(response)}");

    awaitaiblePeer.RemoteTcpPeer.Disconnect(AsyncNet.Tcp.Connection.ConnectionCloseReason.LocalShutdown);
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    return;
}
```

## AsyncNet.Udp
### Installation
[NuGet](https://www.nuget.org/packages/AsyncNet.Udp/)
### Features:
* Easy to use UDP server
* Easy to use UDP client
### Basic Usage
#### UDP Server
```csharp
var server = new AsyncNetUdpServer(7788);
server.ServerStarted += (s, e) => Console.WriteLine($"Server started on port: {e.ServerPort}");
server.UdpPacketArrived += (s, e) =>
{
    Console.WriteLine($"Server received: " +
        $"{System.Text.Encoding.UTF8.GetString(e.PacketData)} " +
        "from " +
        $"[{e.RemoteEndPoint}]");

    var response = "Response!";
    var bytes = System.Text.Encoding.UTF8.GetBytes(response);
    server.Post(bytes, e.RemoteEndPoint);
};
await server.StartAsync(CancellationToken.None);
```
#### UDP Client
```csharp
var client = new AsyncNetUdpClient("127.0.0.1", 7788);
client.ClientReady += (s, e) =>
{
    var hello = "Hello!";
    var bytes = System.Text.Encoding.UTF8.GetBytes(hello);

    e.Client.Post(bytes);
};
client.UdpPacketArrived += (s, e) =>
{
    Console.WriteLine($"Client received: " +
        $"{System.Text.Encoding.UTF8.GetString(e.PacketData)} " +
        "from " +
        $"[{e.RemoteEndPoint}]");
};
await client.StartAsync(CancellationToken.None);
```
