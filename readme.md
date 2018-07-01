BuildTools Windows GUI
======================

The goal for this is, when it's finished, to be an easy to use, self-contained BuildTools GUI for Windows. It will
check for and automatically setup the proper environment for BuildTools to run in, as well as automatically check for
and download updates to the BuildTools jar itself. It is currently working on 64 bit systems, there seems to be some
issues when running with a 32 bit JVM. This seems to be a BuildTools.jar issue, though.

Downloading
-----------

~~You can download the most recent build from my server:~~ ~~https://www.demonwav.com/down/BuildToolsGUI.exe~~

No other setup needs to be done, just run the program once you've downloaded it.

Building
--------

Open the solution in Visual Studio.

To resolve the dependencies, open the NuGet Package Manager (Tools -> Library Package Manager -> Manage NuGet Packages
for Solution) and search for and install `Json.NET` and `DotNetZip`. Then make sure you have the [Vitevic Assembly
Embedder](https://visualstudiogallery.msdn.microsoft.com/a7196a81-67fc-4a26-a88a-b68ef31c2266) for Visual Studio
installed.

Once that is done, right click on the `Ionic.Zip` and `Newtonsoft.Json` packages under "References" in the Solution
Explorer, and choose `Properties`. In the Properties windows, under `Advanced` change `Embedded Assembly` to `True`.
This will make Visual Studio embed these two libraries into the single `BuildTools.exe` so there are no DLL's to worry
about.

You will also need to add a reference to `System.Manager` for the UninstallJava project.

Further Info
------------

If you have any
questions or want to contact me for any reason, you can find me in IRC:

`irc.spi.gt`

I am usually in *#spigot* and *#spigot-dev*, but you can always just message me, username is DemonWav.
If you can't  find me there, you should always be able to message me in `chat.freenode.net`
