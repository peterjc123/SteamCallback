# SteamCallback ![](https://travis-ci.org/peterjc123/SteamCallback.svg?branch=master)
## Introduction
It is a library written in C# to provide developers with callbacks when games start or finish updating or running.

## Example
First, add the callbacks in your code.
```C#
Callbacks.AppStarted += Callbacks_AppStarted;
Callbacks.AppEnded += Callbacks_AppEnded;
```
And then, add some code to process the stuff.
```C#
private void Callbacks_AppEnded(int appid, DateTime time)
{
    Console.WriteLine($"appid: {appid} ended at time: {time}");
}

private void Callbacks_AppStarted(int appid, DateTime time)
{
    Console.WriteLine($"appid: {appid} started at time: {time}");
}
```
It's done! Just so simple. Try it if you need it.

## Future Work
There're two more callbacks available at the time. They' re __AppUpdateStarted__ and __AppUpdateEnded__.
I will try to introduce more and more callbacks using local Steam client as far as I can.
